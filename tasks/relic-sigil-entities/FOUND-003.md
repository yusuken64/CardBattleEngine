Task ID:
FOUND-003

Model:
haiku

Title:
Add Relic flag to the EntityType enum

Purpose:
Spell target selectors (like `EntityTypeSelector`) use the `[Flags] EntityType` enum to decide what kinds of entities a spell/effect can target. The project's new "Relic" board entity must be a valid target for spells, so it needs its own flag alongside `Player`, `Minion`, `Card`, `Weapon`.

Dependencies:
(none)

Consumes:
(none)

Produces:
- `EntityType.Relic` flag value (`1 << 4`) — consumed by WIRE-001 (EntityTypeSelector.cs).

Files:
CardBattleEngine\CastRestictions\IValidTargetSelector.cs

Requirements:
Change:
```csharp
[Flags]
public enum EntityType
{
	None = 0,
	Player = 1 << 0,
	Minion = 1 << 1,
	Card = 1 << 2,
	Weapon = 1 << 3
}
```
to:
```csharp
[Flags]
public enum EntityType
{
	None = 0,
	Player = 1 << 0,
	Minion = 1 << 1,
	Card = 1 << 2,
	Weapon = 1 << 3,
	Relic = 1 << 4
}
```
Do not add a `Sigil` flag — Sigils are hidden, non-targetable entities (like `Secret`, which also has no `EntityType` flag).

Constraints:
- Scope this diff to exactly this one file.
- Do not change any existing flag's bit value.

Acceptance Criteria:
- `EntityType.Relic` exists with value `1 << 4` (16). All existing values unchanged.

Validation:
This file alone cannot make the full solution build (the consumer lands in WIRE-001, a separate task in this batch — expected). Full integration validated by EXIT-001.

Handoff:
WIRE-001 references `EntityType.Relic` by this exact name.
