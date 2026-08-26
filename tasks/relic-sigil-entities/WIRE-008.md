Task ID:
WIRE-008

Model:
haiku

Title:
Enforce shared board-size cap for Relic in Actions/PlayCardAction.cs

Purpose:
`PlayCardAction.CanCast` already blocks playing a `MinionCard` when the player's `Board` is full (`Card is MinionCard minionCard && player.Board.Count >= state.MaxBoardSize`). The project's new "Relic" card type shares the exact same `Board` list as Minions (see WIRE-002), so it needs the identical check — no arithmetic changes are needed to the existing Minion check since `Board.Count` already reflects both Minions and Relics once they share one list.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `RelicCard` class (produced by ENT-003, running in parallel).
- `Player.Board` (`List<BoardPermanent>`, produced by WIRE-002, running in parallel) and `GameState.MaxBoardSize` (already exists, unchanged).

Produces:
(none — leaf wiring)

Files:
CardBattleEngine\Actions\PlayCardAction.cs

Requirements:
In `PlayCardAction.CanCast`, change:
```csharp
if (Card is MinionCard minionCard && player.Board.Count >= state.MaxBoardSize)
{
	reason = "Board is Full";
	return false;
}

reason = null;
return true;
```
to:
```csharp
if (Card is MinionCard minionCard && player.Board.Count >= state.MaxBoardSize)
{
	reason = "Board is Full";
	return false;
}

if (Card is RelicCard relicCard && player.Board.Count >= state.MaxBoardSize)
{
	reason = "Board is Full";
	return false;
}

reason = null;
return true;
```
The existing `MinionCard` check's right-hand side (`player.Board.Count >= state.MaxBoardSize`) needs NO change — since Relics and Minions now share the exact same `Board` list, `Board.Count` already reflects both kinds of entities without any extra arithmetic.

Constraints:
- Scope this diff to exactly this one file, exactly this one insertion in `CanCast`.
- Do not touch `IsValid` or `Resolve` in this file.
- Use the same `"Board is Full"` message for both checks — it is genuinely the same board.

Acceptance Criteria:
- `CanCast` returns `false` with reason `"Board is Full"` when `Card is RelicCard` and `player.Board.Count >= state.MaxBoardSize`.
- Existing `MinionCard` board-full check is functionally unchanged (still correctly blocked once the board — now shared with Relics — is full).

Validation:
This file references `RelicCard`, created by a sibling task in this same batch, and depends on `Player.Board`'s new shared-type semantics from WIRE-002 — it cannot build alone in isolation (expected). Confirm the insertion matches exactly. Full solution build and behavior validated by EXIT-001.

Handoff:
None — leaf task, but EXIT-001's tests (playing a Relic when the board is full of Minions, and vice versa) exercise this behavior directly.
