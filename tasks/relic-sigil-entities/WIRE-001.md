Task ID:
WIRE-001

Model:
haiku

Title:
Wire Relic into EntityTypeSelector.MatchesType

Purpose:
`EntityTypeSelector` (used by spell/effect target selectors) maps a runtime `IGameEntity` to its `EntityType` flag via a type-pattern `switch`. The project's new "Relic" entity must be targetable by spells, so it needs a case mapping `Relic` to the new `EntityType.Relic` flag.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `EntityType.Relic` (produced by FOUND-003, running in parallel).
- `Relic` class (produced by ENT-002, running in parallel).

Produces:
(none — leaf wiring)

Files:
CardBattleEngine\CastRestictions\EntityTypeSelector.cs

Requirements:
In `EntityTypeSelector.MatchesType`, change:
```csharp
public static bool MatchesType(IGameEntity entity, EntityType entityTypes)
{
	return (entity switch
	{
		Player => EntityType.Player,
		Minion => EntityType.Minion,
		Card => EntityType.Card,
		Weapon => EntityType.Weapon,
		_ => EntityType.None
	} & entityTypes) != 0;
}
```
to:
```csharp
public static bool MatchesType(IGameEntity entity, EntityType entityTypes)
{
	return (entity switch
	{
		Player => EntityType.Player,
		Minion => EntityType.Minion,
		Card => EntityType.Card,
		Weapon => EntityType.Weapon,
		Relic => EntityType.Relic,
		_ => EntityType.None
	} & entityTypes) != 0;
}
```
Note: `Minion` and `Relic` are both `BoardPermanent` subclasses now, but they are still distinct concrete types, so this `switch` pattern-match still works correctly without any other change — `Relic` must be listed as its own case exactly like `Minion` is; do not use `BoardPermanent` here.

Constraints:
- Scope this diff to exactly this one file, exactly this one switch expression.

Acceptance Criteria:
- `Relic` maps to `EntityType.Relic` in the switch. All other cases unchanged.

Validation:
This file references `Relic` and `EntityType.Relic`, created by sibling tasks in this same batch — it cannot build alone in isolation (expected). Full solution build validated by EXIT-001.

Handoff:
None — leaf task with no downstream consumers in this batch.
