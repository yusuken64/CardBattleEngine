namespace CardBattleEngine;

public class GameEngine
{
	private readonly EventBus _eventBus;
	private readonly Queue<IGameAction> _actionQueue = new();
	private readonly IRNG rNG;

	public Action<GameState, IGameAction> ActionCallback;

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
				if (trigger != null)
					_actionQueue.Enqueue(trigger);
			}

			if (current.Canceled)
			{
				continue;
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
				if (trigger != null)
					_actionQueue.Enqueue(trigger);
			}

			ActionCallback?.Invoke(gameState, current);
			//PrintState(gameState, current);

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

	public static void PrintState(GameState gameState, GameActionBase current)
	{
		var p1 = gameState.Players[0];
		var p2 = gameState.Players[1];

		Console.WriteLine($"T{gameState.turn} {current}");
		Console.WriteLine(PlayerStateInfo(p1));
		Console.WriteLine(PlayerStateInfo(p2));
	}

	public static string PlayerStateInfo(Player player)
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

	public GameEngine Clone()
	{
		return new GameEngine(this.rNG.Clone());
	}
	
	public static List<IGameEntity> GetPotentialTargets(TargetType type, GameState state, Player owner, Player opponent)
	{
		var targets = new List<IGameEntity>();

		switch (type)
		{
			case TargetType.EnemyHero:
				targets.Add(opponent);
				break;

			case TargetType.EnemyMinion:
				targets.AddRange(opponent.Board.Where(m => m.IsAlive));
				break;

			case TargetType.AnyEnemy:
				targets.Add(opponent);
				targets.AddRange(opponent.Board.Where(m => m.IsAlive));
				break;

			case TargetType.FriendlyHero:
				targets.Add(owner);
				break;

			case TargetType.FriendlyMinion:
				targets.AddRange(owner.Board.Where(m => m.IsAlive));
				break;

			case TargetType.Self:
				if (owner != null) targets.Add(owner);
				break;

			case TargetType.Any:
				targets.Add(owner);
				targets.AddRange(owner.Board.Where(m => m.IsAlive));
				targets.Add(opponent);
				targets.AddRange(opponent.Board.Where(m => m.IsAlive));
				break;
		}

		return targets;
	}
}