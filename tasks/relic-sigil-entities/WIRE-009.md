Task ID:
WIRE-009

Model:
haiku

Title:
Add RelicCardDefinition and GetRelicCard to CardDatabase.cs (JSON-driven Relic cards)

Purpose:
`CardDatabase` loads `MinionCardDefinition`/`SpellCardDefinition` JSON files from disk and exposes `GetMinionCard`/`GetSpellCard` to instantiate playable `Card` instances from them. The project's new "Relic" card type needs the same data-driven loading path, mirroring `MinionCardDefinition`/`GetMinionCard` closely. Note: the existing `HeroPower` is NOT database/JSON-driven in this codebase (it's constructed directly in code) — following that precedent, this task also does NOT add JSON support for `RelicAbility`; only `Health`, `Charges`, and triggered effects are JSON-driven for now. This task is unrelated to the `Player.Board` type change happening elsewhere in this batch.

Dependencies:
PHASEB-EXIT (this task is part of the WIRE phase and must not start until the ENT phase checkpoint passes)

Consumes:
- `CardType.Relic` (produced by FOUND-002, running in parallel).
- `RelicCard` class (produced by ENT-003, running in parallel) — trust constructor `RelicCard(string name, int cost, int health)` and public `Charges` (`int?`), `RelicTriggeredEffects` (`List<TriggeredEffect>`) members exist.

Produces:
- `RelicCardDefinition` class, `CardDatabase.GetRelicCard(string id, Player owner)` method — consumed by EXIT-001 (tests).

Files:
CardBattleEngine\CardDatabase.cs

Requirements:
1. Add a new private field next to the existing `_minions`/`_spells` fields:
```csharp
private readonly Dictionary<string, MinionCardDefinition> _minions = new();
private readonly Dictionary<string, SpellCardDefinition> _spells = new();
private readonly Dictionary<string, RelicCardDefinition> _relics = new();
```
2. In `LoadAll`'s `switch (card)` block, add a new case next to the existing cases:
```csharp
switch (card)
{
	case MinionCardDefinition minion:
		Console.WriteLine($"loaded minion {minion.Id}");
		_minions[minion.Id] = minion;
		break;
	case SpellCardDefinition spell:
		Console.WriteLine($"loaded spell {spell.Id}");
		_spells[spell.Id] = spell;
		break;
	case RelicCardDefinition relic:
		Console.WriteLine($"loaded relic {relic.Id}");
		_relics[relic.Id] = relic;
		break;
}
```
3. In `LoadCardFromJson`'s `type switch`, add a new arm next to the existing arms:
```csharp
return type switch
{
	CardType.Minion => JsonConvert.DeserializeObject<MinionCardDefinition>(json, JsonSettings),
	CardType.Spell => JsonConvert.DeserializeObject<SpellCardDefinition>(json, JsonSettings),
	CardType.Relic => JsonConvert.DeserializeObject<RelicCardDefinition>(json, JsonSettings),
	_ => null
};
```
4. Add a new class near `MinionCardDefinition`:
```csharp
public class RelicCardDefinition : CardDefinition
{
	public int Health { get; set; }
	public int? Charges { get; set; }
	public List<TriggeredEffectDefinition> TriggeredEffectDefinitions { get; set; } = new();
}
```
5. Add a new public method right after `GetMinionCard`, mirroring it exactly in structure:
```csharp
public RelicCard GetRelicCard(string id, Player owner)
{
	if (!_relics.TryGetValue(id, out var def))
		throw new KeyNotFoundException($"Unknown relic id '{id}'");

	var card = new RelicCard(def.Name, def.Cost, def.Health);
	card.Owner = owner;
	card.Charges = def.Charges;

	foreach (var triggeredEffectDefinition in def.TriggeredEffectDefinitions)
	{
		List<ActionDefinition> actionDefinitions =
			(triggeredEffectDefinition.ActionDefintions != null && triggeredEffectDefinition.ActionDefintions.Count > 0)
				? triggeredEffectDefinition.ActionDefintions
				: new List<ActionDefinition> { triggeredEffectDefinition.ActionDefintion };

		List<IGameAction> actions = actionDefinitions
			.Select(ad => CreateGameActionFromDefinition(ad.GameActionTypeName, ad.Params))
			.ToList();

		TriggerConditionDefinition triggerConditionDefintion = triggeredEffectDefinition.TriggerConditionDefintion;
		var condition = CreateTriggerConditionFromDefinition(
			triggerConditionDefintion?.ConditionTypeName,
			triggerConditionDefintion?.Params);

		TriggeredEffect effect = new()
		{
			EffectTiming = triggeredEffectDefinition.EffectTiming,
			EffectTrigger = triggeredEffectDefinition.EffectTrigger,
			Scope = triggeredEffectDefinition.Scope,
			Condition = condition,
			GameActions = actions,
			AffectedEntitySelector = CreateAffectedEntitySelectorFromDefinition(
				triggeredEffectDefinition.AffectedEntitySelectorDefinition?.EntitySelectorTypeName,
				triggeredEffectDefinition.AffectedEntitySelectorDefinition?.Params
				)
		};
		card.RelicTriggeredEffects.Add(effect);
	}

	return card;
}
```

Constraints:
- Scope this diff to exactly this one file, exactly the five additions above.
- Do not add any `RelicAbility`/JSON serialization support.
- `CreateGameActionFromDefinition`, `CreateTriggerConditionFromDefinition`, and `CreateAffectedEntitySelectorFromDefinition` already exist on this class — reuse them exactly as `GetMinionCard` does.

Acceptance Criteria:
- `RelicCardDefinition` class exists with `Health`, `Charges`, `TriggeredEffectDefinitions`.
- `GetRelicCard` throws `KeyNotFoundException` for an unknown id, otherwise returns a populated `RelicCard` mirroring `GetMinionCard`'s exact structure.
- A relic JSON file with `"type": "Relic"` is correctly routed to `RelicCardDefinition`.

Validation:
This file references `RelicCard` and `CardType.Relic`, created by sibling tasks in this same batch — it cannot build alone in isolation (expected). Confirm all five additions mirror `GetMinionCard`'s structure. Full solution build and behavior validated by EXIT-001.

Handoff:
EXIT-001's tests exercise `GetRelicCard` directly, mirroring how `CardDBTest.cs` exercises `GetMinionCard`.
