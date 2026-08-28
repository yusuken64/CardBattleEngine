Task ID:
MECH-020

Model:
haiku

Title:
Migrate Target -> Targets in ValueProviders\StatValue.cs

Purpose:
Part of the solution-wide mechanical rename from `ActionContext.Target` (singular) to `ActionContext.Targets` (list), following the field-shape change landed in CTX-000.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf mechanical fixup)

Files:
CardBattleEngine\ValueProviders\StatValue.cs

Requirements:
This value provider currently reads a stat (Attack/Health/etc.) off of `context.Target`. Translate this to read off the first chosen target: `context.Targets?.FirstOrDefault()`. This preserves exact existing behavior for every current (single-target) card.

General substitution rules for any other `.Target` usages in this file:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `x.Target.<Prop>` | `x.Targets?.FirstOrDefault()?.<Prop>` (add appropriate null handling to match the surrounding code's existing null-safety style) |

Do NOT use `.Single()` anywhere.

Constraints:
- Scope this diff to exactly this one file.
- Do not change any behavior other than the field rename.

Acceptance Criteria:
- No bare `.Target` (singular) reference remains anywhere in this file.
- The computed value for any existing single-target card is identical before and after this change.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file. Full solution build is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint.
