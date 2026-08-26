using CardBattleEngine.View;

namespace GameServer.Hubs;

// Strongly-typed push contract from server to a connected client (Hub<IMatchClient>).
public interface IMatchClient
{
	Task OnStateUpdated(PlayerGameView view);
	Task OnMatchEnded(Guid? winnerPlayerId);
	Task OnActionRejected(string reason);
}
