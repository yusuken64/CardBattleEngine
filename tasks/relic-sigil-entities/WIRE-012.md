Task ID:
WIRE-012

Model:
haiku

Title:
Reject Relic as an attack target in AttackBehaviors/HeroAttackBehavior.cs

Purpose:
Same reasoning as WIRE-011, but for the Hero's own attack (via an equipped weapon or hero attack power): the project's new "Relic" entity now lives in `Player.Board` alongside Minions, so it will be enumerated as a candidate attack target for the Hero too. Relics must never be a valid attack target for the Hero either.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `Relic` class (produced by ENT-002, running in parallel).

Produces:
(none — leaf wiring)

Files:
CardBattleEngine\AttackBehaviors\HeroAttackBehavior.cs

Requirements:
In `HeroAttackBehavior.IsValidAttackTarget`, add a new check right after the "target is null / not alive" check and before the "Cannot attack friendly units" check:
```csharp
public bool IsValidAttackTarget(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
{
	if (target == null || !target.IsAlive)
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
	if (attacker.Owner == target.Owner)
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

	if (!AttackRules.MustAttackTaunt(attacker, target, state))
	{
		reason = "Must Attack Minion with Taunt";
		return false;
	}

	reason = null;
	return true;
}
```
Leave every other method in this file (`MaxAttacks`, `CanInitiateAttack`, `CanAttack`, `GenerateDamageActions`) exactly as-is.

Constraints:
- Scope this diff to exactly this one method, exactly this one new check.

Acceptance Criteria:
- `IsValidAttackTarget` returns `false` with reason `"Relic can't be attacked"` whenever `target is Relic`, regardless of any other condition.
- All other existing rejection/acceptance logic unchanged.

Validation:
This file references `Relic`, created by a sibling task in this same batch — it cannot build alone in isolation (expected). Confirm the new check is present and correctly ordered. Full solution build and behavior validated by EXIT-001.

Handoff:
WIRE-011 adds the identical rejection to `MinionAttackBehavior`. EXIT-001's tests confirm neither a Minion nor the Hero can attack a Relic.
