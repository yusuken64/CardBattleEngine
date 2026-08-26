Task ID:
WIRE-006

Model:
haiku

Title:
Mark OncePerTurn effects as used, and decrement Relic Charges, in Actions/TriggerEffectAction.cs

Purpose:
`TriggerEffectAction` is the passthrough action that actually executes a fired `TriggeredEffect`'s `GameActions`. Two new behaviors need to happen here, at the moment an effect actually fires:
1. If the effect is `EffectFrequency.OncePerTurn`, mark it `UsedThisTurn = true` so `EventBus.GetTriggers` (WIRE-005) won't fire it again until the next turn reset (WIRE-004).
2. If the firing trigger source is a `Relic` with a limited number of `Charges` (e.g. "the next 3 times a minion attacks, do X"), decrement its charge count, and destroy the Relic once it reaches zero.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `EffectFrequency.OncePerTurn`, `TriggeredEffect.Frequency`, `TriggeredEffect.UsedThisTurn` (produced by FOUND-001, running in parallel).
- `Relic` class (produced by ENT-002, running in parallel) — trust it has a public `int? Charges` property and a public `Owner` (`Player`) property.
- `DeathAction` (already exists, unchanged by this task) — WIRE-007 (running in parallel) adds the branch that removes a dying `Relic` from `Player.Board` when this action's yielded `DeathAction` resolves.

Produces:
- Updated `TriggerEffectAction.Resolve` — consumed by EXIT-001 (tests).

Files:
CardBattleEngine\Actions\TriggerEffectAction.cs

Requirements:
Change `TriggerEffectAction.Resolve` from:
```csharp
public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
{
	if (TriggeredEffect.AffectedEntitySelector == null) yield break;

	foreach (var action in TriggeredEffect.GameActions)
	{
		yield return (action, new ActionContext
		{
			Source = TriggerSource.Entity,
			SourcePlayer = TriggerSource.Entity.Owner,
			AffectedEntitySelector = TriggeredEffect.AffectedEntitySelector,
			Target = context.Target,
			OriginalAction = context.OriginalAction,
			OriginalSource = context.Source,
		});
	}
}
```
to:
```csharp
public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
{
	if (TriggeredEffect.AffectedEntitySelector == null) yield break;

	foreach (var action in TriggeredEffect.GameActions)
	{
		yield return (action, new ActionContext
		{
			Source = TriggerSource.Entity,
			SourcePlayer = TriggerSource.Entity.Owner,
			AffectedEntitySelector = TriggeredEffect.AffectedEntitySelector,
			Target = context.Target,
			OriginalAction = context.OriginalAction,
			OriginalSource = context.Source,
		});
	}

	if (TriggeredEffect.Frequency == EffectFrequency.OncePerTurn)
	{
		TriggeredEffect.UsedThisTurn = true;
	}

	if (TriggerSource is Relic relic && relic.Charges.HasValue)
	{
		relic.Charges--;
		if (relic.Charges <= 0)
		{
			yield return (new DeathAction(), new ActionContext
			{
				SourcePlayer = relic.Owner,
				Source = relic,
				Target = relic,
			});
		}
	}
}
```
Leave `IsValid` and every other member of this file unchanged.

Constraints:
- Scope this diff to exactly this one file, exactly this one method body.
- The charge decrement and `UsedThisTurn` marking must happen only once per actual fire of this `TriggerEffectAction` instance (inside `Resolve`, not `IsValid`, and not inside the `foreach` over `GameActions`).

Acceptance Criteria:
- After `Resolve` runs on a `OncePerTurn` effect, `TriggeredEffect.UsedThisTurn` is `true`.
- After `Resolve` runs on a `TriggerEffectAction` whose `TriggerSource` is a `Relic` with `Charges.HasValue`, `Charges` is decremented by exactly 1.
- When `Charges` reaches `0` or below, a `DeathAction` targeting that `Relic` is yielded.
- A `Relic` with `Charges == null` never has its charges touched and is never auto-destroyed by this logic.

Validation:
This file references `EffectFrequency` and `Relic`, created by sibling tasks in this same batch — it cannot build alone in isolation (expected). Confirm the code above is transcribed exactly. Full solution build and behavior validated by EXIT-001.

Handoff:
WIRE-007 (DeathAction.cs) must handle `actionContext.Target is Relic` for the `DeathAction` yielded here to actually remove the Relic from play — both tasks are required together; EXIT-001's Relic Charges test validates the combination.
