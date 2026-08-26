Task ID:
SPEC-003

Model:
haiku

Title:
Migrate Target -> Targets in Entities\WeaponCard.cs

Purpose:
Part of the solution-wide mechanical rename from `ActionContext.Target` (singular) to `ActionContext.Targets` (list), following the field-shape change landed in CTX-000. Unlike the stale mutations in DamageAction/DestroyWeaponAction, this file's write is a legitimate single-target assignment and must be translated, not deleted.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf mechanical fixup)

Files:
CardBattleEngine\Entities\WeaponCard.cs

Requirements:
Locate the line `actionContext.Target = actionContext.SourcePlayer;`. This is a legitimate assignment (the weapon card's play effect targets the player equipping it) — translate it to:
```csharp
actionContext.Targets = [actionContext.SourcePlayer];
```
This context object is reused by the caller (`PlayCardAction.Resolve` passes the same `actionContext` into `Card.GetPlayEffects`) — do not reorder this assignment relative to other statements in the method; keep it exactly where the original `Target` write was.

Apply the standard mechanical substitution rules to any other `.Target` usages in this file:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `Target = <expr>` in an object initializer | `Targets = [<expr>]` (or `Targets = null` if `<expr>` was `null`) |

Do NOT use `.Single()` anywhere.

Constraints:
- Scope this diff to exactly this one file.
- Do not move or reorder the assignment relative to surrounding statements.
- Do not change weapon-card play-effect logic otherwise.

Acceptance Criteria:
- `actionContext.Target = actionContext.SourcePlayer;` becomes `actionContext.Targets = [actionContext.SourcePlayer];`, in the same location in the method.
- No bare `.Target` (singular) reference remains anywhere else in this file.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file. Full solution build and test-suite pass is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
Task MTC-001 (Phase B, adds `Card.RequiredTargetCount` and propagates it through `Clone()` in `WeaponCard.cs` among other files) touches this same file later — sequence MTC-001 after this task and after PHASEA-EXIT to avoid merge conflicts.
