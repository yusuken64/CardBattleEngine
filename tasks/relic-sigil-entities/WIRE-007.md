Task ID:
WIRE-007

Model:
haiku

Title:
Remove dying Relics from Player.Board in Actions/DeathAction.cs

Purpose:
`DeathAction` is the single place that handles an entity's death — for a `Player` it ends the game, for a `Minion` it moves it from `Board` to `Graveyard` and fires Deathrattles/Reborn. The project's new "Relic" entity can die two ways: from taking lethal damage via a spell (Relics are targetable by spells — `DamageAction` already yields a generic `DeathAction` whenever any `IGameEntity.Health <= 0`, so Relics get this for free), or from running out of `Charges` (WIRE-006 yields a `DeathAction` targeting the Relic). Either way, `DeathAction` needs a branch to remove the Relic from `Player.Board` (the shared board it lives in alongside Minions — there is no separate Relic list). No deathrattle/graveyard equivalent — Relics just disappear.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `Relic` class (produced by ENT-002, running in parallel) — trust it has a public `Owner` (`Player`) property.
- `Player.Board` (`List<BoardPermanent>`, produced by WIRE-002, running in parallel).

Produces:
(none — leaf wiring)

Files:
CardBattleEngine\Actions\DeathAction.cs

Requirements:
Add a new `if` branch right after the existing `if (actionContext.Target is Minion minion) { ... }` block (which handles graveyard/deathrattle/reborn — note that block's `minion.Owner.Board.Remove(minion)` line needs NO change; `Board.Remove(minion)` still compiles fine against `List<BoardPermanent>` since `Minion` is a `BoardPermanent`) and before the closing brace of the method:
```csharp
if (actionContext.Target is Relic relic)
{
	relic.Owner.Board.Remove(relic);
}
```
Leave the `actionContext.Target.IsAlive = false;` line, the `Player` branch, and the entire `Minion` branch exactly as-is — this is a pure addition of one new sibling `if` block.

Constraints:
- Scope this diff to exactly this one file, exactly this one insertion.
- Do not add Deathrattle- or Reborn-equivalent logic for Relics — just remove it from `Board`.

Acceptance Criteria:
- When a `DeathAction` resolves with `actionContext.Target` being a `Relic`, that Relic is removed from `relic.Owner.Board`.
- No other branch's behavior changed (confirm the existing `Minion` branch's `Board.Remove(minion)` call still compiles unchanged against the new `List<BoardPermanent>` type).

Validation:
This file references `Relic` and `Player.Board`'s new type, created/changed by sibling tasks in this same batch — it cannot build alone in isolation (expected). Confirm the new `if` block is present and correctly placed. Full solution build and behavior validated by EXIT-001.

Handoff:
None — leaf task, but EXIT-001's tests (Relic dies from spell damage, Relic dies from Charges reaching zero) exercise this behavior directly.
