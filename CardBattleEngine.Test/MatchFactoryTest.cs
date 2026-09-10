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
}
