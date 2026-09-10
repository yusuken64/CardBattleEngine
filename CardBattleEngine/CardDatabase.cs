using System.IO;


namespace CardBattleEngine;

public class CardDatabase
{
	private readonly Dictionary<string, MinionCardDefinition> _minions = new();
	private readonly Dictionary<string, SpellCardDefinition> _spells = new();
	private readonly Dictionary<string, WeaponCardDefinition> _weapons = new();
	private readonly Dictionary<string, RelicCardDefinition> _relics = new();

	public CardDatabase(string path)
	{
		LoadAll(path);
	}

	public static MinionCardDefinition ToMinionCardDefinition(MinionCard card, string id)
	{
		return new MinionCardDefinition
		{
			Type = CardType.Minion,
			Id = id,
			Name = card.Name,
			Cost = card.ManaCost,
			Attack = card.Attack,
			Health = card.Health,
			Tribes = card.MinionTribes?.ToList(),
			CastRestriction = card.CastRestriction,
			ValidTargetSelector = card.ValidTargetSelector,
			TriggeredEffects = card.TriggeredEffects.ToList()
		};
	}

	public static SpellCardDefinition ToSpellCardDefinition(SpellCard card, string id)
	{
		return new SpellCardDefinition
		{
			Type = CardType.Spell,
			Id = id,
			Name = card.Name,
			Cost = card.ManaCost,
			CastRestriction = card.CastRestriction,
			ValidTargetSelector = card.ValidTargetSelector,
			SpellCastEffects = card.SpellCastEffects.ToList()
		};
	}

	public static WeaponCardDefinition ToWeaponCardDefinition(WeaponCard card, string id)
	{
		return new WeaponCardDefinition
		{
			Type = CardType.Weapon,
			Id = id,
			Name = card.Name,
			Cost = card.ManaCost,
			Attack = card.Attack,
			Durability = card.Durability,
			CastRestriction = card.CastRestriction,
			ValidTargetSelector = card.ValidTargetSelector,
			TriggeredEffects = card.TriggeredEffects.ToList()
		};
	}

	public static string ToDefinitionJson(CardDefinition def)
	{
		return JsonConvert.SerializeObject(def, JsonSettings);
	}

	public static string CreateFileFromMinionCard(MinionCard card, string directory, string cardName)
	{
		Directory.CreateDirectory(directory);

		string json = ToDefinitionJson(ToMinionCardDefinition(card, cardName));
		File.WriteAllText(Path.Combine(directory, $"{cardName}.json"), json);

		return json;
	}

	public static string CreateJsonFromSpellCard(SpellCard card, string cardName)
	{
		if (card == null) throw new ArgumentNullException(nameof(card));
		if (string.IsNullOrWhiteSpace(cardName)) throw new ArgumentNullException(nameof(cardName));

		return ToDefinitionJson(ToSpellCardDefinition(card, cardName));
	}

	public static string CreateJsonFromWeaponCard(WeaponCard card, string cardName)
	{
		if (card == null) throw new ArgumentNullException(nameof(card));
		if (string.IsNullOrWhiteSpace(cardName)) throw new ArgumentNullException(nameof(cardName));

		return ToDefinitionJson(ToWeaponCardDefinition(card, cardName));
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
				case WeaponCardDefinition weapon:
					Console.WriteLine($"loaded weapon {weapon.Id}");
					_weapons[weapon.Id] = weapon;
					break;
				case RelicCardDefinition relic:
					Console.WriteLine($"loaded relic {relic.Id}");
					_relics[relic.Id] = relic;
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
				CardType.Weapon => JsonConvert.DeserializeObject<WeaponCardDefinition>(json, JsonSettings),
				CardType.Relic => JsonConvert.DeserializeObject<RelicCardDefinition>(json, JsonSettings),
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

		return BuildMinionCard(def, owner);
	}

	public MinionCard BuildMinionCard(MinionCardDefinition def, Player owner)
	{
		var card = new MinionCard(def.Name, def.Cost, def.Attack, def.Health);
		card.Owner = owner;
		card.CardId = def.Id;
		card.MinionTribes = def.Tribes?.ToList() ?? [];
		card.CastRestriction = def.CastRestriction;
		card.ValidTargetSelector = def.ValidTargetSelector;
		card.TriggeredEffects.AddRange(def.TriggeredEffects.Select(e => e.Clone()));

		return card;
	}

	public WeaponCard GetWeaponCard(string id, Player owner)
	{
		if (!_weapons.TryGetValue(id, out var def))
			throw new KeyNotFoundException($"Unknown weapon id '{id}'");

		return BuildWeaponCard(def, owner);
	}

	public WeaponCard BuildWeaponCard(WeaponCardDefinition def, Player owner)
	{
		var card = new WeaponCard(def.Name, def.Cost, def.Attack, def.Durability);
		card.Owner = owner;
		card.CardId = def.Id;
		card.CastRestriction = def.CastRestriction;
		card.ValidTargetSelector = def.ValidTargetSelector;
		card.TriggeredEffects.AddRange(def.TriggeredEffects.Select(e => e.Clone()));

		return card;
	}

	public RelicCard GetRelicCard(string id, Player owner)
	{
		if (!_relics.TryGetValue(id, out var def))
			throw new KeyNotFoundException($"Unknown relic id '{id}'");

		return BuildRelicCard(def, owner);
	}

	public RelicCard BuildRelicCard(RelicCardDefinition def, Player owner)
	{
		var card = new RelicCard(def.Name, def.Cost, def.Health);
		card.Owner = owner;
		card.CardId = def.Id;
		card.Charges = def.Charges;
		card.CastRestriction = def.CastRestriction;
		card.ValidTargetSelector = def.ValidTargetSelector;
		card.RelicTriggeredEffects.AddRange(def.TriggeredEffects.Select(e => e.Clone()));

		return card;
	}

	public SpellCard GetSpellCard(string id, Player owner)
	{
		if (!_spells.TryGetValue(id, out var def))
			throw new KeyNotFoundException($"Unknown spell id '{id}'");

		return BuildSpellCard(def, owner);
	}

	public SpellCard BuildSpellCard(SpellCardDefinition def, Player owner)
	{
		var card = new SpellCard(def.Name, def.Cost);
		card.Owner = owner;
		card.CardId = def.Id;
		card.CastRestriction = def.CastRestriction;
		card.ValidTargetSelector = def.ValidTargetSelector;
		card.SpellCastEffects.AddRange(def.SpellCastEffects);

		return card;
	}
}

public abstract class CardDefinition
{
	public CardType Type { get; set; }
	public string Id { get; set; }
	public string Name { get; set; }
	public int Cost { get; set; }
	public ICastRestriction CastRestriction { get; set; }
	public IValidTargetSelector ValidTargetSelector { get; set; }
}

public class SpellCardDefinition : CardDefinition
{
	public List<SpellCastEffect> SpellCastEffects { get; set; } = new();
}

public class MinionCardDefinition : CardDefinition
{
	public int Attack { get; set; }
	public int Health { get; set; }
	public List<string> Tribes { get; set; }
	public List<TriggeredEffect> TriggeredEffects { get; set; } = new();
}

public class WeaponCardDefinition : CardDefinition
{
	public int Attack { get; set; }
	public int Durability { get; set; }
	public List<TriggeredEffect> TriggeredEffects { get; set; } = new();
}

public class RelicCardDefinition : CardDefinition
{
	public int Health { get; set; }
	public int? Charges { get; set; }
	public List<TriggeredEffect> TriggeredEffects { get; set; } = new();
}
