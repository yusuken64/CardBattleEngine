using CardBattleEngine.View;
using GameServer.Contracts;
using GameServer.Matches;
using Microsoft.AspNetCore.SignalR;

namespace GameServer.Hubs;

public class MatchHub : Hub<IMatchClient>
{
	private readonly MatchRegistry _registry;
	private readonly IHubContext<MatchHub, IMatchClient> _hubContext;

	public MatchHub(MatchRegistry registry, IHubContext<MatchHub, IMatchClient> hubContext)
	{
		_registry = registry;
		_hubContext = hubContext;
	}

	public Task<string> CreateMatch(DecklistRequest myDeck)
	{
		var matchId = _registry.CreateMatch(Context.ConnectionId, myDeck);
		return Task.FromResult(matchId.Value);
	}

	public Task<JoinResult> JoinMatch(string matchId, DecklistRequest myDeck)
	{
		if (!_registry.TryJoinMatch(new MatchId(matchId), Context.ConnectionId, myDeck, out var match, out var error))
		{
			return Task.FromResult(new JoinResult { Success = false, Error = error });
		}

		match!.DriverLoopTask = MatchDriver.Run(match, _hubContext, CancellationToken.None);

		return Task.FromResult(new JoinResult { Success = true, MatchId = match.Id.Value });
	}

	public Task<PlayerGameView?> GetState(string matchId)
	{
		if (!_registry.TryGet(new MatchId(matchId), out var match))
		{
			return Task.FromResult<PlayerGameView?>(null);
		}

		var seat = match!.SeatOf(Context.ConnectionId);
		if (seat == null)
		{
			return Task.FromResult<PlayerGameView?>(null);
		}

		var agent = match.AgentFor(seat.Value);
		var currentOptions = agent.CurrentOptions;
		var view = PlayerViewBuilder.Build(match.GameState, match.PlayerFor(seat.Value), null, currentOptions?.Options, currentOptions?.Version);
		return Task.FromResult<PlayerGameView?>(view);
	}

	public Task JoinQueue(DecklistRequest myDeck)
	{
		var match = _registry.TryMatchmake(Context.ConnectionId, myDeck);
		if (match != null)
		{
			match.DriverLoopTask = MatchDriver.Run(match, _hubContext, CancellationToken.None);

			_hubContext.Clients.Client(match.ConnectionIdPlayer1!).OnMatchFound(match.Id.Value);
			_hubContext.Clients.Client(match.ConnectionIdPlayer2!).OnMatchFound(match.Id.Value);
		}

		return Task.CompletedTask;
	}

	public Task LeaveQueue()
	{
		_registry.LeaveQueue(Context.ConnectionId);
		return Task.CompletedTask;
	}

	public Task RequestCardArt(string matchId, string cardId)
	{
		if (_registry.TryGet(new MatchId(matchId), out var match))
		{
			var other = match!.OtherConnectionId(Context.ConnectionId);
			if (other != null)
			{
				return _hubContext.Clients.Client(other).OnCardArtRequested(matchId, cardId);
			}
		}

		return Task.CompletedTask;
	}

	public Task SubmitCardArt(string matchId, string cardId, byte[] imageBytes)
	{
		if (_registry.TryGet(new MatchId(matchId), out var match))
		{
			var other = match!.OtherConnectionId(Context.ConnectionId);
			if (other != null)
			{
				return _hubContext.Clients.Client(other).OnCardArtReceived(cardId, imageBytes);
			}
		}

		return Task.CompletedTask;
	}

	public Task<ActionResult> SubmitAction(string matchId, int actionIndex, int version)
	{
		if (!_registry.TryGet(new MatchId(matchId), out var match))
		{
			return Task.FromResult(new ActionResult { Success = false, Error = "No such match." });
		}

		var seat = match!.SeatOf(Context.ConnectionId);
		if (seat == null)
		{
			return Task.FromResult(new ActionResult { Success = false, Error = "This connection is not seated in that match." });
		}

		var agent = match.AgentFor(seat.Value);
		if (!agent.TrySubmit(actionIndex, version, out var error))
		{
			return Task.FromResult(new ActionResult { Success = false, Error = error });
		}

		return Task.FromResult(new ActionResult { Success = true });
	}

	public override Task OnDisconnectedAsync(Exception? exception)
	{
		if (_registry.TryGetByConnection(Context.ConnectionId, out var match))
		{
			var seat = match!.SeatOf(Context.ConnectionId);
			if (seat != null)
			{
				match.AgentFor(seat.Value).Abandon();
			}
		}

		_registry.RemoveConnection(Context.ConnectionId);
		return base.OnDisconnectedAsync(exception);
	}
}
