Task ID:
WIRE-010

Model:
haiku

Title:
Fix AttackRules.MustAttackTaunt for the new Board type in Actions/AttackAction.cs

Purpose:
`Player.Board` is changing from `List<Minion>` to `List<BoardPermanent>` (WIRE-002), shared with the project's new `Relic` entity. `AttackRules.MustAttackTaunt` (in `CardBattleEngine\Actions\AttackAction.cs`) reads `.Taunt`, a Minion-only member, directly off `Board` elements — that stops compiling once `Board` can contain Relics (which have no `Taunt` property). This needs an `OfType<Minion>()` filter; Relics can never have Taunt, so this is also correct behaviorally, not just a compile fix.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `Player.Board` retyped to `List<BoardPermanent>` (produced by WIRE-002, running in parallel).

Produces:
(none — leaf wiring)

Files:
CardBattleEngine\Actions\AttackAction.cs

Requirements:
In `AttackRules.MustAttackTaunt`, change:
```csharp
public static bool MustAttackTaunt(IGameEntity attacker, IGameEntity target, GameState state)
{
	// Find opponent's taunt minions
	var opponent = state.OpponentOf(attacker.Owner);
	var taunts = opponent.Board.Where(m => m.Taunt && m.IsAlive && !m.IsStealth);

	// If there are taunts, you must target one
	return !taunts.Any() || (target is Minion t && t.Taunt);
}
```
to:
```csharp
public static bool MustAttackTaunt(IGameEntity attacker, IGameEntity target, GameState state)
{
	// Find opponent's taunt minions
	var opponent = state.OpponentOf(attacker.Owner);
	var taunts = opponent.Board.OfType<Minion>().Where(m => m.Taunt && m.IsAlive && !m.IsStealth);

	// If there are taunts, you must target one
	return !taunts.Any() || (target is Minion t && t.Taunt);
}
```
Leave `AttackAction`'s `IsValid`/`Resolve` methods in the same file exactly as-is — they pattern-match on runtime types (`context.Source is Minion minion`, `context.Source is Player player`), which is unaffected by `Board`'s declared element type.

Constraints:
- Scope this diff to exactly this one method, exactly this one line change (`opponent.Board.Where(...)` → `opponent.Board.OfType<Minion>().Where(...)`).

Acceptance Criteria:
- `MustAttackTaunt` only considers actual `Minion` instances when checking for Taunt — a `Relic` on the board never forces or satisfies a "must attack Taunt" requirement.

Validation:
This file depends on `Player.Board`'s new element type from WIRE-002, a sibling task in this same batch — it cannot build alone in isolation (expected). Confirm the one-line change is present. Full solution build and behavior validated by EXIT-001.

Handoff:
None — leaf task, but EXIT-001's tests (a Relic on the board doesn't interfere with Taunt-forced-attack rules) can exercise this behavior.
