using CardBattleEngine;

namespace GameServer.Matches;

// Owns one GameState + GameEngine for the lifetime of a match, and the mapping from SignalR
// ConnectionIds to seats. All mutation of GameState happens exclusively on DriverLoopTask's thread
// (see MatchDriver) - everything else here is read-only bookkeeping safe to touch from Hub calls.
public class Match
{
	public MatchId Id { get; }
	public GameState GameState { get; }
	public GameEngine Engine { get; }
	public RemotePlayerAgent AgentPlayer1 { get; }
	public RemotePlayerAgent AgentPlayer2 { get; }

	public string? ConnectionIdPlayer1 { get; set; }
	public string? ConnectionIdPlayer2 { get; set; }

	public Task? DriverLoopTask { get; set; }

	public Match(MatchId id, GameState gameState, GameEngine engine)
	{
		Id = id;
		GameState = gameState;
		Engine = engine;
		AgentPlayer1 = new RemotePlayerAgent(gameState.Players[0]);
		AgentPlayer2 = new RemotePlayerAgent(gameState.Players[1]);
	}

	public bool IsFull => ConnectionIdPlayer1 != null && ConnectionIdPlayer2 != null;

	public MatchSeat? SeatOf(string connectionId)
	{
		if (connectionId == ConnectionIdPlayer1) return MatchSeat.Player1;
		if (connectionId == ConnectionIdPlayer2) return MatchSeat.Player2;
		return null;
	}

	public Player PlayerFor(MatchSeat seat) =>
		seat == MatchSeat.Player1 ? GameState.Players[0] : GameState.Players[1];

	public RemotePlayerAgent AgentFor(MatchSeat seat) =>
		seat == MatchSeat.Player1 ? AgentPlayer1 : AgentPlayer2;

	public string? ConnectionFor(MatchSeat seat) =>
		seat == MatchSeat.Player1 ? ConnectionIdPlayer1 : ConnectionIdPlayer2;
}
