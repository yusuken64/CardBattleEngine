Task ID:
WIRE-003

Model:
haiku

Title:
Fix GetAllMinions() to filter Board by type, and add Sigils to GetAllTriggerSources in Engine/GameState.cs

Purpose:
`Player.Board` is changing from `List<Minion>` to `List<BoardPermanent>` (WIRE-002), shared by both `Minion` and the project's new `Relic` entity. `GameState.GetAllMinions()` currently assumes every `Board` element is a `Minion` and returns `IEnumerable<Minion>` directly from it — that no longer compiles once `Board` can contain Relics too, so it needs an `OfType<Minion>()` filter. Separately, the project's new "Sigil" entity (hidden trigger holder, not part of `Board`) needs to be included in `GetAllTriggerSources()` so its triggered effects actually fire. Note: `RebuildEntityMap` and `GetAllEntities` do NOT need any change — their existing `foreach (var minion in player.Board) ...` loops already work correctly once `Board`'s element type changes, because both `Minion` and `Relic` already satisfy `IGameEntity` (the loop variable's members used there — `.Id`, or just yielding it directly — don't touch any Minion-only member).

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `Player.Board` retyped to `List<BoardPermanent>` (produced by WIRE-002, running in parallel).
- `Player.Sigils` (`List<Sigil>`, produced by WIRE-002, running in parallel).
- `Sigil` class (produced by FOUND-006, running in parallel) — trust it implements `ITriggerSource`.

Produces:
- Fixed `GetAllMinions()` (still returns `IEnumerable<Minion>`) — consumed by WIRE-005 (EventBus's modifier-triggered-effects loop, unaffected in signature, already calls this method).
- Updated `GetAllTriggerSources()` (now also yields Sigils) — consumed by WIRE-004 (StartTurnAction's OncePerTurn reset), WIRE-005 (EventBus's trigger lookup), EXIT-001.

Files:
CardBattleEngine\Engine\GameState.cs

Requirements:
1. Change `GetAllMinions()` from:
```csharp
public IEnumerable<Minion> GetAllMinions()
{
	var all = new List<Minion>();

	foreach (var player in Players)
	{
		all.AddRange(player.Board);
	}

	return all;
}
```
to:
```csharp
public IEnumerable<Minion> GetAllMinions()
{
	var all = new List<Minion>();

	foreach (var player in Players)
	{
		all.AddRange(player.Board.OfType<Minion>());
	}

	return all;
}
```
2. In `GetAllTriggerSources`, add a Sigils loop right next to the existing Secrets loop:
```csharp
public IEnumerable<ITriggerSource> GetAllTriggerSources()
{
	foreach (var player in Players)
	{
		yield return player;

		foreach (var secret in player.Secrets)
		{
			yield return secret;
		}

		foreach (var sigil in player.Sigils)
		{
			yield return sigil;
		}

		foreach (var minion in player.Board)
		{
			yield return minion;
		}

		foreach (var card in player.Hand)
		{
			yield return card;
		}
	}
}
```
Note the `foreach (var minion in player.Board)` loop is otherwise UNCHANGED — it already yields every `BoardPermanent` (Minion or Relic) correctly as an `ITriggerSource`, since `BoardPermanent` implements that interface. Do not add a separate Relic loop; `Board`'s loop already covers Relics.

Constraints:
- Scope this diff to exactly these two methods (`GetAllMinions`, `GetAllTriggerSources`). Do NOT touch `RebuildEntityMap` or `GetAllEntities` — they need no change.
- Do not add a `GetAllRelics()` helper — nothing in this batch needs it.

Acceptance Criteria:
- `GetAllMinions()` still returns only actual `Minion` instances (never a `Relic`), across both players.
- `GetAllTriggerSources()` yields every `Sigil`, in addition to everything it already yielded.

Validation:
This file depends on `Player.Board`'s new element type and `Player.Sigils`, both changed/added by WIRE-002, a sibling task in this same batch — it cannot build alone in isolation (expected). Confirm both edits are present and `RebuildEntityMap`/`GetAllEntities` are left untouched. Full solution build validated by EXIT-001.

Handoff:
WIRE-004 and WIRE-005 rely on `GetAllTriggerSources()` yielding Sigils (and Relics, via the untouched `Board` loop) for their once-per-turn reset/filter logic to reach those entities.
