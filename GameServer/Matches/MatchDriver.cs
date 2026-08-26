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

		void Broadcast(GameState _)
		{
			var newHistory = state.History.Skip(lastHistoryIndex).ToList();
			lastHistoryIndex = state.History.Count;

			BroadcastTo(match, hub, MatchSeat.Player1, newHistory);
			BroadcastTo(match, hub, MatchSeat.Player2, newHistory);
		}

		engine.ActionResolvedCallback = Broadcast;
		match.AgentPlayer1.OptionsAvailable += _ => Broadcast(state);
		match.AgentPlayer2.OptionsAvailable += _ => Broadcast(state);

		engine.StartGame(state);
		Broadcast(state);

		MatchSeat? forfeitedSeat = null;

		while (!state.IsGameOver() && !cancellationToken.IsCancellationRequested && match.AbandonedSeat == null)
		{
			var seat = state.CurrentPlayer == state.Players[0] ? MatchSeat.Player1 : MatchSeat.Player2;
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

		BroadcastEnd(match, hub);
	}

	private static void BroadcastTo(Match match, IHubContext<MatchHub, IMatchClient> hub, MatchSeat seat, List<HistoryEntry> newHistory)
	{
		var connectionId = match.ConnectionFor(seat);
		if (connectionId == null) return;

		var viewer = match.PlayerFor(seat);
		var agent = match.AgentFor(seat);
		var currentOptions = agent.CurrentOptions;

		var view = PlayerViewBuilder.Build(match.GameState, viewer, newHistory, currentOptions?.Options, currentOptions?.Version);
		hub.Clients.Client(connectionId).OnStateUpdated(view);
	}

	private static void BroadcastEnd(Match match, IHubContext<MatchHub, IMatchClient> hub)
	{
		var winnerId = match.GameState.Winner?.Id;

		if (match.ConnectionIdPlayer1 != null)
			hub.Clients.Client(match.ConnectionIdPlayer1).OnMatchEnded(winnerId);
		if (match.ConnectionIdPlayer2 != null)
			hub.Clients.Client(match.ConnectionIdPlayer2).OnMatchEnded(winnerId);
	}
}
