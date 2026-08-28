Task ID:
WIRE-004

Model:
haiku

Title:
Reset Relic ability and OncePerTurn TriggeredEffect flags in Actions/StartTurnAction.cs, and fix the per-minion reset loop for the new Board type

Purpose:
`StartTurnAction` is the single place that resets all per-turn state (Hero Power's `UsedThisTurn`, minions' `AttacksPerformedThisTurn`, freeze flags). Two things need to change here:
1. `Player.Board` is changing from `List<Minion>` to `List<BoardPermanent>` (WIRE-002), shared with the new `Relic` entity. The EXISTING loop `foreach (var minion in actionContext.SourcePlayer.Board) { minion.AttacksPerformedThisTurn = 0; ... }` reads Minion-only members (`AttacksPerformedThisTurn`, `HasSummoningSickness`, `IsFrozen`, `MissedAttackFromFrozen`) that don't exist on `BoardPermanent` — this stops compiling once `Board`'s element type changes, and must be fixed with an `OfType<Minion>()` filter.
2. Two new per-turn resets need to be added: a Relic's activated ability (`RelicAbility.UsedThisTurn`, mirroring Hero Power exactly) and any `TriggeredEffect` configured as `EffectFrequency.OncePerTurn` (which can appear on any trigger source — Sigils, Relics, Minions, Weapons, Cards, or the Player itself).

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `Player.Board` retyped to `List<BoardPermanent>` (produced by WIRE-002, running in parallel).
- `Relic.Ability` (`RelicAbility`), `RelicAbility.UsedThisTurn` (produced by ENT-002/FOUND-005, running in parallel).
- `EffectFrequency.OncePerTurn`, `TriggeredEffect.Frequency`, `TriggeredEffect.UsedThisTurn` (produced by FOUND-001, running in parallel).
- `GameState.GetAllTriggerSources()` (extended by WIRE-003, running in parallel, to also yield Sigils — trust it yields every trigger source belonging to every player, including Relics via `Board`, once the whole batch lands).
- `ITriggerSource.Entity` / `IGameEntity.Owner` (already exist, unchanged) — for any `ITriggerSource`, `source.Entity.Owner` resolves to the owning `Player`.

Produces:
(none — leaf wiring)

Files:
CardBattleEngine\Actions\StartTurnAction.cs

Requirements:
1. Change the existing per-minion reset loop from:
```csharp
// Reset attack flags
foreach (var minion in actionContext.SourcePlayer.Board)
{
	minion.AttacksPerformedThisTurn = 0;
	minion.HasSummoningSickness = false;

	if (minion.IsFrozen &&
		minion.MissedAttackFromFrozen)
	{
		minion.IsFrozen = false;
		minion.MissedAttackFromFrozen = false;
		actionContext.ResolvedStatusChanges.Add(
			new StatusDelta(minion, StatusType.Freeze, false));
	}
	else if (minion.IsFrozen &&
		!minion.MissedAttackFromFrozen)
	{
		minion.MissedAttackFromFrozen = true;
	}
}
```
to:
```csharp
// Reset attack flags
foreach (var minion in actionContext.SourcePlayer.Board.OfType<Minion>())
{
	minion.AttacksPerformedThisTurn = 0;
	minion.HasSummoningSickness = false;

	if (minion.IsFrozen &&
		minion.MissedAttackFromFrozen)
	{
		minion.IsFrozen = false;
		minion.MissedAttackFromFrozen = false;
		actionContext.ResolvedStatusChanges.Add(
			new StatusDelta(minion, StatusType.Freeze, false));
	}
	else if (minion.IsFrozen &&
		!minion.MissedAttackFromFrozen)
	{
		minion.MissedAttackFromFrozen = true;
	}
}
```
(the only change is `.Board` → `.Board.OfType<Minion>()` in the `foreach` line — every line inside the loop body is unchanged).
2. Right after the existing block:
```csharp
if (player.HeroPower != null)
{
	player.HeroPower.UsedThisTurn = false;
}
```
add:
```csharp
foreach (var relic in player.Board.OfType<Relic>())
{
	if (relic.Ability != null)
	{
		relic.Ability.UsedThisTurn = false;
	}
}

foreach (var source in state.GetAllTriggerSources().Where(s => s.Entity.Owner == player))
{
	foreach (var effect in source.TriggeredEffects)
	{
		if (effect.Frequency == EffectFrequency.OncePerTurn)
		{
			effect.UsedThisTurn = false;
		}
	}
}
```

Constraints:
- Scope this diff to exactly this one file, exactly these two insertion points.
- Only reset flags belonging to the player whose turn is starting (`player` = `actionContext.SourcePlayer`) — the `.Where(s => s.Entity.Owner == player)` filter is required, do not remove it.

Acceptance Criteria:
- The per-minion reset loop only iterates actual `Minion` instances on the board (skips Relics), with identical behavior to before for Minions.
- Every `Relic.Ability.UsedThisTurn` belonging to `player` is reset to `false` at the start of that player's turn.
- Every `TriggeredEffect.UsedThisTurn` with `Frequency == EffectFrequency.OncePerTurn`, belonging to any trigger source owned by `player`, is reset to `false` at the start of that player's turn. Opponent's flags are untouched.

Validation:
This file references `Relic`, `EffectFrequency`, and `TriggeredEffect.Frequency`/`UsedThisTurn`, created by sibling tasks in this same batch, and relies on `Player.Board`'s new element type (WIRE-002) and `GetAllTriggerSources()` yielding Sigils (WIRE-003) — it cannot build or behave correctly alone in isolation (expected). Confirm both insertions are present and correctly placed. Full solution build and behavior validated by EXIT-001.

Handoff:
None — leaf task, but EXIT-001's tests (Relic ability reset, Sigil OncePerTurn re-arming across turns) exercise this behavior directly.
