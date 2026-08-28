Task ID:
WIRE-005

Model:
haiku

Title:
Skip already-fired OncePerTurn effects in Engine/EventBus.cs GetTriggers

Purpose:
`EventBus.GetTriggers` is the query that finds every `TriggeredEffect` matching a firing action, across every trigger source in the game. The project added an `EffectFrequency` concept (`Unlimited` vs `OncePerTurn`) to `TriggeredEffect` — this task makes `GetTriggers` respect it by excluding effects that are `OncePerTurn` and have already fired this turn (`UsedThisTurn == true`). This is unrelated to the `Player.Board` type change happening elsewhere in this batch — `GetTriggers`' "Modifier-triggered effects" loop already calls `gameState.GetAllMinions()`, whose signature and Minion-only contents are preserved by WIRE-003, so no board-related change is needed in this file.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `EffectFrequency.OncePerTurn`, `TriggeredEffect.Frequency`, `TriggeredEffect.UsedThisTurn` (produced by FOUND-001, running in parallel).

Produces:
- Updated trigger-filtering behavior in `GetTriggers` — consumed by WIRE-006 (TriggerEffectAction.cs, which flips `UsedThisTurn` to `true` once a `OncePerTurn` effect fires), EXIT-001.

Files:
CardBattleEngine\Engine\EventBus.cs

Requirements:
In `EventBus.GetTriggers`, change the `.Where(...)` predicate inside the "Normal card effects" loop from:
```csharp
foreach (var effect in triggerSource.TriggeredEffects
	.Where(te => te.EffectTrigger == triggeringAction.EffectTrigger &&
				 te.EffectTiming == timing &&
				 (te.Scope != TriggerScope.Self || triggerSource.Entity == context.Source)))
```
to:
```csharp
foreach (var effect in triggerSource.TriggeredEffects
	.Where(te => te.EffectTrigger == triggeringAction.EffectTrigger &&
				 te.EffectTiming == timing &&
				 (te.Scope != TriggerScope.Self || triggerSource.Entity == context.Source) &&
				 (te.Frequency != EffectFrequency.OncePerTurn || !te.UsedThisTurn)))
```
Do not change the "Modifier-triggered effects" loop later in the same method — modifier-expiration triggers don't use `Frequency`/`UsedThisTurn` and are out of scope. Do not change anything related to `GetAllMinions()` — its call site here is unaffected.

Constraints:
- Scope this diff to exactly this one file, exactly this one `.Where(...)` predicate.
- Do not modify `EvaluatePersistentEffects` — `Aura`-timed effects are continuous and don't participate in the `OncePerTurn` concept.

Acceptance Criteria:
- `GetTriggers` no longer yields a `TriggeredEffect` whose `Frequency == EffectFrequency.OncePerTurn` and `UsedThisTurn == true`. All other filtering behavior unchanged.

Validation:
This file references `EffectFrequency`, created by a sibling task in this same batch — it cannot build alone in isolation (expected). Full solution build and behavior validated by EXIT-001.

Handoff:
WIRE-006 (TriggerEffectAction.cs) is responsible for actually setting `UsedThisTurn = true` when a `OncePerTurn` effect fires — both tasks are required together; EXIT-001's Sigil tests validate the combination.
