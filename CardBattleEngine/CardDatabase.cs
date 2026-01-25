using System.IO;


namespace CardBattleEngine;

public class CardDatabase
{
	private readonly Dictionary<string, MinionCardDefinition> _minions = new();
	private readonly Dictionary<string, SpellCardDefinition> _spells = new();

	private readonly Dictionary<string, Type> _actions = new();
	private readonly Dictionary<string, Type> _triggerConditions = new();
	private readonly Dictionary<string, Type> _affectedEntitySelectors = new();
	private readonly Dictionary<string, Type> _targetOperations = new();

	public CardDatabase(string path)
	{
		LoadAll(path);
		RegisterTypes<IGameAction>(_actions);
		RegisterTypes<ITriggerCondition>(_triggerConditions);
		RegisterTypes<IAffectedEntitySelector>(_affectedEntitySelectors);
		RegisterTypes<ITargetOperation>(_targetOperations);
	}

	private void RegisterTypes<T>(Dictionary<string, Type> targetDictionary)
	{
		var assembly = typeof(T).Assembly;

		foreach (var type in assembly.GetTypes())
		{
			if (typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
			{
				targetDictionary[type.Name] = type;
			}
		}
	}

	public static string CreateFileFromMinionCard(MinionCard card, string directory, string cardName)
	{
		Directory.CreateDirectory(directory);

		var def = new MinionCardDefinition
		{
			Id = cardName,
			Name = card.Name,
			Cost = card.ManaCost,
			Attack = card.Attack,
			Health = card.Health,
			Tribes = card.MinionTribes?.ToList(),
			TriggeredEffectDefinitions = card.TriggeredEffects.Select(x =>
			{
				TriggerConditionDefinition cond = null;

				if (x.Condition != null)
				{
					cond = new TriggerConditionDefinition
					{
						ConditionTypeName = x.Condition.GetType().Name,
						Params = x.Condition.EmitParams()
					};
				}

				return new TriggeredEffectDefinition
				{
					EffectTiming = x.EffectTiming,
					EffectTrigger = x.EffectTrigger,
					TriggerConditionDefintion = cond,
					ActionDefintion = new ActionDefinition
					{
						GameActionTypeName = x.GameActions[0].GetType().Name,
						Params = x.GameActions[0].EmitParams()
					},
					AffectedEntitySelectorDefinition = new AffectedEntitySelectorDefinition()
					{
						EntitySelectorTypeName = x.AffectedEntitySelector?.GetType().Name,
						Params = x.AffectedEntitySelector?.EmitParams()
					}
				};
			}).ToList()
		};

		string json = JsonConvert.SerializeObject(def, JsonSettings);
		File.WriteAllText(Path.Combine(directory, $"{cardName}.json"), json);

		return json;
	}

	public static string CreateJsonFromSpellCard(SpellCard card, string cardName)
	{
		if (card == null) throw new ArgumentNullException(nameof(card));
		if (string.IsNullOrWhiteSpace(cardName)) throw new ArgumentNullException(nameof(cardName));

		var def = new SpellCardDefinition
		{
			Id = cardName,
			Name = card.Name,
			Cost = card.ManaCost,
			Type = card.Type,
			SpellCastEffectDefinitions = card.SpellCastEffects.Select(x =>
			{
				AffectedEntitySelectorDefinition? affectedEntitySelectorDefinition = null;
				if (x.AffectedEntitySelector != null)
				{
					affectedEntitySelectorDefinition = new AffectedEntitySelectorDefinition
					{
						EntitySelectorTypeName = x.AffectedEntitySelector.GetType().Name,
						Params = x.AffectedEntitySelector.EmitParams(),
					};
				}

				return new SpellCastEffectDefinition
				{
					AffectedEntitySelectorDefinition = affectedEntitySelectorDefinition,
					ActionDefintions = x.GameActions.Select(ga => new ActionDefinition
					{
						GameActionTypeName = ga.GetType().Name,
						Params = ga.EmitParams()
					}).ToList(),
				};
			}).ToList()
		};

		string json = JsonConvert.SerializeObject(def, JsonSettings);

		return json;
	}

	public static void WriteJsonToFile(string json, string directory, string cardName)
	{
		if (string.IsNullOrWhiteSpace(json)) throw new ArgumentNullException(nameof(json));
		if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentNullException(nameof(directory));
		if (string.IsNullOrWhiteSpace(cardName)) throw new ArgumentNullException(nameof(cardName));

		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, $"{cardName}.json");
		File.WriteAllText(path, json);
	}
	protected void LoadAll(string directory)
	{
		Console.WriteLine($"Loading Directory {directory}");

		foreach (var file in Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories))
		{
			string json;
			try
			{
				json = File.ReadAllText(file);
			}
			catch (IOException ex)
			{
				Console.WriteLine($"Failed to read file {file}: {ex.Message}");
				continue;
			}

			var card = LoadCardFromJson(json);
			if (card == null) continue;

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
			}
		}
	}

	public static CardDefinition? LoadCardFromJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			throw new ArgumentException(nameof(json));

		JObject root;

		try
		{
			root = JObject.Parse(json);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Invalid JSON: {ex.Message}");
			return null;
		}

		string typeString =
			root["type"]?.ToString()
			?? root["Type"]?.ToString();

		if (!Enum.TryParse(typeString, true, out CardType type))
		{
			Console.WriteLine("Missing or invalid CardType.");
			return null;
		}

		try
		{
			return type switch
			{
				CardType.Minion => JsonConvert.DeserializeObject<MinionCardDefinition>(json, JsonSettings),
				CardType.Spell => JsonConvert.DeserializeObject<SpellCardDefinition>(json, JsonSettings),
				_ => null
			};
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed to deserialize: {ex.Message}");
			return null;
		}
	}

	private static readonly JsonSerializerSettings JsonSettings = new()
	{
		Formatting = Formatting.Indented,
		TypeNameHandling = TypeNameHandling.Auto,
		NullValueHandling = NullValueHandling.Ignore,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		//ContractResolver = new CamelCasePropertyNamesContractResolver()
	};

	public MinionCard GetMinionCard(string id, Player owner)
	{
		if (!_minions.TryGetValue(id, out var def))
			throw new KeyNotFoundException($"Unknown minion id '{id}'");

		var card = new MinionCard(def.Name, def.Cost, def.Attack, def.Health);
		card.Owner = owner;
		card.MinionTribes = def.Tribes == null ?[MinionTribe.None] : def.Tribes.ToList();

		foreach (var triggeredEffectDefinition in def.TriggeredEffectDefinitions)
		{
			ActionDefinition actionDefintion = triggeredEffectDefinition.ActionDefintion;
			var action = CreateGameActionFromDefinition(
				actionDefintion.GameActionTypeName,
				actionDefintion.Params);

			TriggerConditionDefinition triggerConditionDefintion = triggeredEffectDefinition.TriggerConditionDefintion;
			var condition = CreateTriggerConditionFromDefinition(
				triggerConditionDefintion?.ConditionTypeName,
				triggerConditionDefintion?.Params);

			TriggeredEffect effect = new()
			{
				EffectTiming = triggeredEffectDefinition.EffectTiming,
				EffectTrigger = triggeredEffectDefinition.EffectTrigger,
				Condition = condition,
				GameActions = [action],
				AffectedEntitySelector = CreateAffectedEntitySelectorFromDefinition(
					triggeredEffectDefinition.AffectedEntitySelectorDefinition?.EntitySelectorTypeName,
					triggeredEffectDefinition.AffectedEntitySelectorDefinition?.Params
					)
			};
			card.TriggeredEffects.Add(effect);
		}

		return card;
	}
	public SpellCard GetSpellCard(string id, Player owner)
	{
		if (!_spells.TryGetValue(id, out var def))
			throw new KeyNotFoundException($"Unknown spell id '{id}'");

		var card = new SpellCard(def.Name, def.Cost);
		card.Owner = owner;

		foreach (var spellCastEffectDefinition in def.SpellCastEffectDefinitions)
		{
			IEnumerable<IGameAction> gameActions = 
				spellCastEffectDefinition.ActionDefintions
				.Select(x => CreateGameActionFromDefinition(
						x.GameActionTypeName,
						x.Params)
				);

			IAffectedEntitySelector affectedEntitySelector = null;
			if (spellCastEffectDefinition.AffectedEntitySelectorDefinition != null)
			{
				affectedEntitySelector =
					CreateAffectedEntitySelectorFromDefinintion(
						spellCastEffectDefinition.AffectedEntitySelectorDefinition.EntitySelectorTypeName,
						spellCastEffectDefinition.AffectedEntitySelectorDefinition.Params
					);
			}
			SpellCastEffect effect = new()
			{
				GameActions = gameActions.ToList(),
				AffectedEntitySelector = affectedEntitySelector
			};
			card.SpellCastEffects.Add(effect);
		}

		return card;
	}

	private IAffectedEntitySelector CreateAffectedEntitySelectorFromDefinintion(
		string entitySelectorTypeName,
		Dictionary<string, object> paramObj)
	{
		if (!_affectedEntitySelectors.TryGetValue(entitySelectorTypeName, out var t))
			throw new Exception($"Unknown entitySelector: {entitySelectorTypeName}");

		var instance = (IAffectedEntitySelector)Activator.CreateInstance(t)!;

		instance.ConsumeParams(paramObj);
		return instance;
	}

	public IGameAction CreateGameActionFromDefinition(string typeName, Dictionary<string, object> paramObj)
	{
		if (!_actions.TryGetValue(typeName, out var t))
			throw new Exception($"Unknown entitySelector: {typeName}");

		var action = (IGameAction)Activator.CreateInstance(t)!;
		action.ConsumeParams(paramObj);
		return action;
	}

	public ITriggerCondition CreateTriggerConditionFromDefinition(string typeName, Dictionary<string, object> paramObj)
	{
		if (typeName == null) { return null; }

		if (!_triggerConditions.TryGetValue(typeName, out var t))
			throw new Exception($"Unknown entitySelector: {typeName}");

		var triggerCondition = (ITriggerCondition)Activator.CreateInstance(t)!;
		triggerCondition.ConsumeParams(JsonParamHelper.Normalize(paramObj));
		return triggerCondition;
	}

	public IAffectedEntitySelector CreateAffectedEntitySelectorFromDefinition(string typeName, Dictionary<string, object> paramObj)
	{
		if (typeName == null) { return null; }

		if (!_affectedEntitySelectors.TryGetValue(typeName, out var t))
			throw new Exception($"Unknown entitySelector: {typeName}");

		var entitySelector = (IAffectedEntitySelector)Activator.CreateInstance(t)!;
		entitySelector.ConsumeParams(JsonParamHelper.Normalize(paramObj));
		return entitySelector;
	}
}

