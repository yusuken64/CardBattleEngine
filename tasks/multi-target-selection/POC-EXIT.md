Task ID:
POC-EXIT

Model:
haiku

Title:
Plan-wide final checkpoint — full build, full test pass, and manual GameRunner smoke test of both proofs-of-concept

Purpose:
This is the final sign-off for the entire multi-target/generic-targeting plan: confirms the whole solution builds and tests pass, and manually exercises both the card-driven ("pick 2 minions, swap health") and trigger-driven ("whenever this minion attacks, select a minion, deal 3 damage") flows through the actual console `HumanAgent` experience — not just automated tests — to catch anything a unit test wouldn't (confusing display text, a menu that doesn't make sense to a real player).

Dependencies:
POC-002, POC-003

Consumes:
POC-002: the card-driven end-to-end test. POC-003: the trigger-driven end-to-end test.

Produces:
(none — this is the terminal task of the plan)

Files:
(no file edits expected in the common case — this task runs and manually observes the app; see Requirements for the fallback if gaps are found)

Requirements:
1. Run `dotnet build CardBattleEngine.sln` — must succeed with zero errors across every project in the solution.
2. Run the full `CardBattleEngine.Test` suite — every test (pre-existing and newly added across this entire plan) must pass.
3. Run `GameRunner` with a human-controlled player. Wire in both proof-of-concept fixtures — the `BodySwap` card (`RequiredTargetCount = 2`, `SwapHealthAction` effect, from POC-002) and the `Sniper` minion (attack-triggered `TargetRequirement`-based `DamageAction`, from POC-003) — into whatever ad-hoc/debug hand-seeding mechanism `GameRunner\Program.cs` already provides for testing specific cards. Play a game that exercises both.
4. Manually confirm, via the console, for the **card-driven** flow:
   - `BodySwap` appears as a normal single playable option in the action menu (no separate "begin selection" step visible to the player).
   - Selecting it immediately prompts a "Pick target" menu listing valid minions.
   - After picking the first target, that minion no longer appears in the second pick's menu.
   - After picking the second target, the effect resolves silently (health swap) and the game continues normally.
5. Manually confirm, via the console, for the **trigger-driven** flow:
   - Attacking with `Sniper` resolves the attack normally, then immediately prompts a "Pick target" menu — with no card-specific framing (this is clearly a trigger-driven prompt, exercising the same generic UI path as the card-driven one).
   - Both minions on the opposing board (not just the one that was attacked) are legal choices.
   - Picking one applies exactly 3 damage to it and leaves the other untouched.
6. If any of the above manual checks reveal a genuine defect (not a display nitpick), fix it in the specific offending file from the relevant earlier task and re-run steps 1–5. If it's a minor display wording issue only, note it but a fix is optional at your discretion.

Constraints:
- Do not add new game content, cards, or mechanics beyond wiring up the two proof-of-concept fixtures already defined by POC-002/POC-003 for this manual smoke test.
- Any production fix made here should be the minimal correction needed — not a broader refactor.

Acceptance Criteria:
- `dotnet build CardBattleEngine.sln` succeeds with zero errors.
- The full `CardBattleEngine.Test` suite passes (100% of tests, including every test touched or added across this entire plan).
- A human playtester can play `BodySwap` through the console `GameRunner`/`HumanAgent` flow and see two picked minions' health swap, with no crashes, hangs, or nonsensical menu text.
- A human playtester can attack with `Sniper` through the same console flow and see the picked minion take 3 damage, with the same generic "Pick target" menu experience as the card-driven case — proving the mechanism is genuinely shared, not two parallel implementations.

Validation:
This is the terminal validation step for the entire plan (Phases A, B, and C). A clean build, a fully green test suite, and two successful manual playtests — one card-driven, one trigger-driven, both going through the identical generic targeting UI — together constitute full sign-off that "any action that requires targets should prompt the player" is now genuinely true of this engine, not just true for `PlayCardAction`.

Handoff:
None — this is the last task in the plan.
