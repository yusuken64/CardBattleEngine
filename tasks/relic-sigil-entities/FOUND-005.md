Task ID:
FOUND-005

Model:
haiku

Title:
Create Entities/RelicAbility.cs

Purpose:
The project's new "Relic" board entity can carry an activated ability, usable at most once per turn — exactly like the existing Hero Power. This task creates the data-holder class for that ability, mirroring the existing `HeroPower` class (`CardBattleEngine\Entities\HeroPower.cs`) exactly, under a new name scoped to Relics.

Dependencies:
(none)

Consumes:
Existing types (unchanged): `IValidTargetSelector`, `ICastRestriction`, `IAffectedEntitySelector`, `IGameAction`.

Produces:
- `RelicAbility` class — consumed by ENT-002 (Relic.cs), ENT-003 (RelicCard.cs), ENT-005 (RelicAbilityAction.cs), WIRE-004 (StartTurnAction.cs).

Files:
CardBattleEngine\Entities\RelicAbility.cs (new file)

Requirements:
Create the file with exactly this content (direct mirror of `HeroPower.cs`, renamed):
```csharp
namespace CardBattleEngine;

public class RelicAbility
{
	public string Name { get; set; }
	public int ManaCost { get; set; }
	public bool UsedThisTurn { get; set; } = false;
	public IValidTargetSelector? ValidTargetSelector { get; set; }
	public ICastRestriction? CastRestriction { get; set; }
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public IEnumerable<IGameAction> GameActions { get; set; }
}
```

Constraints:
- Create only this one new file. Do not edit `HeroPower.cs` or any other existing file.
- Do not add a `Charges` field here — `Charges` belongs on the `Relic` entity itself, not the ability.

Acceptance Criteria:
- File exists at the exact path above, in namespace `CardBattleEngine`, with exactly the properties listed.

Validation:
Confirm this file alone compiles (only references pre-existing interfaces). Full solution integration validated by EXIT-001.

Handoff:
ENT-002, ENT-003, ENT-005, and WIRE-004 all reference `RelicAbility` and its members by these exact names.
