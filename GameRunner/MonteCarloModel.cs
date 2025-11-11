using CardBattleEngine;
using System.Text.Json;
using System.Text.Json.Serialization;

public class MonteCarloModel
{
	private string _path;
	private Dictionary<string, StatRecord> _stats = new();

	public MonteCarloModel(string path)
	{
		_path = path;
		LoadWeights();
	}

	private static string MakeKey(string stateHash, string actionId)
		=> $"{stateHash}::{actionId}";

	internal (IGameAction, ActionContext) ChooseAction(GameState game, List<(IGameAction, ActionContext)> actions)
	{
		string s = HashState(game);
		var action = actions
			.OrderByDescending(a =>
			{
				string key = MakeKey(s, a.ToString());
				float winRate = _stats.TryGetValue(key, out var stat) ? stat.WinRate : 0.5f;
				return winRate + Random.Shared.NextSingle() * 0.001f; // small noise
			})
			.First();

		return action;
	}

	public void UpdateFromResult(List<(GameState state, (IGameAction, ActionContext) action)> history, bool won)
	{
		foreach (var (state, action) in history)
		{
			string key = MakeKey(HashState(state), action.ToString());
			if (!_stats.TryGetValue(key, out var entry))
				entry = new StatRecord { Wins = 0, Total = 0 };

			entry.Wins += won ? 1 : 0;
			entry.Total += 1;
			_stats[key] = entry;
		}
	}

	public void SaveWeights()
	{
		var options = new JsonSerializerOptions
		{
			WriteIndented = true
		};
		var json = JsonSerializer.Serialize(_stats, options);
		File.WriteAllText(_path, json);
	}

	public void LoadWeights()
	{
		if (!File.Exists(_path)) return;
		var json = File.ReadAllText(_path);
		_stats = JsonSerializer.Deserialize<Dictionary<string, StatRecord>>(json);
		// convert back to tuple keys, if necessary
	}

	private string HashState(GameState s)
	{
		return GameStateHasher.Hash(s);
	}

	public record StatRecord
	{
		public int Wins { get; set; }
		public int Total { get; set; }

		[JsonIgnore]
		public float WinRate => Total == 0 ? 0 : (float)Wins / Total;
	}
}