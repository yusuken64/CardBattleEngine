Task ID:
MECH-016

Model:
haiku

Title:
Migrate Target -> Targets in TargetSelectors\CleaveOperation.cs

Purpose:
Part of the solution-wide mechanical rename from `ActionContext.Target` (singular) to `ActionContext.Targets` (list), following the field-shape change landed in CTX-000.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf mechanical fixup)

Files:
CardBattleEngine\TargetSelectors\CleaveOperation.cs

Requirements:
This file contains both `CleaveOperation` and `AdjacentOperation`. Cleave/adjacent logic conceptually pivots around a single "center" minion (the one thing the player actually targeted), so the correct translation for a check like `if (context.Target is not Minion target)` is:
```csharp
if (context.Targets?.FirstOrDefault() is not Minion target)
```
i.e. always take the *first* chosen target as the pivot/center — do not attempt to make cleave operate over multiple pivot minions in this task, that is out of scope.

General substitution rules for any other `.Target` usages in this file:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `x.Target is Minion m` (type pattern) | `x.Targets?.FirstOrDefault() is Minion m` |
| `Target = <expr>` in an object initializer | `Targets = [<expr>]` (or `Targets = null` if `<expr>` was `null`) |

Do NOT use `.Single()` anywhere.

Constraints:
- Scope this diff to exactly this one file.
- Do not change any behavior other than the field rename — cleave/adjacent still pivot around exactly one entity (the first chosen target), matching current behavior exactly.

Acceptance Criteria:
- No bare `.Target` (singular) reference remains anywhere in this file.
- Both `CleaveOperation` and `AdjacentOperation` still pivot around a single minion (now `context.Targets?.FirstOrDefault()`), with identical behavior to before for any single-target card.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file. Full solution build is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint.
