Task ID:
FOUND-004

Model:
haiku

Title:
Create AttackBehaviors/InertAttackBehavior.cs

Purpose:
The project's new "Relic" board entity can never attack and can never be attacked in combat (it can still be targeted by spells/damage — just not by the attack system). Every combat-capable entity exposes an `IAttackBehavior AttackBehavior` property (see `Minion`, which uses `MinionAttackBehavior`). This task creates a no-op `IAttackBehavior` implementation for Relic to use.

Dependencies:
(none)

Consumes:
`IAttackBehavior` interface (already exists, unchanged) at `CardBattleEngine\Interfaces\IAttackBehavior.cs`:
```csharp
public interface IAttackBehavior
{
	public int MaxAttacks(IGameEntity attacker);
	bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state, out string reason);
	IEnumerable<(IGameAction, ActionContext)> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state);
	bool CanInitiateAttack(IGameEntity attacker, out string reason);
	bool IsValidAttackTarget(IGameEntity attacker, IGameEntity target, GameState state, out string reason);
}
```

Produces:
- `InertAttackBehavior` class with a static `Instance` singleton — consumed by ENT-002 (Relic.cs), ENT-003 (RelicCard.cs).

Files:
CardBattleEngine\AttackBehaviors\InertAttackBehavior.cs (new file)

Requirements:
Create the file with exactly this content:
```csharp
namespace CardBattleEngine;

public class InertAttackBehavior : IAttackBehavior
{
	public static readonly InertAttackBehavior Instance = new();

	public int MaxAttacks(IGameEntity attacker) => 0;

	public bool CanInitiateAttack(IGameEntity attacker, out string reason)
	{
		reason = null;
		return false;
	}

	public bool IsValidAttackTarget(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
	{
		reason = null;
		return false;
	}

	public bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
	{
		reason = null;
		return false;
	}

	public IEnumerable<(IGameAction, ActionContext)> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state)
	{
		return Enumerable.Empty<(IGameAction, ActionContext)>();
	}
}
```
Match the existing file style used in `CardBattleEngine\AttackBehaviors\MinionAttackBehavior.cs`/`HeroAttackBehavior.cs` (same namespace, same directory).

Constraints:
- Create only this one new file. Do not edit any existing file.

Acceptance Criteria:
- File exists at the exact path above, implementing `IAttackBehavior`, with a public static readonly `Instance` singleton, every method always returning a "can't attack / can't be attacked" result.

Validation:
Confirm this file alone compiles as a syntactically valid C# class implementing the existing `IAttackBehavior` interface (no dependency on any other new type in this batch). Full solution integration is validated by EXIT-001.

Handoff:
ENT-002 (Relic.cs) and ENT-003 (RelicCard.cs) both reference `InertAttackBehavior.Instance` by this exact name.
