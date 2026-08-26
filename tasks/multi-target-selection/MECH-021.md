Task ID:
MECH-021

Model:
haiku

Title:
Migrate Target -> Targets in Engine\EventBus.cs

Purpose:
Part of the solution-wide mechanical rename from `ActionContext.Target` (singular) to `ActionContext.Targets` (list), following the field-shape change landed in CTX-000.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf mechanical fixup)

Files:
CardBattleEngine\Engine\EventBus.cs

Requirements:
Both `GetTriggers` and `EvaluatePersistentEffects` in this file build a new/derived `effectContext` and copy the target across from the original context. Apply these specific translations:
- `effectContext.Target = context.Target;` -> `effectContext.Targets = context.Targets;`
- `effectContext.Target = null;` -> `effectContext.Targets = null;`
- In the modifier-triggered-effect branch, `Target = minion` (object initializer or assignment where `minion` is a single non-null `IGameEntity`) -> `Targets = [minion]`

General substitution rules for any other `.Target` usages in this file:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `Target = <expr>` in an object initializer, `<expr>` provably non-null | `Targets = [<expr>]` |

Do NOT use `.Single()` anywhere. Do not change any logic beyond this substitution — this is a pure rename, no behavior change.

Constraints:
- Scope this diff to exactly this one file.
- Do not change any behavior other than the field rename — triggers and persistent-effect evaluation must fire under exactly the same conditions as before for every existing single-target card.

Acceptance Criteria:
- No bare `.Target` (singular) reference remains anywhere in this file.
- Both `GetTriggers` and `EvaluatePersistentEffects` copy `Targets` (the full list reference) rather than a single entity.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file. Full solution build is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint.
