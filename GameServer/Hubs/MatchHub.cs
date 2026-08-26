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

	public Task<Guid> CreateMatch(DecklistRequest myDeck)
	{
		var matchId = _registry.CreateMatch(Context.ConnectionId, myDeck);
		return Task.FromResult(matchId.Value);
	}

	public Task<JoinResult> JoinMatch(Guid matchId, DecklistRequest myDeck)
	{
		if (!_registry.TryJoinMatch(new MatchId(matchId), Context.ConnectionId, myDeck, out var match, out var error))
		{
			return Task.FromResult(new JoinResult { Success = false, Error = error });
		}

		match!.DriverLoopTask = MatchDriver.Run(match, _hubContext, CancellationToken.None);

		return Task.FromResult(new JoinResult { Success = true });
	}

	public Task<PlayerGameView?> GetState(Guid matchId)
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

	public Task<ActionResult> SubmitAction(Guid matchId, int actionIndex, int version)
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
		_registry.RemoveConnection(Context.ConnectionId);
		return base.OnDisconnectedAsync(exception);
	}
}
