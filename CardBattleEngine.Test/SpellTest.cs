namespace CardBattleEngine.Test;

[TestClass]
public class SpellTest
{
	[TestMethod]
	public void DrawCardSpellTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentPlayer;

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var spellCard = cardDatabase.GetSpellCard("DrawCardSpell", current);
		current.Hand.Add(spellCard);

		int initialHandCount = current.Hand.Count;
		int initialDeckCount = current.Deck.Count;

		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = current,
			SourceCard = spellCard
		}, new PlayCardAction { Card = spellCard });
		
		Assert.AreEqual(initialHandCount + 2, current.Hand.Count, "Player should have drawn three cards.");
		Assert.AreEqual(initialDeckCount - 3, current.Deck.Count, "Deck should have one less card.");
	}

	[TestMethod]
	public void FrostBoltSpellTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentPlayer;

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var spellCard = cardDatabase.GetSpellCard("FrostBolt", current);
		current.Hand.Add(spellCard);

		var card = cardDatabase.GetMinionCard("TestMinion", opponent);
		var enemyMinion = new Minion(card, opponent);
		enemyMinion.Health = 5;
		opponent.Board.Add(enemyMinion);

		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = current,
			SourceCard = spellCard,
			Target = enemyMinion,
		}, new PlayCardAction { Card = spellCard });

		// Assert
		Assert.AreEqual(2, opponent.Board[0].Health, "Frostbolt should deal 3 damage.");
		Assert.IsTrue(opponent.Board[0].IsFrozen, "Frostbolt should freeze the target.");
	}
}
