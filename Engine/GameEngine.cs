using System.Text;

namespace CardBattleEngine;

public class GameEngine
{
	private readonly EventBus _eventBus;
	private readonly Queue<IGameAction> _actionQueue = new();
	private readonly IRNG rNG;

	public GameEngine(IRNG rNG)
	{
		this.rNG = rNG;
		_eventBus = new();
	}
	
	public void Resolve(GameState gameState, Player currentPlayer, Player opponent, IGameAction action)
	{
		_actionQueue.Enqueue(action);

		while (_actionQueue.Count > 0)
		{
			var current = _actionQueue.Dequeue();

			// Check if action is still valid
			if (!current.IsValid(gameState))
				continue;

			// Pre-resolution triggers
			foreach (var trigger in _eventBus.GetPreTriggers(gameState, current))
			{
				var triggerAction = trigger.GenerateAction(gameState);
				if (triggerAction != null)
					_actionQueue.Enqueue(triggerAction);
			}

			// Resolve action and enqueue returned side effects
			var sideEffects = current.Resolve(gameState, currentPlayer, opponent);
			if (sideEffects != null)
			{
				foreach (var effect in sideEffects)
					_actionQueue.Enqueue(effect);
			}

			// Post-resolution triggers
			foreach (var trigger in _eventBus.GetPostTriggers(gameState, current))
			{
				var triggerAction = trigger.GenerateAction(gameState);
				if (triggerAction != null)
					_actionQueue.Enqueue(triggerAction);
			}

			PrintState(gameState, current);

			if (gameState.IsGameOver())
				break;
		}
	}

	public void StartGame(GameState gameState)
	{
		var p1 = gameState.Players[0];
		var p2 = gameState.Players[1];

		Resolve(gameState, p1, p2, new StartGameAction(p1, Shuffle));
		Resolve(gameState, p2, p1, new StartGameAction(p2, Shuffle));
	}

	private static void PrintState(GameState gameState, IGameAction current)
	{
		var p1 = gameState.Players[0];
		var p2 = gameState.Players[1];

		Console.WriteLine($"T{gameState.turn} {current}");
		Console.WriteLine(PlayerStateInfo(p1));
		Console.WriteLine(PlayerStateInfo(p2));
	}

	private static string PlayerStateInfo(Player player)
	{
		var board = string.Join(",", player.Board.Select(x =>
		{
			return $"{x.Attack}/{x.Health}";
		}));
		return $"{player.Name.PadRight(10)} {player.Health} {player.Mana} | {board}";
	}

	public T ChooseRandom<T>(IReadOnlyList<T> options)
	{
		if (options.Count == 0) return default!;
		return options[rNG.NextInt(0, options.Count)];
	}

	public void Shuffle<T>(IList<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rNG.NextInt(0, i + 1); // same RNG as ChooseRandom
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}