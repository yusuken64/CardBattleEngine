Task ID:
MECH-010

Model:
haiku

Title:
Migrate Target -> Targets in Actions\ReturnMinionToHand.cs (class ReturnMinionToCard)

Purpose:
Part of the solution-wide mechanical rename from `ActionContext.Target` (singular) to `ActionContext.Targets` (list), following the field-shape change landed in CTX-000.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf mechanical fixup)

Files:
CardBattleEngine\Actions\ReturnMinionToHand.cs

Requirements:
Apply these substitution rules to every occurrence of `.Target` (singular) in this file (the relevant class here is named `ReturnMinionToCard`):

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target != null && x.Target.<Prop>` (existence + property check combined) | Extract `var single = x.Targets?.FirstOrDefault();` then check `single is { <Prop>: true }` (or the equivalent single-variable form) |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `x.Target is Minion m` (type pattern) | `x.Targets?.FirstOrDefault() is Minion m` |
| `Target = <expr>` in an object initializer, where `<expr>` is provably non-null at that call site | `Targets = [<expr>]` |
| `Target = null` | `Targets = null` |
| `Target = <expr>` where `<expr>` could be null | `Targets = <expr> is null ? null : [<expr>]` |

Do NOT use `.Single()` anywhere. Do not change any logic beyond this substitution — this is a pure rename, no behavior change.

Constraints:
- Scope this diff to exactly this one file.
- Do not touch any other file.
- Do not change any behavior other than the field rename.

Acceptance Criteria:
- No bare `.Target` (singular) reference remains anywhere in this file.
- Every substitution follows the table above with correct null/list semantics.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file. Full solution build is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint.
