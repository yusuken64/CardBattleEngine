namespace CardBattleEngine.Test;

[TestClass]
[DoNotParallelize]
public class MatchFactoryTest
{
	[TestMethod]
	public void CreateMatch_IncludesWeaponsInPlayerDeck()
	{
		CardDatabase cardDatabase = new(CardDBTest.DBPath);

		var player1 = new Player("Player1");
		var player2 = new Player("Player2");

		var gameState = MatchFactory.CreateMatch(
			cardDatabase,
			player1,
			minionDeck1: new[] { ("TestMinion", 1) },
			spellDeck1: Array.Empty<(string, int)>(),
			weaponDeck1: new[] { ("TestWeapon", 2) },
			player2,
			minionDeck2: new[] { ("TestMinion", 1) },
			spellDeck2: Array.Empty<(string, int)>(),
			weaponDeck2: Array.Empty<(string, int)>(),
			rngSeed: 1);

		var weaponsInDeck = player1.Deck.OfType<WeaponCard>().ToList();

		Assert.AreEqual(2, weaponsInDeck.Count);
		Assert.IsTrue(weaponsInDeck.All(w => w.Name == "TestWeapon"));
	}

	[TestMethod]
	public void CreateMatch_PrefersCustomDefinitionOverDatabaseLookup()
	{
		CardDatabase cardDatabase = new(CardDBTest.DBPath);

		var player1 = new Player("Player1");
		var player2 = new Player("Player2");

		// "InlineOnlyMinion" exists in no json file - it can only resolve via the custom dictionary.
		var customMinions = new Dictionary<string, MinionCardDefinition>
		{
			["InlineOnlyMinion"] = new MinionCardDefinition
			{
				Type = CardType.Minion,
				Id = "InlineOnlyMinion",
				Name = "Inline Only Minion",
				Cost = 2,
				Attack = 9,
				Health = 9,
			}
		};

		var gameState = MatchFactory.CreateMatch(
			cardDatabase,
			player1,
			minionDeck1: new[] { ("InlineOnlyMinion", 1), ("TestMinion", 1) },
			spellDeck1: Array.Empty<(string, int)>(),
			weaponDeck1: Array.Empty<(string, int)>(),
			player2,
			minionDeck2: new[] { ("TestMinion", 1) },
			spellDeck2: Array.Empty<(string, int)>(),
			weaponDeck2: Array.Empty<(string, int)>(),
			rngSeed: 1,
			customMinions: customMinions);

		var customCard = player1.Deck.OfType<MinionCard>().FirstOrDefault(m => m.Name == "Inline Only Minion");
		Assert.IsNotNull(customCard, "Expected the custom-defined minion to be present in player1's deck.");
		Assert.AreEqual(9, customCard.Attack);
		Assert.AreEqual(9, customCard.Health);

		var builtInCard = player1.Deck.OfType<MinionCard>().FirstOrDefault(m => m.Name == "TestMinion");
		Assert.IsNotNull(builtInCard, "Expected the built-in TestMinion (not in the custom dictionary) to still resolve via the database.");
	}
}
