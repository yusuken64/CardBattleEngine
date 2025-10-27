using Newtonsoft.Json;

namespace CardBattleEngine;

public class CardDatabase
{
	private readonly Dictionary<string, CardDefinition> _minions = new();

	private readonly Dictionary<string, Type> _actions = new();
	private readonly Dictionary<string, Type> _triggerConditions = new();


	public CardDatabase(string path)
	{
		LoadAll(path);
		RegisterGameActions();
	}

	private void RegisterGameActions()
	{
		// register automatically
		foreach (var t in typeof(IGameAction).Assembly.GetTypes())
		{
			if (typeof(IGameAction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				_actions[t.Name] = t;
		}

		foreach (var t in typeof(ITriggerCondition).Assembly.GetTypes())
		{
			if (typeof(ITriggerCondition).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				_triggerConditions[t.Name] = t;
		}
	}

	public static string CreateFileFromMinionCard(MinionCard card, string directory, string cardName)
	{
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, $"{cardName}.json");

		if (card == null) throw new ArgumentNullException(nameof(card));
		if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));

		// Convert MinionCard -> CardDefinition for serialization
		var def = new CardDefinition
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

		var json = JsonConvert.SerializeObject(def, Formatting.Indented, new JsonSerializerSettings
		{
			TypeNameHandling = TypeNameHandling.Auto
		});

		File.WriteAllText(path, json);

		return json;
	}

	protected void LoadAll(string directory)
	{
		foreach (var file in Directory.GetFiles(directory, "*.json"))
		{
			var json = File.ReadAllText(file);
			var card = JsonConvert.DeserializeObject<CardDefinition>(json, new JsonSerializerSettings
			{
				TypeNameHandling = TypeNameHandling.Auto,
				NullValueHandling = NullValueHandling.Ignore
			});
			if (card != null)
				_minions[card.Id] = card;
		}
	}

	public MinionCard GetMinionCard(string id, Player owner)
	{
		if (!_minions.TryGetValue(id, out var def))
			throw new KeyNotFoundException($"Unknown minion id '{id}'");

		var card = new MinionCard(def.Name, def.Cost, def.Attack, def.Health);
		card.Owner = owner;

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

public class CardDefinition
{
	public string Id { get; set; }
	public string Name { get; set; }
	public int Cost { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; }
	public MinionTribe Tribe { get; set; }
	public List<TriggeredEffectDefinition> TriggeredEffectDefinitions { get; set; } = new();
}

public class TriggeredEffectDefinition
{
	//public string Trigger { get; set; } // e.g. Battlecry
	//public ConditionDefinition Condition { get; set; }
	//public ActionDefinition Action { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public EffectTrigger EffectTrigger { get; set; }
	public TargetType TargetType { get; set; }
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