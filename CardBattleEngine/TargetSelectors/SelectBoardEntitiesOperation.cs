using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

public class SelectBoardEntitiesOperation : ITargetOperation
{
	public TargetSide Side { get; set; } = TargetSide.Enemy;   // Enemy, Friendly, Both
	public TargetGroup Group { get; set; } = TargetGroup.Minions; // Minions, Hero, All
	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		// Ignore the input — this is the "base selection"
		var players = Side switch
		{
			TargetSide.Friendly => new[] { context.SourcePlayer },
			TargetSide.Enemy => new[] { state.OpponentOf(context.SourcePlayer) },
			TargetSide.Both => new[] { context.SourcePlayer, state.OpponentOf(context.SourcePlayer) },
			_ => Enumerable.Empty<Player>()
		};

		foreach (var player in players)
		{
			switch (Group)
			{
				case TargetGroup.Minions:
					foreach (var minion in player.Board)
						yield return minion;
					break;
				case TargetGroup.Hero:
					yield return player;
					break;
				case TargetGroup.All:
					foreach (var minion in player.Board)
						yield return minion;
					yield return player;
					break;
			}
		}
	}

	// Optional: serialization
	public Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(Side), Side.ToString() },
			{ nameof(Group), Group.ToString() }
		};
	}

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
		if (actionParam.TryGetValue(nameof(Side), out var sideValue) && sideValue is string sideString)
			Side = Enum.Parse<TargetSide>(sideString);

		if (actionParam.TryGetValue(nameof(Group), out var groupValue) && groupValue is string groupString)
			Group = Enum.Parse<TargetGroup>(groupString);
	}
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TargetSide
{
	Enemy,
	Friendly,
	Both
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TargetGroup
{
	Minions,
	Hero,
	All
}