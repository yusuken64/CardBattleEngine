Task ID:
WIRE-011

Model:
haiku

Title:
Reject Relic as an attack target in AttackBehaviors/MinionAttackBehavior.cs

Purpose:
The project's new "Relic" entity now lives in `Player.Board` alongside Minions (WIRE-002), which means it will be enumerated as a candidate attack target anywhere code iterates the opponent's `Board` looking for defenders (e.g. `GameState.GetValidActions`'s `foreach (var defender in OpponentOf(player).Board)` loop — that loop needs NO change itself, because it already gates every candidate through `AttackAction.IsValid`, which delegates to `IAttackBehavior.CanAttack` → `IsValidAttackTarget`). Relics must never be a valid attack target (they can be damaged by spells, just not attacked) — today nothing rejects this because Relics never existed in `Board` before. This task adds that explicit rejection to `MinionAttackBehavior.IsValidAttackTarget` (used when a Minion is the attacker).

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `Relic` class (produced by ENT-002, running in parallel).

Produces:
(none — leaf wiring, but establishes the "Relic can't be attacked" behavior EXIT-001's tests will check)

Files:
CardBattleEngine\AttackBehaviors\MinionAttackBehavior.cs

Requirements:
In `MinionAttackBehavior.IsValidAttackTarget`, add a new check right after the "Target must be alive" check and before the "Cannot attack friendly units" check:
```csharp
public bool IsValidAttackTarget(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
{
	var minion = (Minion)attacker;

	// Target must be alive
	if (!target.IsAlive)
	{
		reason = null;
		return false;
	}

	// Relics can't be attacked
	if (target is Relic)
	{
		reason = "Relic can't be attacked";
		return false;
	}

	// Cannot attack friendly units
	if (minion.Owner == target.Owner)
	{
		reason = null;
		return false;
	}

	// Cannot attack stealth minions
	if (target is Minion tminion && tminion.IsStealth)
	{
		reason = "Target is Stealthed";
		return false;
	}

	// Rush restriction (only on first turn)
	if (minion.HasSummoningSickness && minion.HasRush && target is not Minion)
	{
		reason = "Must Attack another minion";
		return false;
	}

	// Taunt rules (board scan)
	if (!AttackRules.MustAttackTaunt(minion, target, state))
	{
		reason = "Must Attack Minion with Taunt";
		return false;
	}

	reason = null;
	return true;
}
```
Leave every other method in this file (`CanInitiateAttack`, `CanAttack`, `GenerateDamageActions`, `MaxAttacks`) exactly as-is.

Constraints:
- Scope this diff to exactly this one method, exactly this one new check.
- Note the pre-existing "Rush restriction" check (`target is not Minion`) already technically excludes Relics from Rush-eligible targets, but the new check above must come BEFORE it so a non-Rush attacker still gets rejected with a clear reason.

Acceptance Criteria:
- `IsValidAttackTarget` returns `false` with reason `"Relic can't be attacked"` whenever `target is Relic`, regardless of any other condition.
- All other existing rejection/acceptance logic unchanged.

Validation:
This file references `Relic`, created by a sibling task in this same batch — it cannot build alone in isolation (expected). Confirm the new check is present and correctly ordered. Full solution build and behavior validated by EXIT-001.

Handoff:
WIRE-012 adds the identical rejection to `HeroAttackBehavior` (for Hero attacks). EXIT-001's tests confirm neither a Minion nor the Hero can attack a Relic.
