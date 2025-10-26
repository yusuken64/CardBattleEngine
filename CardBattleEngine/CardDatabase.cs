using Newtonsoft.Json;

namespace CardBattleEngine;

public class CardDatabase
{
	private readonly Dictionary<string, CardDefinition> _minions = new();

	public CardDatabase(string path)
	{
		LoadAll(path);

		RegisterGameActions();
	}

	private static void RegisterGameActions()
	{
		// register automatically
		foreach (var t in typeof(IGameAction).Assembly.GetTypes())
		{
			if (typeof(IGameAction).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
				_actions[t.Name] = t;
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
			TriggeredEffectDefinitions = card.TriggeredEffects.Select(x => new TriggeredEffectDefinition()
			{
				EffectTiming = x.EffectTiming,
				EffectTrigger = x.EffectTrigger,
				TargetType = x.TargetType,
				ActionDefintion = new()
				{
					GameActionTypeName = x.GameActions[0].GetType().Name,
					Params = x.GameActions[0].EmitParams()
				}
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
				TypeNameHandling = TypeNameHandling.Auto
			});
			if (card != null)
				_minions[card.Id] = card;
		}
	}

	public MinionCard GetMinion(string id, Player owner)
	{
		if (!_minions.TryGetValue(id, out var def))
			throw new KeyNotFoundException($"Unknown minion id '{id}'");

		var card = new MinionCard(def.Name, def.Cost, def.Attack, def.Health);
		card.Owner = owner;

		foreach (var triggeredEffectDefinition in def.TriggeredEffectDefinitions)
		{
			ActionDefinition actionDefintion = triggeredEffectDefinition.ActionDefintion;
			var action = CreateFromDefinition(
				actionDefintion.GameActionTypeName,
				actionDefintion.Params);
			TriggeredEffect effect = new()
			{
				EffectTiming = triggeredEffectDefinition.EffectTiming,
				EffectTrigger = triggeredEffectDefinition.EffectTrigger,
				TargetType = triggeredEffectDefinition.TargetType,
				GameActions = [action],
			};
			card.TriggeredEffects.Add(effect);
		}

		return card;
	}

	private static readonly Dictionary<string, Type> _actions = new();

	public static IGameAction CreateFromDefinition(string typeName, Dictionary<string, object> paramObj)
	{
		if (!_actions.TryGetValue(typeName, out var t))
			throw new Exception($"Unknown action: {typeName}");

		var action = (IGameAction)Activator.CreateInstance(t)!;
		action.ConsumeParams(paramObj);
		return action;
	}
}

public class CardDefinition
{
	public string Id { get; set; }
	public string Name { get; set; }
	public int Cost { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; }
	public string Tribe { get; set; }
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
	public ActionDefinition ActionDefintion { get; set; }
}

public class ActionDefinition
{
	public string GameActionTypeName { get; set; }
	public Dictionary<string, object> Params { get; set; }
}

//public class ConditionDefinition
//{
//}