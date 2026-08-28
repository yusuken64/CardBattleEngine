Task ID:
SPEC-001

Model:
haiku

Title:
Migrate Target -> Targets in Actions\DamageAction.cs, removing the stale per-iteration mutation

Purpose:
`DamageAction` currently mutates `actionContext.Target` inside its per-entity damage loop as a side effect, which was only ever a scratch-variable hack (no downstream code reads it back). This task migrates the file to `Targets` and deletes that stale mutation instead of translating it mechanically, per the plan's explicit decision.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf fixup, but behaviorally significant: removes a stale-state bug pattern)

Files:
CardBattleEngine\Actions\DamageAction.cs

Requirements:
1. Locate the per-entity damage loop (iterates over the result of `ResolveTargets(...)`). Inside that loop, there is a line that mutates the shared context, e.g.:
```csharp
actionContext.Target = target;
```
**Delete this line entirely. Do not replace it with `actionContext.Targets = [target];` or anything else.** It exists only as a stale scratch-variable write with no reader; the surrounding code should already have direct access to the local `target` variable it was copying from, so nothing needs to read it back off the context.
2. The rest of the loop body (health mutation, `DamageDealt`/`HealthDamageDealt`/`ArmorDamageDealt` tracking, `AffectedEntities.Add(...)`) is unchanged.
3. Elsewhere in this file, apply the standard mechanical substitution rules to any other `.Target` usages, including the `new ActionContext { ... }` construction sites (e.g. one building a context with `Target = attackingMinion.Owner` and one building a `DeathAction` context with `Target = target`):

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `Target = <expr>` in an object initializer, `<expr>` provably non-null | `Targets = [<expr>]` |
| `Target = null` | `Targets = null` |

Do NOT use `.Single()` anywhere.

Constraints:
- Scope this diff to exactly this one file.
- The one exception to "pure mechanical rename" in this file is the deletion described in step 1 — do not apply the mechanical `Targets = [target]` substitution to that specific line, delete it instead.
- Do not change damage math, armor handling, or `AffectedEntities` tracking logic.

Acceptance Criteria:
- The stale `actionContext.Target = target;` (or equivalent) line inside the per-entity damage loop is deleted, with no replacement.
- No bare `.Target` (singular) reference remains anywhere else in this file.
- Damage calculation, armor absorption, and `AffectedEntities` logging behavior is otherwise unchanged.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file, and specifically confirm the stale mutation line is gone rather than translated. Full solution build and test-suite pass is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint. Note for reviewers: this is the one file in Phase A where "no replacement" is the correct outcome for one specific line — do not flag that as an incomplete migration.
