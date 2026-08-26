Task ID:
PHASEC-EXIT

Model:
haiku

Title:
Phase C checkpoint — verify the whole solution builds and passes once Player.Board actually becomes List<BoardPermanent>

Purpose:
WIRE-002 changes `Player.Board`'s declared element type from `List<Minion>` to `List<BoardPermanent>` — this is the change that finally makes Relics live on the same board as Minions with real shared capacity and adjacency. Twelve other WIRE tasks (WIRE-001, WIRE-003..013) each fix or extend exactly one existing call site that this change affects (or is otherwise part of the Relic/Sigil feature), all written against the SAME contract but by independent agents with no visibility into each other's actual diffs. This checkpoint is where that fans back in: the full solution must build, and — because `Board`'s element type change can silently break existing code this batch's tasks didn't explicitly touch (most likely in `CardBattleEngine.Test\*.cs`, which nothing in this batch was scoped to edit) — this task is also responsible for finding and fixing any such breakage before the final EXIT-001 task adds new tests on top.

Dependencies:
PHASEB-EXIT, WIRE-001, WIRE-002, WIRE-003, WIRE-004, WIRE-005, WIRE-006, WIRE-007, WIRE-008, WIRE-009, WIRE-010, WIRE-011, WIRE-012, WIRE-013

Consumes:
Every artifact produced by WIRE-001..013 — see each task's own "Produces" section. In particular the fact that `Player.Board` is now `List<BoardPermanent>` everywhere.

Produces:
- A green build and a green full existing-test-suite run with `Board` fully unified — this unblocks EXIT-001 (which only adds NEW tests on top of an already-correct foundation).

Files:
- (read/fix-as-needed across the whole solution, especially `CardBattleEngine.Test\*.cs` — see Requirements)

Requirements:
1. Run `dotnet build` on the full solution. Fix errors in this priority order:
   a. Drift between two of WIRE-001..013's own contracts (e.g. a task assumed a slightly different shape than a sibling task produced) — fix the wrong side to match the contract documented in the relevant task file.
   b. Existing files that this batch's task list did NOT explicitly cover but that assumed `Player.Board` is `List<Minion>` and now fail to compile because an element could be `BoardPermanent`/`Relic`. Search broadly (`CardBattleEngine\**\*.cs` and `CardBattleEngine.Test\**\*.cs`) for `.Board[` indexing or `foreach (var <x> in ...Board)` followed by access to a Minion-only member (`.Taunt`, `.HasDivineShield`, `.HasPoisonous`, `.HasRush`, `.HasWindfury`, `.HasLifeSteal`, `.HasReborn`, `.HasCharge`, `.IsStealth`, `.Elusive`, `.AttacksPerformedThisTurn`, `.HasSummoningSickness`, `.IsFrozen`, `.MissedAttackFromFrozen`, `.CannotAttack`, `.Tribes`, `.OriginalCard` where a `MinionCard` is expected). Fix each by casting/pattern-matching to `Minion` (if the surrounding logic is genuinely Minion-only) or adding `.OfType<Minion>()` — do not change `Board`'s type back, and do not add Minion-only members to `BoardPermanent`.
2. Run `dotnet test` on the FULL existing test suite. It must be 100% green with the SAME pass count as the pre-batch baseline (Phase B already proved the Minion refactor itself is sound — any failure here is specifically about the `Board` type change or one of the WIRE tasks' behavior additions, e.g. a test that plays a Minion and happens to also exercise something WIRE-010/011/012 touched).
3. Do NOT write the new `RelicTest.cs`/`SigilTest.cs` from this task — that's EXIT-001's job, deliberately kept separate so this checkpoint's pass/fail signal is purely "did the refactor + wiring stay correct," not "did we also write good new tests."

Constraints:
- Do not change the public shape (names/types) that any of the WIRE tasks' "Produces" sections promised unless it's genuinely broken/inconsistent — prefer fixing the minority side to match the majority contract.
- Any fix to a pre-existing test file must be the minimal change needed to keep it compiling/passing against the new `Board` type — do not rewrite test logic or weaken assertions to make them pass.

Acceptance Criteria:
- `dotnet build` succeeds for the whole solution with zero errors.
- `dotnet test` is 100% green across the ENTIRE existing suite (same pass count as baseline). No new tests are added at this checkpoint.

Validation:
`dotnet build` + `dotnet test` (full suite) from the repository root.

Handoff:
Once green, EXIT-001 may proceed — it can assume the whole solution, including every pre-existing test, is already correct, and its only remaining job is authoring and passing the NEW Relic/Sigil tests.
