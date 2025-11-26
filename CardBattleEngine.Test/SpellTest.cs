namespace CardBattleEngine.Test;

[TestClass]
public class SpellTest
{
	[TestMethod]
	public void DrawCardSpellTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentOf(current);

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
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentOf(current);

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var spellCard = cardDatabase.GetSpellCard("FrostBolt", current);
		current.Hand.Add(spellCard);

		var card = cardDatabase.GetMinionCard("TestMinion", opponent);
		card.Health = 5;
		var enemyMinion = new Minion(card, opponent);
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

	[TestMethod]
	public void FlamestrikeSpellTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 7; // Assume Flamestrike costs 7
		var opponent = state.OpponentOf(current);

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var spellCard = cardDatabase.GetSpellCard("FlameStrike", current);
		current.Hand.Add(spellCard);

		// Create multiple enemy minions with varying health
		for (int i = 0; i < 5; i++)
		{
			var card = cardDatabase.GetMinionCard("TestMinion", opponent);
			var enemyMinion = new Minion(card, opponent)
			{
				Health = 6
			};
			opponent.Board.Add(enemyMinion);
		}

		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = current,
			SourceCard = spellCard,
			Target = null,
		}, new PlayCardAction { Card = spellCard });

		// Assert - All enemy minions should have taken 5 damage
		foreach (var minion in opponent.Board)
		{
			Assert.AreEqual(1, minion.Health, "Flamestrike should deal 5 damage to all enemy minions.");
		}
	}

}
