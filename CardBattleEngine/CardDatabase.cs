using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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
		var path = Path.Combine(directory, $"{cardName}.json");

		if (card == null) throw new ArgumentNullException(nameof(card));
		if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

		// Convert MinionCard -> CardDefinition for serialization
		var def = new MinionCardDefinition
		{
			Id = cardName,
			Name = card.Name,
			Cost = card.ManaCost,
			Attack = card.Attack,
			Health = card.Health,
			Tribe = card.MinionTribe,
			TriggeredEffectDefinitions = card.TriggeredEffects.Select(x =>
			{
				TriggerConditionDefinition conditionDef = null;
				
				if (x.Condition != null)
				{
					conditionDef = new TriggerConditionDefinition
					{
						ConditionTypeName = x.Condition.GetType().Name,
						Params = x.Condition.EmitParams()
					};
				}

				return new TriggeredEffectDefinition()
				{
					EffectTiming = x.EffectTiming,
					EffectTrigger = x.EffectTrigger,
					TargetType = x.TargetType,
					TriggerConditionDefintion = conditionDef,
					ActionDefintion = new()
					{
						GameActionTypeName = x.GameActions[0].GetType().Name,
						Params = x.GameActions[0].EmitParams()
					}
				};
			}).ToList(),
		};

		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			TypeInfoResolver = new DefaultJsonTypeInfoResolver
			{
				Modifiers =
		{
			static typeInfo =>
			{
				// Equivalent to TypeNameHandling.Auto: include type discriminator for polymorphic types
				if (typeInfo.Kind == JsonTypeInfoKind.Object && typeInfo.Type.IsAbstract)
				{
					typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
					{
						TypeDiscriminatorPropertyName = "$type",
						IgnoreUnrecognizedTypeDiscriminators = true
					};
				}
			}
		}
			}
		};

		// Serialize
		string json = JsonSerializer.Serialize(def, options);

		File.WriteAllText(path, json);

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
			TargetingType = card.TargetingType,
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

		var options = new JsonSerializerOptions
		{
			WriteIndented = true,
			TypeInfoResolver = new DefaultJsonTypeInfoResolver
			{
				Modifiers =
		{
			static typeInfo =>
			{
				// Equivalent to TypeNameHandling.Auto: include type discriminator for polymorphic types
				if (typeInfo.Kind == JsonTypeInfoKind.Object && typeInfo.Type.IsAbstract)
				{
					typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
					{
						TypeDiscriminatorPropertyName = "$type",
						IgnoreUnrecognizedTypeDiscriminators = true
					};
				}
			}
		}
			}
		};

		string json = JsonSerializer.Serialize(def, options);

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
					_minions[minion.Id] = minion;
					break;
				case SpellCardDefinition spell:
					_spells[spell.Id] = spell;
					break;
			}
		}
	}
	public static CardDefinition? LoadCardFromJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			throw new ArgumentException("JSON string cannot be null or empty.", nameof(json));

		JsonNode? root;
		try
		{
			root = JsonNode.Parse(json);
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"Failed to parse JSON: {ex.Message}");
			return null;
		}

		if (root?["Type"] is null || !Enum.TryParse(root["Type"]!.ToString(), out CardType cardType))
		{
			Console.WriteLine("Missing or invalid CardType in JSON.");
			return null;
		}

		try
		{
			return cardType switch
			{
				CardType.Minion => root.Deserialize<MinionCardDefinition>(JsonOptions),
				CardType.Spell => root.Deserialize<SpellCardDefinition>(JsonOptions),
				_ => throw new NotSupportedException($"Unsupported CardType: {cardType}")
			};
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"Failed to deserialize card: {ex.Message}");
			return null;
		}
	}

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		PropertyNameCaseInsensitive = true,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver
		{
			Modifiers =
		{
			static typeInfo =>
			{
				// Add type discriminator support for polymorphic serialization (if needed)
				if (typeInfo.Kind == JsonTypeInfoKind.Object && typeInfo.Type.IsAbstract)
				{
					typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
					{
						TypeDiscriminatorPropertyName = "$type",
						IgnoreUnrecognizedTypeDiscriminators = true
					};
				}
			}
		}
		}
	};

	public MinionCard GetMinionCard(string id, Player owner)
	{
		if (!_minions.TryGetValue(id, out var def))
			throw new KeyNotFoundException($"Unknown minion id '{id}'");

		var card = new MinionCard(def.Name, def.Cost, def.Attack, def.Health);
		card.Owner = owner;
		card.MinionTribe = def.Tribe;

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
				TargetType = triggeredEffectDefinition.TargetType,
				Condition = condition,
				GameActions = [action],
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
		card.TargetingType = def.TargetingType;

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

	private IAffectedEntitySelector CreateAffectedEntitySelectorFromDefinintion(string entitySelectorTypeName, List<SerializedOperation> paramObj)
	{
		if (!_affectedEntitySelectors.TryGetValue(entitySelectorTypeName, out var t))
			throw new Exception($"Unknown triggerCondition: {entitySelectorTypeName}");

		var instance = (IAffectedEntitySelector)Activator.CreateInstance(t)!;
		List<ITargetOperation> targetOperations = paramObj.Select(x =>
		{
			var targetOperationType =_targetOperations[x.Type];
			var targetOperation = (ITargetOperation)Activator.CreateInstance(targetOperationType)!;
			targetOperation.ConsumeParams(x.Params);

			return targetOperation;
		}).ToList();

		instance.ConsumeParams(targetOperations);
		return instance;
	}

	public IGameAction CreateGameActionFromDefinition(string typeName, Dictionary<string, object> paramObj)
	{
		if (!_actions.TryGetValue(typeName, out var t))
			throw new Exception($"Unknown triggerCondition: {typeName}");

		var action = (IGameAction)Activator.CreateInstance(t)!;
		action.ConsumeParams(paramObj);
		return action;
	}

	public ITriggerCondition CreateTriggerConditionFromDefinition(string typeName, Dictionary<string, object> paramObj)
	{
		if (typeName == null) { return null; }

		if (!_triggerConditions.TryGetValue(typeName, out var t))
			throw new Exception($"Unknown triggerCondition: {typeName}");

		var triggerCondition = (ITriggerCondition)Activator.CreateInstance(t)!;
		triggerCondition.ConsumeParams(paramObj);
		return triggerCondition;
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
	public TargetingType TargetingType { get; set; }
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
	public MinionTribe Tribe { get; set; }
	public List<TriggeredEffectDefinition> TriggeredEffectDefinitions { get; set; } = new();
}

public class TriggeredEffectDefinition
{
	public EffectTiming EffectTiming { get; set; }
	public EffectTrigger EffectTrigger { get; set; }
	public TargetingType TargetType { get; set; }
	public TriggerConditionDefinition TriggerConditionDefintion { get; set; }
	public ActionDefinition ActionDefintion { get; set; }
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
	public List<SerializedOperation> Params { get; set; } = new();
}
