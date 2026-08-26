Task ID:
PHASEA-EXIT

Model:
haiku

Title:
Phase A integration checkpoint — full solution build and test pass after Target -> Targets migration

Purpose:
Every task in Phase A (CTX-000, all MECH-001..MECH-039, all SPEC-001..SPEC-004, all FWD-001..FWD-009) touches a different file in isolation and cannot itself verify the whole solution compiles. This task is the single point where all of those changes are combined and validated together, and is the gate before any Phase B work (which depends on Phase A being fully green) begins.

Dependencies:
CTX-000, MECH-001, MECH-002, MECH-003, MECH-004, MECH-005, MECH-006, MECH-007, MECH-008, MECH-009, MECH-010, MECH-011, MECH-012, MECH-013, MECH-014, MECH-015, MECH-016, MECH-017, MECH-018, MECH-019, MECH-020, MECH-021, MECH-022, MECH-023, MECH-024, MECH-025, MECH-026, MECH-027, MECH-028, MECH-029, MECH-030, MECH-031, MECH-032, MECH-033, MECH-034, MECH-035, MECH-036, MECH-037, MECH-038, MECH-039, SPEC-001, SPEC-002, SPEC-003, SPEC-004, FWD-001, FWD-002, FWD-003, FWD-004, FWD-005, FWD-006, FWD-007, FWD-008, FWD-009

Consumes:
Every file touched by the above tasks — the fully-migrated `ActionContext.Targets` shape applied solution-wide.

Produces:
A green build + green test suite that every Phase B task (MTC-*) depends on as its starting point.

Files:
(no direct file edits expected in the common case — this task runs build/test tooling; see Requirements for the fallback if gaps are found)

Requirements:
1. Run `dotnet build CardBattleEngine.sln` from the repository root. It must succeed with zero errors.
2. Run the full `CardBattleEngine.Test` test suite (e.g. `dotnet test CardBattleEngine.sln`). Every test that passed before this migration must still pass — this is a behavior-preserving rename, so a regression here indicates a mistake in one of the upstream tasks, not an intentional behavior change.
3. If the build fails with a compile error referencing a leftover singular `.Target` field access, that means a file was missed by the file enumeration in this plan. Locate the file via the compiler error, and apply the same standard mechanical substitution rules used throughout Phase A:

| Old code pattern | New code pattern |
|---|---|
| `x.Target != null` | `x.Targets is { Count: > 0 }` |
| `x.Target` (read as a value) | `x.Targets?.FirstOrDefault()` |
| `Target = <expr>` in an object initializer, `<expr>` provably non-null | `Targets = [<expr>]` |
| `Target = null` | `Targets = null` |

Do NOT use `.Single()`. Fix only what the compiler flags — do not proactively refactor other things.
4. If tests fail (not just fail to compile) after a clean build, investigate whether a mechanical substitution used the wrong rule (e.g. `.Single()` used where `.FirstOrDefault()` was required, or a null-guard missing) and correct it in the specific offending file.

Constraints:
- Do not add any Phase B or Phase C functionality here (no `RequiredTargetCount`, no `MultiTargetChoice`, no `SwapHealthAction`). This task's only job is making Phase A's already-planned changes actually build and pass tests together.
- Any fix made here should be the minimal correction needed to restore a clean build/green tests — not a broader refactor.

Acceptance Criteria:
- `dotnet build CardBattleEngine.sln` succeeds with zero errors across all projects (CardBattleEngine, CardBattleEngine.Test, CardBattleEngine.Benchmark, GameRunner, GamePlayer, GameRecordService, CardGenerator).
- The full `CardBattleEngine.Test` suite passes with the same pass count as before the migration began (zero new failures, zero new skips).
- No remaining bare singular `.Target` reference exists anywhere in the solution (a solution-wide search for the pattern should only match `.Targets`, `.TargetOwner`, or similarly-named unrelated identifiers).

Validation:
This IS the full build + full test validation step for Phase A — there is no further checkpoint above this one for Phase A. Run both `dotnet build` and `dotnet test` and confirm both are clean.

Handoff:
Once this task is green, every Phase B task (MTC-001 through MTC-008) may proceed — they all assume a fully-migrated, fully-green `ActionContext.Targets` codebase as their starting point.
