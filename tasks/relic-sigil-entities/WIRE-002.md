Task ID:
WIRE-002

Model:
haiku

Title:
Change Player.Board to List<BoardPermanent>, add Sigils, update Clone()/LightClone()

Purpose:
The project is adding a new "Relic" board entity that must share `Player.Board` with `Minion` (same list, same capacity, same left-to-right ordering, so board-slot capacity and adjacency both come free from plain list membership/position). This requires changing `Board`'s element type from `Minion` to the new common base `BoardPermanent` (see ENT-001). The project is also adding a "Sigil" entity (hidden, like `Secret`) that needs its own list on `Player`. Both changes require careful `Clone()`/`LightClone()` updates — this project's most recent commit (`3d1e5d6`, "fix core cloning, aura death, and minion serialization bugs") fixed exactly this class of bug (fields dropped on clone); do not reintroduce it here.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `BoardPermanent` abstract class, with `Minion : BoardPermanent` and `Relic : BoardPermanent` (produced by ENT-001/ENT-002, running in parallel) — trust `BoardPermanent` has `internal abstract BoardPermanent Clone();` (overridden covariantly by both subclasses) and a public `Owner` (`Player`) property.
- `Sigil` class (produced by FOUND-006, running in parallel) — trust it has an internal `Clone()` method and a public `Owner` (`Player`) property.

Produces:
- `Player.Board` retyped to `List<BoardPermanent>` — consumed by every existing file that reads `Board` as `List<Minion>` today; WIRE-003, WIRE-004, WIRE-010, WIRE-013 all update the specific call sites in this batch that break as a result (`GetAllMinions()`, `StartTurnAction`'s per-minion reset loop, `AttackRules.MustAttackTaunt`, `GameStateVectorizer.ToVector`). EXIT-001 is responsible for a final `dotnet build` sweep to catch and fix any other existing call site (including test files) that assumed `Board`'s elements are always `Minion`.
- `Player.Sigils` (`List<Sigil>`) — consumed by ENT-006 (SigilAction.cs), WIRE-003, EXIT-001.

Files:
CardBattleEngine\Entities\Player.cs

Requirements:
1. Change the `Board` and `Graveyard` declarations. `Board` becomes `List<BoardPermanent>` (shared by Minions and Relics). `Graveyard` STAYS `List<Minion>` (Relics never go to the graveyard — see WIRE-007, they're just removed from `Board` with no deathrattle-equivalent).
```csharp
public List<BoardPermanent> Board { get; } = new List<BoardPermanent>();
public List<Minion> Graveyard { get; } = new List<Minion>();
```
2. Add a new property right next to the existing `Secrets` field:
```csharp
public List<Secret> Secrets { get; set; } = new List<Secret>();//TODO expand to hero auras
public List<Sigil> Sigils { get; set; } = new List<Sigil>();
```
3. In `Player.Clone()`, change the existing Board-cloning loop from:
```csharp
foreach (var minion in Board)
{
	Minion clonedMinion = minion.Clone();
	clone.Board.Add(clonedMinion);
	clonedMinion.Owner = clone;
}
```
to (the loop variable is now `BoardPermanent`, and `.Clone()` polymorphically returns a `Minion` or `Relic` matching the original's runtime type — `entity.Clone()` returns `BoardPermanent` when accessed through a `BoardPermanent`-typed variable, which is exactly what's needed here since we're re-adding it to `clone.Board`, also `List<BoardPermanent>`):
```csharp
foreach (var entity in Board)
{
	BoardPermanent clonedEntity = entity.Clone();
	clone.Board.Add(clonedEntity);
	clonedEntity.Owner = clone;
}
```
Leave the existing `foreach (var minion in Graveyard) { Minion clonedMinion = minion.Clone(); clone.Graveyard.Add(clonedMinion); clonedMinion.Owner = clone; }` block exactly as-is (unchanged — `Graveyard` is still `List<Minion>`).
4. Immediately after that Graveyard loop (and before `clone._modifiers = ...`), add a new loop to deep-copy Sigils:
```csharp
foreach (var sigil in Sigils)
{
	Sigil clonedSigil = sigil.Clone();
	clone.Sigils.Add(clonedSigil);
	clonedSigil.Owner = clone;
}
```
5. In `Player.LightClone()`, apply the same two changes: the Board-cloning loop (currently `foreach (var minion in Board) { Minion clonedMinion = minion.Clone(); clone.Board.Add(clonedMinion); clonedMinion.Owner = clone; }`) becomes the same `BoardPermanent`-typed version as step 3, and add the same Sigils-cloning loop as step 4 right after it (note: `LightClone()` intentionally skips `Graveyard`, commented out for rollout-speed reasons — `Sigils` represents live, currently-active hidden state analogous to `Board`, not historical state like `Graveyard`, so it MUST be included here even though `Graveyard` is skipped).

Constraints:
- Scope this diff to exactly this one file.
- `Secrets` is currently NOT deep-copied anywhere in `Clone()`/`LightClone()` (pre-existing gap, out of scope) — do not copy that gap's pattern for `Sigils`; Sigils must actually be cloned as specified.
- Do not remove or rename `Graveyard`, and do not change its element type.

Acceptance Criteria:
- `Player.Board` is `List<BoardPermanent>`. `Player.Graveyard` is still `List<Minion>`. `Player.Sigils` is `List<Sigil>`.
- `Clone()` and `LightClone()` both deep-copy `Board` polymorphically (calling `.Clone()` on each element, reassigning `Owner`) and deep-copy `Sigils` the same way.

Validation:
This file references `BoardPermanent`, `Relic`, and `Sigil`, created by sibling tasks in this same batch, and this change will break several OTHER existing files that assume `Board` is `List<Minion>` (tracked individually by WIRE-003, WIRE-004, WIRE-010, WIRE-013 in this same batch) — it cannot make the full solution build alone (expected). Confirm all five edits are present. Full solution build validated by EXIT-001.

Handoff:
WIRE-003, WIRE-004, WIRE-010, and WIRE-013 all depend on `Board`'s new element type (`BoardPermanent`) and must update their own files' Minion-specific member access accordingly. WIRE-007 (DeathAction) and WIRE-011/WIRE-012 (attack behaviors) depend on `Relic` living in `Board` rather than a separate list.
