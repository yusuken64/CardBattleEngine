using CardBattleEngine;

namespace GameServer.Matches;

// Bridges the engine's synchronous IGameAgent contract to a network client. GetNextAction blocks
// the calling thread until a client submits a choice via TrySubmit - this is only safe because it
// is only ever called from a match's own dedicated MatchDriver thread (see MatchDriver.cs), never
// from a SignalR/threadpool thread.
public class RemotePlayerAgent : IGameAgent
{
	public Player Player { get; }

	private int _version;
	private PendingActionSet? _currentOptions;
	private TaskCompletionSource<(IGameAction, ActionContext)>? _pending;
	private readonly TaskCompletionSource<bool> _abandoned = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public event Action<PendingActionSet>? OptionsAvailable;

	public RemotePlayerAgent(Player player)
	{
		Player = player;
	}

	public PendingActionSet? CurrentOptions => _currentOptions;

	public bool IsAbandoned => _abandoned.Task.IsCompleted;

	// Marks this player's connection as gone. Any GetNextAction call already blocked waiting on this
	// player - or the next one made for them - throws PlayerAbandonedException instead of hanging.
	public void Abandon() => _abandoned.TrySetResult(true);

	public (IGameAction, ActionContext) GetNextAction(GameState game)
	{
		// MatchDriver only ever calls the agent for game.CurrentPlayer, and this engine's PendingChoice
		// is always raised for the currently active player - so PendingChoice.SourcePlayer == Player here.
		List<(IGameAction, ActionContext)> options = game.PendingChoice != null
			? game.PendingChoice.GetActions(game).ToList()
			: game.GetValidActions(Player);

		var pendingSet = new PendingActionSet
		{
			Options = options,
			Version = ++_version,
		};
		_currentOptions = pendingSet;
		var pending = new TaskCompletionSource<(IGameAction, ActionContext)>(TaskCreationOptions.RunContinuationsAsynchronously);
		_pending = pending;

		OptionsAvailable?.Invoke(pendingSet);

		var completed = Task.WhenAny(pending.Task, _abandoned.Task).GetAwaiter().GetResult();
		if (completed == _abandoned.Task)
		{
			throw new PlayerAbandonedException();
		}

		return pending.Task.GetAwaiter().GetResult();
	}

	public bool TrySubmit(int actionIndex, int version, out string? error)
	{
		var options = _currentOptions;
		var pending = _pending;

		if (options == null || pending == null)
		{
			error = "No action is currently awaited from this player.";
			return false;
		}

		if (version != options.Version)
		{
			error = "Stale action version.";
			return false;
		}

		if (actionIndex < 0 || actionIndex >= options.Options.Count)
		{
			error = "Action index out of range.";
			return false;
		}

		_currentOptions = null;
		_pending = null;

		error = null;
		return pending.TrySetResult(options.Options[actionIndex]);
	}

	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}
