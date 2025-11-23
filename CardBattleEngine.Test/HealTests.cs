namespace CardBattleEngine.Test;

[TestClass]
public class HealTests
{
	[TestMethod]
	public void LifeStealTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// Give current player's hero some missing HP
		int originalHeroHealth = current.Health;
		current.Health -= 3; // hero is damaged

		// Create minion with Lifesteal
		var lifestealCard = new MinionCard("Lifesteal Minion", cost: 1, attack: 2, health: 3);
		lifestealCard.HasLifeSteal = true;

		var lifestealMinion = new Minion(lifestealCard, current);

		// Create dummy enemy minion
		var dummyCard = new MinionCard("Dummy", cost: 1, attack: 1, health: 3);
		var dummy = new Minion(dummyCard, opponent);

		// Put both minions on the board
		current.Board.Add(lifestealMinion);
		opponent.Board.Add(dummy);

		engine.Resolve(state, new ActionContext() { SourcePlayer = current }, new StartTurnAction());

		// Act: lifesteal minion attacks dummy
		var action = new AttackAction();
		var context = new ActionContext()
		{
			SourcePlayer = lifestealMinion.Owner,
			Source = lifestealMinion,
			Target = dummy,
		};

		engine.Resolve(state, context, action);

		// Lifesteal minion deals 2 damage → hero heals for 2
		Assert.AreEqual(originalHeroHealth - 3 + 2, current.Health, "Hero should be healed by lifesteal amount.");

		// Dummy should have taken 2 damage
		Assert.AreEqual(1, dummy.Health, "Dummy minion should take 2 damage.");
	}
}
