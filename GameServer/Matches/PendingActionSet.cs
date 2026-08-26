using CardBattleEngine;

namespace GameServer.Matches;

// The exact list of (IGameAction, ActionContext) instances offered to a player for one prompt.
// Must be the SAME instances GameEngine will later resolve - GameEngine.IsAllowedChoice matches
// pending-choice submissions by reference equality, so a client can only ever pick an index into
// this snapshot, never submit a reconstructed action.
public class PendingActionSet
{
	public required IReadOnlyList<(IGameAction Action, ActionContext Context)> Options { get; init; }
	public required int Version { get; init; }
}
