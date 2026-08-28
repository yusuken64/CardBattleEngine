Task ID:
SPEC-004

Model:
haiku

Title:
Migrate Target -> Targets in AI\GameStateVectorizer.cs, keeping single-target AI encoding

Purpose:
The AI action encoder/decoder uses a fixed-width bitfield with one slot for "the target." This task migrates it to read/write `ActionContext.Targets` while deliberately keeping the AI's action space single-target-only for now — no multi-target card will be registered in game content as part of this plan, so the encoder does not need to represent more than one target per action.

Dependencies:
CTX-000

Consumes:
CTX-000: `ActionContext.Targets` field shape (`List<IGameEntity>`)

Produces:
(none — leaf fixup)

Files:
CardBattleEngine\AI\GameStateVectorizer.cs

Requirements:
1. In `EncodeAction`, find the line reading the target for encoding, e.g. `int target = GetTargetIndex(ctx.Target, state);`. Change it to read the first (and, for now, only) chosen target:
```csharp
int target = GetTargetIndex(ctx.Targets?.FirstOrDefault(), state);
```
Do not attempt to encode multiple targets into the bitfield in this task — the AI/RL action space intentionally stays single-target for now. Add a short one-line comment at this site noting this is a known limitation if multi-target cards are added later.
2. In `DecodeAction`, find every `new ActionContext { ..., Target = target }` construction (there are multiple call sites in this file, for the `AttackAction`, `PlayCardAction`, and `HeroPowerAction` decode cases). In each, `target` here **can be null** (per `ResolveTargetFromIndex`'s contract) — so translate using the null-guarded form, not the bare list-literal shortcut:
```csharp
Targets = target is null ? null : [target],
```
Do not use `Targets = [target]` unguarded at these sites — that would wrap a null into a one-element list containing null, which is wrong.
3. Apply the standard mechanical substitution rules to any other `.Target` usages in this file:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |

Do NOT use `.Single()` anywhere.

Constraints:
- Scope this diff to exactly this one file.
- Do not change the bitfield layout, the number of bits allocated to target encoding, or any other part of the action-space design — this task only fixes the field name and null-handling, not the encoding scheme.
- Do not attempt to add multi-target support to the AI in this task.

Acceptance Criteria:
- `EncodeAction` reads `ctx.Targets?.FirstOrDefault()` instead of `ctx.Target`, with a one-line comment noting the single-target limitation.
- Every `DecodeAction` call site constructing an `ActionContext` uses the null-guarded `Targets = target is null ? null : [target]` form, not an unguarded `[target]`.
- No bare `.Target` (singular) reference remains anywhere else in this file.
- AI encoding/decoding behavior for any existing single-target action is identical before and after this change.

Validation:
This file alone cannot make the full solution build (many sibling files still reference the old field until their own tasks land) — that's expected. Confirm via search that no bare singular `.Target` reference remains in this file, and specifically confirm the null-guard is present at every `DecodeAction` construction site. Full solution build and test-suite pass (including `AITest.cs`, migrated separately in MECH-023) is validated by the PHASEA-EXIT task once all sibling tasks land.

Handoff:
No downstream task specifically depends on this file beyond the overall PHASEA-EXIT integration checkpoint. If multi-target cards are ever added to real game content, this file's single-target AI encoding is a known future limitation (documented in-line via the comment added in step 1).
