Task ID:
WIRE-013

Model:
haiku

Title:
Fix GameStateVectorizer.ToVector for the new Board type in AI/GameStateVectorizer.cs

Purpose:
`Player.Board` is changing from `List<Minion>` to `List<BoardPermanent>` (WIRE-002), shared with the project's new `Relic` entity. `GameStateVectorizer.ToVector` reads `.Taunt` and `.HasDivineShield` directly off `Board` elements (Minion-only members) — that stops compiling once `Board` can contain Relics. This task is a minimal compile fix only: the AI vectorizer is intentionally NOT being made Relic-aware in this batch (no new features vs. today's Minion-only vectorization) — a Relic occupying a board slot should just report `0` for the Minion-only stats it doesn't have, and report its actual `Attack`/`Health`/`CanAttack()` (all present on `BoardPermanent`) like any other board entity.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `Player.Board` retyped to `List<BoardPermanent>` (produced by WIRE-002, running in parallel).
- `Relic`, `Minion` types (already exist / produced by ENT-001/ENT-002).

Produces:
(none — leaf compile fix)

Files:
CardBattleEngine\AI\GameStateVectorizer.cs

Requirements:
In `ToVector`, both board loops ("--- FRIENDLY BOARD ---" and "--- ENEMY BOARD ---") currently do:
```csharp
if (slot < current.Board.Count)
{
	var m = current.Board[slot];

	f[i++] = 1f;
	f[i++] = m.Attack / 10f;
	f[i++] = m.Health / 10f;
	f[i++] = m.CanAttack() ? 1f : 0f;
	f[i++] = m.Taunt ? 1f : 0f;
	f[i++] = m.HasDivineShield ? 1f : 0f;
}
else
{
	i += 6; // empty slot
}
```
(the enemy loop is identical, reading `opponent.Board[slot]` instead of `current.Board[slot]`). Change the `.Taunt`/`.HasDivineShield` lines in BOTH loops (friendly and enemy) to guard with an `is Minion` pattern, since those two properties don't exist on `BoardPermanent`:
```csharp
if (slot < current.Board.Count)
{
	var m = current.Board[slot];

	f[i++] = 1f;
	f[i++] = m.Attack / 10f;
	f[i++] = m.Health / 10f;
	f[i++] = m.CanAttack() ? 1f : 0f;
	f[i++] = m is Minion friendlyMinion && friendlyMinion.Taunt ? 1f : 0f;
	f[i++] = m is Minion friendlyMinionDs && friendlyMinionDs.HasDivineShield ? 1f : 0f;
}
else
{
	i += 6; // empty slot
}
```
Apply the same pattern to the enemy board loop (using distinct variable names, e.g. `enemyMinion`/`enemyMinionDs`, to avoid name collisions with the friendly loop's variables in the same method scope). Do not change `f[i++] = m.Attack / 10f;`, `f[i++] = m.Health / 10f;`, or `f[i++] = m.CanAttack() ? 1f : 0f;` — those already work unchanged since `Attack`, `Health`, `CanAttack()` are all declared on `BoardPermanent`.

Do not change `GetSourceIndex`, `GetTargetIndex`, `ResolveSourceFromIndex`, or `ResolveTargetFromIndex` — they already use `entity as Minion` / return `IGameEntity`, both of which remain valid once `Board`'s element type changes, with no code changes required.

Constraints:
- Scope this diff to exactly the two `.Taunt`/`.HasDivineShield` lines in each of the two board loops (four lines total).
- Do not add any Relic-specific feature encoding (e.g. a "this slot is a Relic" flag) — out of scope for this batch; just make it compile and degrade gracefully (Relics report 0 for Taunt/DivineShield).

Acceptance Criteria:
- `ToVector` compiles against `Player.Board : List<BoardPermanent>`.
- For a `Minion` in a board slot, the vector's Taunt/DivineShield features are unchanged from today's behavior.
- For a `Relic` in a board slot, the vector's Taunt/DivineShield features are `0f`, and Attack/Health/CanAttack features reflect the Relic's actual values.

Validation:
This file depends on `Player.Board`'s new element type from WIRE-002 — it cannot build alone in isolation until that lands (expected). Confirm the four-line change is present in both loops. Full solution build validated by EXIT-001.

Handoff:
None — leaf task, no downstream consumers in this batch.
