Task ID:
FOUND-006

Model:
haiku

Title:
Create Entities/Sigil.cs

Purpose:
The project is adding a new "Sigil" entity that behaves like the existing `Secret` (hidden, holds triggered effects, lives on `Player`), except a Sigil is never removed when its trigger fires — a `Secret` currently gets an auto-appended removal action when cast (see `SecretAction.Resolve` in `CardBattleEngine\Actions\SecretAction.cs`), but a Sigil should persist indefinitely and can hold more than one triggered effect (each independently configurable as `Unlimited` or `OncePerTurn` via the `Frequency` field added in FOUND-001).

Dependencies:
(none)

Consumes:
`ITriggerSource` interface (already exists, unchanged):
```csharp
public interface ITriggerSource
{
	public IGameEntity Entity { get; }
	public List<TriggeredEffect> TriggeredEffects { get; }
}
```
`TriggeredEffect.Clone()` (already exists, unchanged by this task).

Produces:
- `Sigil` class with an internal `Clone()` method — consumed by ENT-006 (SigilAction.cs), WIRE-002 (Player.cs).

Files:
CardBattleEngine\Entities\Sigil.cs (new file)

Requirements:
Create the file with exactly this content (mirrors `Secret.cs`, but with a `List<TriggeredEffect>` instead of a single trigger, and its own `Clone()` — Secret has no `Clone()` today, but Sigils need one because, unlike Secrets, they must survive being cloned across turns, exactly like Weapons/Minions do):
```csharp
namespace CardBattleEngine;

public class Sigil : ITriggerSource
{
	public List<TriggeredEffect> TriggeredEffects { get; set; } = new();
	public Player Owner { get; set; }
	public IGameEntity Entity => Owner;

	internal Sigil Clone()
	{
		return new Sigil
		{
			Owner = Owner,
			TriggeredEffects = TriggeredEffects.Select(e => e.Clone()).ToList(),
		};
	}
}
```

Constraints:
- Create only this one new file. Do not edit `Secret.cs` or any other existing file.
- Do not give `Sigil` a `Guid Id` or make it implement `IGameEntity` — Sigils are hidden/non-targetable, exactly like `Secret`, and must never appear in `GameState.GetAllEntities()`.

Acceptance Criteria:
- File exists at the exact path above, implementing `ITriggerSource`.
- `Sigil.Clone()` deep-clones `TriggeredEffects` and preserves `Owner`.

Validation:
Confirm this file alone compiles (only depends on pre-existing types). Full solution integration validated by EXIT-001.

Handoff:
ENT-006 and WIRE-002 both reference `Sigil`, `Sigil.TriggeredEffects`, `Sigil.Owner`, and `Sigil.Clone()` by these exact names.
