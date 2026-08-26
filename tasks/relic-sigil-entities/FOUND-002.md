Task ID:
FOUND-002

Model:
haiku

Title:
Add Relic to the CardType enum

Purpose:
The project is adding a new board-permanent card type called "Relic" (shares the board with Minions, has stats/health, can be targeted by spells — but cannot attack or be attacked). Cards are tagged with a `CardType` enum; Relic needs its own value.

Dependencies:
(none)

Consumes:
(none)

Produces:
- `CardType.Relic` enum value — consumed by ENT-003 (RelicCard.cs), WIRE-009 (CardDatabase.cs).

Files:
CardBattleEngine\Enums\CardType.cs

Requirements:
Change:
```csharp
[JsonConverter(typeof(StringEnumConverter))]
public enum CardType { Minion, Spell, Weapon, Hero }
```
to:
```csharp
[JsonConverter(typeof(StringEnumConverter))]
public enum CardType { Minion, Spell, Weapon, Hero, Relic }
```
Do not add a `Sigil` value — Sigils are created inline by spell effects (the same way `Secret` is created via `SecretAction` inside a `SpellCard`), not via their own `CardType`/`Card` subclass.

Constraints:
- Scope this diff to exactly this one file.
- Do not reorder or remove existing enum values — append `Relic` at the end.

Acceptance Criteria:
- `CardType` enum is exactly `{ Minion, Spell, Weapon, Hero, Relic }` in that order.

Validation:
This file alone cannot make the full solution build (consumers land in other tasks in this batch — expected). Confirm the enum change compiles in isolation. Full integration is validated by EXIT-001.

Handoff:
ENT-003 and WIRE-009 both reference `CardType.Relic` by this exact name.
