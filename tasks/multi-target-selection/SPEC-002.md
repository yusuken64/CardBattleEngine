Task ID:
SPEC-002

Model:
haiku

Title:
Migrate Target -> Targets in Actions\DestroyWeaponAction.cs, removing the stale mutation

Purpose:
`DestroyWeaponAction` currently sets `context.Target = player;` purely so a few lines below can read `player` back off the context — but `player` is already a local variable at that point, so the write is dead. This task migrates the file to `Targets` and deletes that stale write instead of translating it mechanically, per the plan's explicit decision.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf fixup)

Files:
CardBattleEngine\Actions\DestroyWeaponAction.cs

Requirements:
1. Locate the line `context.Target = player;` (or equivalent, where `player` is already an existing local variable at that point in the method). **Delete this line entirely. Do not replace it with `context.Targets = [player];` or anything else.**
2. Elsewhere in this file, the remaining `Target = weapon.Owner` / `Target = target` style sites get the standard mechanical rename:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `Target = <expr>` in an object initializer, `<expr>` provably non-null | `Targets = [<expr>]` |
| `Target = null` | `Targets = null` |

Do NOT use `.Single()` anywhere.

Constraints:
- Scope this diff to exactly this one file.
- The one exception to "pure mechanical rename" in this file is the deletion described in step 1 — do not translate that specific line, delete it instead.
- Do not change weapon-destruction logic otherwise.

Acceptance Criteria:
- The stale `context.Target = player;` (or equivalent) line is deleted, with no replacement.
- No bare `.Target` (singular) reference remains anywhere else in this file.
- Weapon destruction behavior is otherwise unchanged.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file, and specifically confirm the stale write is gone rather than translated. Full solution build and test-suite pass is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint.
