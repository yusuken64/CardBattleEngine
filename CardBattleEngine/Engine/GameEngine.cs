namespace CardBattleEngine;

public class GameEngine
{
	private readonly EventBus _eventBus;
	private readonly Queue<(IGameAction, ActionContext)> _actionQueue = new();
	private readonly IRNG rNG;

	public Action<GameState, IGameAction> ActionCallback;
	public Action<GameState> ActionResolvedCallback;

	public GameEngine(IRNG rNG)
	{
		this.rNG = rNG;
		_eventBus = new();
	}
	
	public void Resolve(GameState gameState, ActionContext actionContext, IGameAction action)
	{
		if (!IsAllowedChoice(gameState, actionContext, action))
		{
			return;
		}

		if (gameState.PendingChoice != null)
		{
			gameState.PendingChoice = null;
		}

		_actionQueue.Enqueue((action, actionContext));

		while (_actionQueue.Count > 0)
		{
			(IGameAction action, ActionContext context) current = _actionQueue.Dequeue();

			// Check if action is still valid
			if (!current.action.IsValid(gameState, current.context))
				continue;

			current.context.OriginalAction = current.action;

			// Pre-resolution triggers
			foreach (var trigger in _eventBus.GetTriggers(gameState, current.action, current.context, EffectTiming.Pre, ChooseRandom))
			{
				var preSideEffects = ResolveAction(gameState, trigger);
				foreach (var preSideEffect in preSideEffects)
				{
					_actionQueue.Enqueue(preSideEffect);
				}
			}

			if (current.action.Canceled)
			{
				continue;
			}

			IEnumerable<(IGameAction, ActionContext)> sideEffects = ResolveAction(gameState, current);
			if (sideEffects != null)
			{
				foreach (var effect in sideEffects)
				{
					_actionQueue.Enqueue((effect.Item1, EnsureSelector(effect.Item2)));
				}
			}

			_eventBus.EvaluatePersistentEffects(gameState);

			// Post-resolution triggers
			foreach (var trigger in _eventBus.GetTriggers(gameState, current.action, current.context, EffectTiming.Post, ChooseRandom))
			{
				_actionQueue.Enqueue(trigger);
			}

			ActionCallback?.Invoke(gameState, current.action);
			//PrintState(gameState, current);

			if (gameState.IsGameOver())
				break;
		}
		ActionResolvedCallback?.Invoke(gameState);
	}

	private static IEnumerable<(IGameAction, ActionContext)> ResolveAction(GameState gameState, (IGameAction action, ActionContext context) current)
	{
		gameState.History.Add(new HistoryEntry()
		{
			Turn = gameState.turn,
			Player = current.context.SourcePlayer,
			Action = current.action,
			Context = current.context
		});

		// Resolve action and enqueue returned side effects
		return current.action.Resolve(gameState, current.context);
	}

	private bool IsAllowedChoice(GameState state, ActionContext actionContext, IGameAction action)
	{
		if (state.PendingChoice == null)
		{
			// Normal validation logic
			return true;
		}

		// If there is a pending choice, only the listed options are valid
		var allowed = state
			.PendingChoice
			.GetActions(state)
			.Select(opt => opt.Item1)
			.ToHashSet();
		
		return allowed.Contains(action)
			&& state.PendingChoice.SourcePlayer == actionContext.SourcePlayer;
	}

	private ActionContext EnsureSelector(ActionContext ctx)
	{
		//if (ctx.TargetSelector == null)
		//	ctx.TargetSelector = CreateRandomTargetSelector();
		return ctx;
	}

	public void StartGame(GameState gameState)
	{
		var p1 = gameState.Players[0];
		var p2 = gameState.Players[1];

		Resolve(gameState, new ActionContext() { SourcePlayer = p1 }, new StartGameAction() { ShuffleFunction = Shuffle});
		Resolve(gameState, new ActionContext() { SourcePlayer = p2 }, new StartGameAction() { ShuffleFunction = Shuffle });
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
}