Task ID:
MECH-026

Model:
haiku

Title:
Migrate Target -> Targets in CardBattleEngine.Test\BattleEffectTest.cs

Purpose:
Part of the solution-wide mechanical rename from `ActionContext.Target` (singular) to `ActionContext.Targets` (list), following the field-shape change landed in CTX-000. Test files must be updated or the test project fails to compile.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf mechanical fixup)

Files:
CardBattleEngine.Test\BattleEffectTest.cs

Requirements:
This file only uses `Target = <expr>` object-initializer writes and/or bare `.Target` reads when constructing `ActionContext` instances for tests. Apply these substitution rules to every occurrence of `.Target`/`Target =` (singular) in this file:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `Target = <expr>` in an object initializer, `<expr>` provably non-null | `Targets = [<expr>]` |
| `Target = null` | `Targets = null` |

Do NOT use `.Single()` anywhere. Do not change any test assertions or test logic beyond this substitution — this is a pure rename, no behavior change.

Constraints:
- Scope this diff to exactly this one file.
- Do not touch any other test file.
- Do not change any assertions, test data, or test names.

Acceptance Criteria:
- No bare `.Target` (singular) reference remains anywhere in this file.
- Every substitution follows the table above with correct null/list semantics.
- Test method names, assertions, and structure are unchanged.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file. Full solution build and test-suite pass is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint.
