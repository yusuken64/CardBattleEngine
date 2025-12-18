namespace CardBattleEngine;

public class GameEngine
{
	private readonly EventBus _eventBus;
	private readonly Queue<(IGameAction, ActionContext)> _actionQueue = new();

	public Action<GameState, IGameAction> ActionCallback;
	public Action<GameState, (IGameAction action, ActionContext context)> ActionPlaybackCallback;
	public Action<GameState> ActionResolvedCallback;

	public GameEngine()
	{
		_eventBus = new();
	}
	
	public void Resolve(GameState gameState, ActionContext actionContext, IGameAction action)
	{
		if (gameState.IsGameOver()) { return; }

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
			if (!current.action.IsValid(gameState, current.context, out string _))
				continue;

			current.context.OriginalAction = current.action;

			// Pre-resolution triggers
			foreach (var trigger in _eventBus.GetTriggers(gameState, current.action, current.context, EffectTiming.Pre))
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
					_actionQueue.Enqueue((effect.Item1, effect.Item2));
				}
			}

			// Post-resolution triggers
			foreach (var trigger in _eventBus.GetTriggers(gameState, current.action, current.context, EffectTiming.Post))
			{
				_actionQueue.Enqueue(trigger);
			}

			_eventBus.EvaluatePersistentEffects(gameState);
		}
		ActionResolvedCallback?.Invoke(gameState);
	}

	private IEnumerable<(IGameAction, ActionContext)> ResolveAction(GameState gameState, (IGameAction action, ActionContext context) current)
	{
		gameState.History.Add(new HistoryEntry()
		{
			Turn = gameState.turn,
			Player = current.context.SourcePlayer,
			Action = current.action,
			Context = current.context
		});

		// Resolve action and enqueue returned side effects
		var ret =  current.action.Resolve(gameState, current.context);
		ActionPlaybackCallback?.Invoke(gameState, current);
		return ret;
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
			.Options
			.Select(opt => opt.Item1)
			.ToList();

		return allowed.Contains(action)
			&& state.PendingChoice.SourcePlayer == actionContext.SourcePlayer;
	}

	public void StartGame(GameState gameState)
	{
		var p1 = gameState.Players[0];
		var p2 = gameState.Players[1];

		Resolve(gameState, new ActionContext() { SourcePlayer = p1 }, new StartGameAction());
		Resolve(gameState, new ActionContext() { SourcePlayer = p2 }, new StartGameAction());

		Resolve(gameState, new ActionContext() { SourcePlayer = p1 }, new PromptMulliganGameAction());

		//Resolve(gameState, new ActionContext() { SourcePlayer = p1 }, new StartTurnAction());
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

	public GameEngine Clone()
	{
		return new GameEngine();
	}
}