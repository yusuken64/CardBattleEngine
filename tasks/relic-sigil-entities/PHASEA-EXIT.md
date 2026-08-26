Task ID:
PHASEA-EXIT

Model:
haiku

Title:
Phase A checkpoint — verify the foundation wave before the ENT phase starts

Purpose:
Correctness, not speed, is the priority for this batch. Rather than trusting all 26 tasks' written contracts end-to-end and only finding out at the very end whether they agree with each other, this batch is gated into phases: FOUND → ENT → WIRE → EXIT. Each phase must build and check out before the next phase's tasks are dispatched. This is Phase A's gate. FOUND-001 through FOUND-006 are all pure, self-contained additions (new enum values, new fields, small new files) with NO forward references to anything created later in the batch — unlike `ENT-007` (which needs the `Relic` type and was deliberately moved out of this phase for exactly that reason), every FOUND task should compile completely on its own once the other FOUND tasks have also landed.

Dependencies:
FOUND-001, FOUND-002, FOUND-003, FOUND-004, FOUND-005, FOUND-006

Consumes:
Every artifact produced by FOUND-001..006 — see each task's own "Produces" section.

Produces:
- Confirmation that the foundation wave is internally consistent — this unblocks ENT-001..007.

Files:
(none — this task only reads/builds, it does not edit code)

Requirements:
1. Run `dotnet build` on `CardBattleEngine\CardBattleEngine.csproj`. It IS expected to fully succeed at this checkpoint (all six FOUND tasks are additive with no forward references to unlanded types) — if it does not, that's a real bug in one of the six FOUND tasks, not an expected gap.
2. Specifically verify:
   - `EffectFrequency` enum exists with `Unlimited`/`OncePerTurn`, and `TriggeredEffect.Frequency`/`UsedThisTurn` exist and are copied in `TriggeredEffect.Clone()`.
   - `EffectTrigger.OnRelicAbility` exists.
   - `CardType` includes `Relic` (5 values total, in the original order plus `Relic` appended).
   - `EntityType` includes `Relic = 1 << 4`.
   - `InertAttackBehavior.cs`, `RelicAbility.cs`, `Sigil.cs` exist and compile as specified in their respective task files.
3. Run `dotnet test` on the existing test suite (`CardBattleEngine.Test`). It should be 100% green and unchanged from before this batch started — nothing in Phase A touches any code path exercised by an existing test.

Constraints:
- Do not proceed to write any ENT/WIRE/EXIT code from this task. This is a checkpoint, not an implementation task.
- If anything fails, fix it within the scope of the specific FOUND-00X file responsible — do not patch around it from here.

Acceptance Criteria:
- `dotnet build` succeeds with zero errors.
- `dotnet test` is 100% green, identical pass count to the pre-batch baseline.

Validation:
`dotnet build` + `dotnet test` from the repository root.

Handoff:
Once green, ENT-001 through ENT-007 (and, per WIRE tasks' own "Dependencies", eventually PHASEB-EXIT) may proceed. Do not start the ENT phase before this checkpoint passes.