public abstract class CardDefinition
{
	public CardType Type { get; set; }
	public string Id { get; set; }
	public string Name { get; set; }
	public int Cost { get; set; }
}

public class SpellCardDefinition : CardDefinition
{
	public List<SpellCastEffectDefinition> SpellCastEffectDefinitions { get; set; }
}

public class SpellCastEffectDefinition
{
	public List<ActionDefinition> ActionDefintions { get; set; } = new();
	public AffectedEntitySelectorDefinition AffectedEntitySelectorDefinition { get; set; }
}

public class MinionCardDefinition : CardDefinition
{
	public int Attack { get; set; }
	public int Health { get; set; }
	public List<MinionTribe> Tribes { get; set; }
	public List<TriggeredEffectDefinition> TriggeredEffectDefinitions { get; set; } = new();
}

public class TriggeredEffectDefinition
{
	public EffectTiming EffectTiming { get; set; }
	public EffectTrigger EffectTrigger { get; set; }
	public TriggerConditionDefinition TriggerConditionDefintion { get; set; }
	public ActionDefinition ActionDefintion { get; set; }
	public AffectedEntitySelectorDefinition AffectedEntitySelectorDefinition { get; set; }
}

public class ActionDefinition
{
	public string GameActionTypeName { get; set; }
	public Dictionary<string, object> Params { get; set; }
}

public class TriggerConditionDefinition
{
	public string ConditionTypeName { get; set; }
	public Dictionary<string, object> Params { get; set; }
}

public class AffectedEntitySelectorDefinition
{
	public string EntitySelectorTypeName { get; set; }
	public Dictionary<string, object> Params { get; set; } = new();
}
