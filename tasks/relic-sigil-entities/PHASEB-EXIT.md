Task ID:
PHASEB-EXIT

Model:
haiku

Title:
Phase B checkpoint — verify the Minion/BoardPermanent refactor is behavior-preserving BEFORE Board's type changes anywhere else

Purpose:
This is the most important checkpoint in the batch. ENT-001 refactors the existing, heavily-tested `Minion.cs` to extract a new `BoardPermanent` base class — this is a real risk: if the refactor subtly changes behavior (a field not copied in `Clone()`, a method that no longer overrides correctly, a modifier calculation moved incorrectly), every downstream WIRE task inherits that bug silently, because they all trust ENT-001's contract without re-deriving it themselves. Critically, at the end of Phase B, `Player.Board` is STILL declared `List<Minion>` — WIRE-002 (the task that changes it to `List<BoardPermanent>`) hasn't run yet. That means the ENTIRE existing test suite should still pass, completely unchanged, against the refactored `Minion`/new `BoardPermanent`/new `Relic` code. If even one existing test fails here, the refactor broke something, and it must be fixed HERE — before Phase C's 13 tasks start building on top of it and make the failure much harder to isolate.

Dependencies:
ENT-001, ENT-002, ENT-003, ENT-004, ENT-005, ENT-006, ENT-007

Consumes:
Every artifact produced by ENT-001..007 — see each task's own "Produces" section. In particular the exact shape of `BoardPermanent`, `Minion : BoardPermanent`, `Relic : BoardPermanent`.

Produces:
- Confirmation that the Minion refactor is behavior-preserving and the new Relic/Sigil/RelicAbility scaffolding is internally consistent — this unblocks WIRE-001..013.

Files:
(none — this task only reads/builds/tests, it does not edit code, EXCEPT for fixing genuine drift within ENT-001..007's own files if found — see Requirements)

Requirements:
1. Run `dotnet build` on the full solution. At this point `Player.Board` is still `List<Minion>` (WIRE-002 hasn't run), so this SHOULD fully succeed — `Minion` still satisfies `List<Minion>` since it's still a concrete `Minion` (just now inheriting from `BoardPermanent` instead of implementing interfaces directly). If it does not build, that is a real defect in ENT-001..007, not an expected gap — find and fix it now.
2. Run `dotnet test` on the FULL existing test suite (`CardBattleEngine.Test`, every file — `CombatTest.cs`, `AbilityTest.cs`, `RebornTest.cs`, `WeaponTest.cs`, `BattleEffectTest.cs`, `CloneTests.cs`, `AuraTest.cs`, `HashTest.cs`, all of them). This is the critical check: it must be 100% green, with the SAME pass count as the pre-batch baseline. Every one of these tests exercises `Minion` through its original interface contract (`IGameEntity`, `ITriggerSource`) — if the refactor moved a member incorrectly, changed a default value, or broke `Clone()`'s covariance, something here will fail.
3. If a test fails, diagnose whether the defect is in `BoardPermanent` (ENT-001) or in `Minion`'s refactored body (also ENT-001) — that task bundled both files together specifically so one agent could keep them consistent; if they drifted, fix the file with the actual bug, not the test.
4. Independently double-check `Minion.Clone()`'s covariant return: confirm `Player.cs` (not yet changed by WIRE-002) still compiles its existing `Minion clonedMinion = minion.Clone();` lines unchanged — if `Clone()`'s return type isn't correctly covariant, this is where it will surface as a compile error, not a test failure.
5. Sanity-check `Relic`/`RelicCard`/`SummonRelicAction`/`RelicAbilityAction`/`Sigil`/`SigilAction` compile and their constructors/members match exactly what ENT-002 through ENT-006 specified (these can't be exercised by the EXISTING test suite yet, since nothing references them outside this batch — but they must at least compile cleanly as part of step 1's build).

Constraints:
- Do not start any WIRE-phase edits from this task. This is a checkpoint.
- Any fix made here must stay within the ENT-001..007 files' own scope — do not reach into files WIRE tasks own (e.g. do not touch `Player.cs`'s `Board` declaration from here; that's WIRE-002's job and must happen only after this checkpoint passes).

Acceptance Criteria:
- `dotnet build` succeeds with zero errors.
- `dotnet test` is 100% green across the ENTIRE existing suite, with the same pass count as the pre-batch baseline.

Validation:
`dotnet build` + `dotnet test` (full suite, not a subset) from the repository root.

Handoff:
Once green, WIRE-001 through WIRE-013 may proceed — they all trust that `BoardPermanent`/`Minion`/`Relic` are correct, because this checkpoint just proved it against the real existing test suite, not just a written contract. Do not start the WIRE phase before this checkpoint passes.
