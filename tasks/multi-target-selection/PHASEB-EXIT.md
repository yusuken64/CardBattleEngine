Task ID:
PHASEB-EXIT

Model:
haiku

Title:
Phase B integration checkpoint — build, full test pass, and new test proving the generic pause/resume cycle works for both a bare card and a bare trigger

Purpose:
Phase B introduced a new generic targeting mechanism spanning several files (NTR-001 through NTR-013). This task combines and validates all of it together, and adds a dedicated test proving the accumulate-until-N-picks loop actually terminates correctly through the real `GameEngine.Resolve` central interception — using bare fixtures, not yet the real `SwapHealthAction`/attack-trigger proofs-of-concept (those are Phase C). This is the gate before Phase C begins.

Dependencies:
NTR-001, NTR-003, NTR-004, NTR-005, NTR-006, NTR-007, NTR-008, NTR-009, NTR-010, NTR-011, NTR-012, NTR-013

Consumes:
Every file touched by the Phase B tasks: `TargetRequirement`/`ActionContext.PendingTargetRequirement`, `TargetSelectionChoice`/`SupplyTargetAction`, the central interception in `GameEngine.Resolve`, `UntargetableFilter`, `Card.RequiredTargetCount`/`AllowDuplicateTargets`, the `GetValidActions` multi-target branch, the upgraded `PlayCardAction.IsValid`, the `ValidTargetRestriction` count check, the `CastSpellAction` passthrough branch, and `TriggeredEffect.TargetRequirement`/`TriggerEffectAction`'s new branch.

Produces:
A green build + green test suite (including the new tests below) that Phase C's two proofs-of-concept build on.

Files:
CardBattleEngine.Test\TargetSelectionChoiceTest.cs (new file)

Requirements:
1. Run `dotnet build CardBattleEngine.sln` — must succeed with zero errors. If it fails due to a mismatch between how NTR-003/NTR-004/NTR-005 assumed types were shaped versus their actual definitions, fix the minimal syntax mismatch in the affected file(s) — do not modify the underlying interfaces themselves.
2. Run the full `CardBattleEngine.Test` suite — every pre-existing test must still pass (Phase B is purely additive for actions that never set `PendingTargetRequirement`).
3. Add a new test file `CardBattleEngine.Test\TargetSelectionChoiceTest.cs` with two tests:

   a) **Card-driven pick-2, using a bare fixture (no `SwapHealthAction` yet)** — a `SpellCard` with `RequiredTargetCount = 2` and an empty effect (`GameActions = []`, so the test only cares about target accumulation, not effect resolution), played by constructing and resolving its `PlayCardAction` directly via `engine.Resolve(...)` (mirroring the real path NTR-007 wires up). Drive it through `state.GetValidActions(current)` to find and resolve the `SupplyTargetAction` options twice, then assert: an already-picked entity is excluded from the second round's candidate pool, and the flow only finalizes (re-runs `PlayCardAction` with a full `Targets` list) after exactly 2 picks, in pick order.

   b) **Trigger-driven pick-1, using a bare fixture** — a `Minion`/`MinionCard` with a `TriggeredEffect { EffectTrigger = EffectTrigger.Attack, Scope = TriggerScope.Self, TargetRequirement = new TargetRequirement { Provider = new EntityTypeSelector { EntityTypes = EntityType.Minion, TeamRelationship = TeamRelationship.Any }, Count = 1 }, GameActions = [] }` (again, empty `GameActions` — this test only proves the trigger correctly installs a `TargetSelectionChoice` and that picking a target correctly resolves it, not any specific damage effect). Trigger an attack with this minion, confirm `gameState.PendingChoice` becomes a `TargetSelectionChoice`, resolve the single pick via `state.GetValidActions`, and confirm the flow completes without error.

   Adapt helper/fixture names to match this codebase's actual existing test conventions (check `TargetTest.cs`, `SpellTest.cs`, `CombatTest.cs` — all migrated in Phase A — for the real setup helpers, constructors, and assertion style).

Constraints:
- Do not use `SwapHealthAction` or the attack-trigger damage effect from Phase C in this test — they don't exist yet at this point in the plan; use empty `GameActions = []` fixtures as described above so this test is purely about the selection mechanism, not any specific effect.
- Do not modify any Phase B production file in this task unless fixing a genuine integration mismatch surfaced by the build/test run (and if so, keep the fix minimal and scoped to the actual error).

Acceptance Criteria:
- `dotnet build CardBattleEngine.sln` succeeds with zero errors.
- The full `CardBattleEngine.Test` suite passes, including both new tests.
- The card-driven test demonstrates: already-picked entities are excluded from the next round's candidate pool (unless `AllowDuplicateTargets` is set), the flow finalizes only after exactly `RequiredTargetCount` picks, and target order is preserved.
- The trigger-driven test demonstrates: an attack trigger with a `TargetRequirement` correctly installs a `TargetSelectionChoice` (not an `AffectedEntitySelector`-based resolution), and picking the required number of targets correctly resumes and completes the triggered effect's resolution chain.

Validation:
This IS the full build + full test validation step for Phase B. Run both `dotnet build` and `dotnet test` and confirm both are clean, including both new tests.

Handoff:
Once this task is green, Phase C tasks (POC-001, POC-002, POC-003) may proceed — they assume a working, tested generic targeting mechanism (covering both card-driven and trigger-driven cases) as their starting point.
