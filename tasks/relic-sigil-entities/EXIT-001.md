Task ID:
EXIT-001

Model:
haiku

Title:
Final task — add Relic/Sigil tests and confirm the full suite is green

Purpose:
This is the final task in the "relic-sigil-entities" batch, gated behind PHASEA-EXIT → PHASEB-EXIT → PHASEC-EXIT. Those three checkpoints already did the hard correctness work in stages: PHASEA-EXIT verified the foundation enums/fields; PHASEB-EXIT verified the `Minion`→`BoardPermanent` refactor is behavior-preserving (full existing suite green, BEFORE `Board`'s type changed); PHASEC-EXIT verified the whole solution builds and the full existing suite is still green AFTER `Board` became `List<BoardPermanent>` everywhere, reconciling any drift or incidental breakage along the way. By the time this task starts, the codebase is already known-correct — Relics and Minions occupy the same `Player.Board` slots in true left-to-right order (which is what makes a Relic between two Minions correctly break their adjacency for `CleaveOperation`/`AdjacentOperation`, with zero code changes needed to that file). This task's only remaining job is proving the two new entity types behave as designed, via new tests:
- **Relic**: a minion-like board permanent, stored in the SAME `Player.Board` list as Minions (sharing `GameState.MaxBoardSize`, and true left-to-right adjacency with Minions). Can never attack and can never be attacked in combat, but IS a valid target for spells/damage effects and dies (is removed from `Player.Board`) when its `Health` reaches 0. Can hold an activated ability (`RelicAbility`, via `RelicAbilityAction`) usable at most once per turn, resetting at the start of its owner's next turn — exactly like Hero Power. Can optionally have a limited `Charges` count; each time a `TriggeredEffect` on the Relic fires, `Charges` decrements, and the Relic is destroyed once it reaches 0 (e.g. "the next 3 times a minion attacks, do X").
- **Sigil**: a hidden, secret-like trigger holder (`Player.Sigils`, via `SigilAction`) that, unlike a `Secret`, is never automatically removed when its trigger fires. Each of its `TriggeredEffect`s can be configured as `EffectFrequency.Unlimited` (fires every time its condition is met, forever) or `EffectFrequency.OncePerTurn` (fires at most once per turn, re-arming at the start of the next turn).

Dependencies:
PHASEC-EXIT (this task must not start until the WIRE phase checkpoint passes — PHASEC-EXIT already proved the full solution builds and the entire pre-existing test suite is green; this task only needs to add and pass NEW tests on top of that)

Consumes:
Every artifact produced by the 26 FOUND/ENT/WIRE tasks that PHASEA/B/C-EXIT already verified — see each task's own "Produces" section for the exact contract. In particular:
- `BoardPermanent`, `Minion : BoardPermanent`, `Relic : BoardPermanent`, `RelicCard`, `RelicAbility`, `SummonRelicAction`, `RelicAbilityAction`, `InertAttackBehavior`
- `Sigil`, `SigilAction`
- `Player.Board` (now `List<BoardPermanent>`), `Player.Sigils`
- `EffectFrequency`, `TriggeredEffect.Frequency`, `TriggeredEffect.UsedThisTurn`, `EffectTrigger.OnRelicAbility`
- `CardType.Relic`, `EntityType.Relic`
- `ActionContext.SummonedRelic`

Produces:
- A green build and a green full test run — this is the ship gate for the whole batch.
- `CardBattleEngine.Test\RelicTest.cs`, `CardBattleEngine.Test\SigilTest.cs` (new test files).

Files:
- (read/fix-as-needed across the whole solution, especially `CardBattleEngine.Test\*.cs` — see Requirements)
- CardBattleEngine.Test\RelicTest.cs (new file)
- CardBattleEngine.Test\SigilTest.cs (new file)

Requirements:
1. Run `dotnet build` and `dotnet test` once, up front, purely as a sanity check that PHASEC-EXIT's green state is still intact (it should be — no code changes have happened since). If it is somehow NOT green, stop and re-run PHASEC-EXIT's checklist rather than patching around it from here; do not silently absorb a regression into this task.
2. Write `CardBattleEngine.Test\RelicTest.cs` (`[TestClass]`/`[TestMethod]`, following `HeroPowerTests.cs`/`SecretTest.cs` conventions — `GameFactory.CreateTestGame()`, `GameEngine`, `engine.Resolve(state, context, action)`) covering:
   - Playing a `RelicCard` from hand adds a `Relic` to `player.Board`, at the expected position relative to any already-present Minions.
   - A `Relic` on the board can never attack (`relic.CanAttack() == false`) and is never a valid `AttackAction` target for a Minion or Hero attacker (assert `MinionAttackBehavior`/`HeroAttackBehavior`'s `IsValidAttackTarget` reject it with reason `"Relic can't be attacked"`).
   - Playing a Minion with a Cleave-style `AffectedEntitySelector` (using `CleaveOperation`, mirroring `BattleEffectTest.cs`'s existing Cleave/Adjacent tests) when a Relic sits directly between two Minions correctly hits the Relic as the adjacent neighbor instead of the far Minion — proving the shared-`Board`-position adjacency behavior.
   - A `DamageAction` CAN target a `Relic` directly and reduces its `Health`; when `Health` reaches 0, a resulting `DeathAction` removes it from `player.Board`.
   - A `Relic` with a `RelicAbility` can be activated once via `RelicAbilityAction` (mana spent, `Ability.UsedThisTurn` becomes `true`, a second activation the same turn is invalid), and after resolving a `StartTurnAction` for that Relic's owner, `Ability.UsedThisTurn` resets to `false` (mirror `HeroPowerTests.TargetedHeroPowerTest`/`HeroPowerResetsAtStartOfTurn` structure).
   - A `Relic` with `Charges = 3` and a `TriggeredEffect` (e.g. triggered on `EffectTrigger.Attack`) has `Charges` decremented each time the effect fires, and is removed from `player.Board` after the 3rd fire.
   - Playing a `RelicCard` when `player.Board.Count >= state.MaxBoardSize` is rejected by `PlayCardAction.CanCast` with reason `"Board is Full"` — and, because Relics/Minions share the pool, a board full of Relics blocks playing a new Minion too, and vice versa.
3. Write `CardBattleEngine.Test\SigilTest.cs` covering:
   - Playing a `SigilAction` (embedded in a `SpellCard`'s effects, the same way `SecretTest.CounterSpellTest` embeds a `SecretAction`) adds a `Sigil` to `player.Sigils`.
   - A `Sigil`'s `TriggeredEffect` with `Frequency = EffectFrequency.Unlimited` fires every time its condition is met and the Sigil is NOT removed from `player.Sigils` afterward (contrast with `Secret`, which IS removed after firing once — see `SecretTest.CounterSpellTest`'s final assertion).
   - A `Sigil`'s `TriggeredEffect` with `Frequency = EffectFrequency.OncePerTurn` fires once, then does NOT fire again the same turn even if its condition is met again, then DOES fire again after a `StartTurnAction` for its owner resolves (mirror `HeroPowerTests.HeroPowerResetsAtStartOfTurn`'s reset-assertion structure).
4. Run the full test suite (`dotnet test`) and confirm all tests pass, including every pre-existing test (no regressions).

Constraints:
- Do not change the public shape (names/types) that any of the 26 upstream tasks' "Produces" sections promised unless it's genuinely broken/inconsistent — prefer fixing the minority side to match the majority contract.
- Do not add new gameplay features beyond what's needed to prove the behaviors listed above (no new card content, no UI, no AI vectorizer feature additions beyond the compile fix already specified in WIRE-013).
- Follow existing test file conventions exactly (namespace `CardBattleEngine.Test`, `[TestClass]`, `[TestMethod]`, `GameFactory.CreateTestGame()`, `new GameEngine()`, `engine.Resolve(state, context, action)`).

Acceptance Criteria:
- `dotnet build` succeeds for the whole solution with zero errors.
- `dotnet test` succeeds for the whole solution with zero failures, including the new `RelicTest.cs`/`SigilTest.cs` and every pre-existing test file.
- Every behavior listed in Requirements steps 2 and 3 has at least one passing test.

Validation:
Full solution build (`dotnet build`) and full test run (`dotnet test`) from the repository root. This is the batch's actual ship gate — unlike every other task in this batch, this one is expected to produce a fully green build and test run, not a partial/isolated compile.

Handoff:
This is the last task in the batch. Once green, the "relic-sigil-entities" feature (Relic + Sigil entity types, sharing one board with true adjacency) is complete and mergeable.
