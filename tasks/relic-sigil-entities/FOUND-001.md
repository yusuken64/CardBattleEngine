Task ID:
FOUND-001

Model:
haiku

Title:
Add EffectFrequency enum, Frequency/UsedThisTurn fields, and OnRelicAbility trigger to Entities/TriggeredEffect.cs

Purpose:
This project is adding two new card entity types: "Relic" (a minion-like board permanent that shares the board with Minions but can't attack or be attacked, can hold an activated ability, and can have a limited number of Charges) and "Sigil" (a secret-like hidden trigger that, unlike a Secret, is not removed when it fires). Both need triggered effects configurable to fire either every time their condition is met ("Unlimited", today's implicit behavior) or at most once per turn ("OncePerTurn"). This task adds that configuration to the shared `TriggeredEffect` class.

Dependencies:
(none)

Consumes:
(none)

Produces:
- `EffectFrequency` enum (`Unlimited`, `OncePerTurn`) — consumed by WIRE-004 (StartTurnAction), WIRE-005 (EventBus), WIRE-006 (TriggerEffectAction), EXIT-001 (tests).
- `TriggeredEffect.Frequency` (`EffectFrequency`, default `Unlimited`) and `TriggeredEffect.UsedThisTurn` (`bool`, default `false`) — same consumers as above.
- `EffectTrigger.OnRelicAbility` enum value — consumed by ENT-005 (RelicAbilityAction.cs).

Files:
CardBattleEngine\Entities\TriggeredEffect.cs

Requirements:
1. Add a new enum near the other enums in this file:
```csharp
[JsonConverter(typeof(StringEnumConverter))]
public enum EffectFrequency
{
	Unlimited,
	OncePerTurn,
}
```
2. On the `TriggeredEffect` class, add two new properties:
```csharp
public EffectFrequency Frequency { get; set; } = EffectFrequency.Unlimited;
public bool UsedThisTurn { get; set; } = false;
```
3. Update `TriggeredEffect.Clone()` to copy both new fields (this preserves exact clone fidelity for AI game-tree search, matching how every other field on this class is already copied):
```csharp
internal TriggeredEffect Clone()
{
	return new TriggeredEffect()
	{
		EffectTiming = this.EffectTiming,
		EffectTrigger = this.EffectTrigger,
		Scope = this.Scope,
		AffectedEntitySelector = AffectedEntitySelector,
		Condition = Condition,
		GameActions = GameActions.Select(a => a.Clone()).ToList(),
		Frequency = this.Frequency,
		UsedThisTurn = this.UsedThisTurn,
	};
}
```
4. Add `OnRelicAbility` as a new value in the existing `EffectTrigger` enum (add it at the end of the list, right after `GameEnd,` and before the closing `}`).

Constraints:
- Scope this diff to exactly this one file.
- Do not remove or rename any existing enum value or property — this is a pure addition.

Acceptance Criteria:
- `EffectFrequency` enum exists with exactly two values: `Unlimited`, `OncePerTurn`.
- `TriggeredEffect.Frequency` and `TriggeredEffect.UsedThisTurn` exist with the defaults specified above.
- `TriggeredEffect.Clone()` copies both new fields.
- `EffectTrigger.OnRelicAbility` exists.

Validation:
This file alone cannot make the full solution build (other tasks in this batch add the types/files that reference these new members — that's expected). Confirm the file still parses as valid C# and that no existing member was altered. Full solution build is validated by EXIT-001 once every task in this batch lands.

Handoff:
Every other task in this batch that reads or sets `Frequency`, `UsedThisTurn`, or `EffectTrigger.OnRelicAbility` depends on the exact names and types defined here — do not rename anything after this lands without updating this file's "Produces" section.
