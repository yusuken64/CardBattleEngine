Task ID:
MECH-015

Model:
haiku

Title:
Migrate Target -> Targets in TargetSelectors\ContextSelector.cs

Purpose:
Part of the solution-wide mechanical rename from `ActionContext.Target` (singular) to `ActionContext.Targets` (list), following the field-shape change landed in CTX-000.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf mechanical fixup)

Files:
CardBattleEngine\TargetSelectors\ContextSelector.cs

Requirements:
Apply the general substitution rules below to any plain `.Target` reads/writes in this file. There are two specific sites that need different treatment:

1. The `IncludeTarget` block (currently something like `if (IncludeTarget && context.Target != null) yield return context.Target;`) — change to:
```csharp
if (IncludeTarget && context.Targets != null)
    foreach (var t in context.Targets)
        yield return t;
```
2. The `IncludeTargetOwner` block (currently something like `if (IncludeTargetOwner && context.Target?.Owner != null) yield return context.Target.Owner;`) — change to:
```csharp
if (IncludeTargetOwner)
    foreach (var t in context.Targets ?? Enumerable.Empty<IGameEntity>())
        if (t.Owner != null)
            yield return t.Owner;
```
This means that once a future multi-target card exists, this selector will naturally include the owners of all chosen targets, not just one — that is an intentional, expected side effect of the field-shape change, not a bug. Do not add any other new capability beyond this direct translation.

General substitution rules for any other `.Target` usages in this file:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `Target = <expr>` in an object initializer | `Targets = [<expr>]` (or `Targets = null` if `<expr>` was `null`) |

Do NOT use `.Single()` anywhere.

Constraints:
- Scope this diff to exactly this one file.
- Do not touch `TargetSelectors\ContextOperation.cs` (separate task MECH-014) even though it has a similar `IncludeTarget` pattern.
- Do not change any behavior other than the field rename plus the two specific loop translations described above.

Acceptance Criteria:
- No bare `.Target` (singular) reference remains anywhere in this file.
- Both `IncludeTarget` and `IncludeTargetOwner` blocks iterate every entry in `context.Targets`, not just one.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file. Full solution build is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint.
