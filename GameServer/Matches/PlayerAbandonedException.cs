namespace GameServer.Matches;

// Thrown out of RemotePlayerAgent.GetNextAction when the seated connection disconnects while the
// driver thread is blocked waiting on that player's action - see MatchDriver.
public class PlayerAbandonedException : Exception
{
}
