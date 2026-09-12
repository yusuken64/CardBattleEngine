using CardBattleEngine;
using CardBattleEngine.View;
using GameServer.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace GameServer.Matches;

public static class MatchDriver
{
	public static Task Run(Match match, IHubContext<MatchHub, IMatchClient> hub, CancellationToken cancellationToken)
	{
		return Task.Factory.StartNew(
			() => RunLoop(match, hub, cancellationToken),
			cancellationToken,
			TaskCreationOptions.LongRunning,
			TaskScheduler.Default);
	}

	private static void RunLoop(Match match, IHubContext<MatchHub, IMatchClient> hub, CancellationToken cancellationToken)
	{
		var state = match.GameState;
		var engine = match.Engine;
		int lastHistoryIndex = 0;
		var playback1 = new List<PlaybackEventView>();
		var playback2 = new List<PlaybackEventView>();
		engine.ActionPlaybackCallback = (snapshot, current) =>
		{
			long sequence = ++match.PlaybackSequence;
			playback1.Add(PlayerViewBuilder.BuildPlayback(snapshot, match.PlayerFor(MatchSeat.Player1), current.action, current.context, sequence));
			playback2.Add(PlayerViewBuilder.BuildPlayback(snapshot, match.PlayerFor(MatchSeat.Player2), current.action, current.context, sequence));
		};

		void Broadcast(GameState _)
		{
			var newHistory = state.History.Skip(lastHistoryIndex).ToList();
			lastHistoryIndex = state.History.Count;

			++match.StateRevision;
			BroadcastTo(match, hub, MatchSeat.Player1, newHistory, playback1);
			BroadcastTo(match, hub, MatchSeat.Player2, newHistory, playback2);
			playback1.Clear();
			playback2.Clear();
		}

		engine.ActionResolvedCallback = Broadcast;
		match.AgentPlayer1.OptionsAvailable += _ => Broadcast(state);
		match.AgentPlayer2.OptionsAvailable += _ => Broadcast(state);

		Broadcast(state); // Baseline before any action can create or remove a view.
		engine.StartGame(state);
		Broadcast(state);

		MatchSeat? forfeitedSeat = null;

		while (!state.IsGameOver() && !cancellationToken.IsCancellationRequested && match.AbandonedSeat == null)
		{
			var activePlayer = state.PendingChoice?.SourcePlayer ?? state.CurrentPlayer;
			var seat = activePlayer == state.Players[0] ? MatchSeat.Player1 : MatchSeat.Player2;
			var agent = match.AgentFor(seat);

			try
			{
				var (action, context) = agent.GetNextAction(state);
				engine.Resolve(state, context, action);
			}
			catch (PlayerAbandonedException)
			{
				forfeitedSeat = seat;
			}
		}

		forfeitedSeat ??= match.AbandonedSeat;
		if (forfeitedSeat != null)
		{
			state.Winner = match.PlayerFor(forfeitedSeat == MatchSeat.Player1 ? MatchSeat.Player2 : MatchSeat.Player1);
		}

		Broadcast(state); // Includes forfeit state, even when no EndGameAction ran.
		BroadcastEnd(match, hub);
	}

	private static void BroadcastTo(Match match, IHubContext<MatchHub, IMatchClient> hub, MatchSeat seat, List<HistoryEntry> newHistory, List<PlaybackEventView> playback)
	{
		var connectionId = match.ConnectionFor(seat);

		var viewer = match.PlayerFor(seat);
		var agent = match.AgentFor(seat);
		var currentOptions = agent.CurrentOptions;

		var view = PlayerViewBuilder.Build(match.GameState, viewer, newHistory,
			currentOptions?.Options ?? new List<(IGameAction, ActionContext)>(), currentOptions?.Version);
		view.StateRevision = match.StateRevision;
		view.PlaybackSequence = match.PlaybackSequence;
		view.PlaybackEvents = playback.ToList();
		match.PublishView(seat, view);
		// Dedicated driver thread: wait for sends (not animation acknowledgements), preserving order
		// and propagating transport failures to DriverLoopTask rather than losing fire-and-forget tasks.
		if (connectionId != null) hub.Clients.Client(connectionId).OnStateUpdated(view).GetAwaiter().GetResult();
	}

	private static void BroadcastEnd(Match match, IHubContext<MatchHub, IMatchClient> hub)
	{
		var winnerId = match.GameState.Winner?.Id;

		if (match.ConnectionIdPlayer1 != null)
			hub.Clients.Client(match.ConnectionIdPlayer1).OnMatchEnded(winnerId).GetAwaiter().GetResult();
		if (match.ConnectionIdPlayer2 != null)
			hub.Clients.Client(match.ConnectionIdPlayer2).OnMatchEnded(winnerId).GetAwaiter().GetResult();
	}
}
