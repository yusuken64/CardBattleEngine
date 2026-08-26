Task ID:
MECH-019

Model:
haiku

Title:
Migrate Target -> Targets in TriggerConditions\TargetTypeCondition.cs

Purpose:
Part of the solution-wide mechanical rename from `ActionContext.Target` (singular) to `ActionContext.Targets` (list), following the field-shape change landed in CTX-000.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf mechanical fixup)

Files:
CardBattleEngine\TriggerConditions\TargetTypeCondition.cs

Requirements:
This condition currently likely type-checks `context.Target` (e.g. `context.Target is Minion`). Translate any such check to use the first chosen target: `context.Targets?.FirstOrDefault() is Minion`. This preserves exact existing behavior for every current (single-target) card. Do not attempt to check the type of *any* of multiple targets — that would be a behavior change out of scope for this mechanical task.

General substitution rules for any other `.Target` usages in this file:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `Target = <expr>` in an object initializer | `Targets = [<expr>]` (or `Targets = null` if `<expr>` was `null`) |

Do NOT use `.Single()` anywhere.

Constraints:
- Scope this diff to exactly this one file.
- Do not touch `TriggerConditions\SourceIsTargetCondition.cs` (MECH-017) or `TriggerConditions\TargetOwnerCondition.cs` (MECH-018).
- Do not change any behavior other than the field rename.

Acceptance Criteria:
- No bare `.Target` (singular) reference remains anywhere in this file.
- The condition's truth value for any existing single-target card is identical before and after this change.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file. Full solution build is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint.
