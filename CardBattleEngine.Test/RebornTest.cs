namespace CardBattleEngine.Test;

[TestClass]
public class RebornTest
{
	[TestMethod]
	public void MinionRebornTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		// Create the reborn card
		var rebornCard = new MinionCard("rebornMinion", 1, 3, 4)
		{
			HasReborn = true
		};

		var current = state.CurrentPlayer;
		current.Mana = 1;
		current.Hand.Add(rebornCard);
		rebornCard.Owner = current;

		// --- PLAY THE CARD ---
		ActionContext playContext = new ActionContext()
		{
			SourcePlayer = current,
			SourceCard = rebornCard
		};

		IGameAction playAction = new PlayCardAction()
		{
			Card = rebornCard
		};

		engine.Resolve(state, playContext, playAction);

		// The minion should now be on the board
		Assert.AreEqual(1, current.Board.Count);

		var minion = current.Board[0];

		Assert.IsTrue(minion.HasReborn, "Minion should start with Reborn.");

		// --- KILL THE MINION ---
		ActionContext killContext = new ActionContext()
		{
			SourcePlayer = current,
			Source = minion,
			Target = minion
		};

		var killAction = new DeathAction();

		engine.Resolve(state, killContext, killAction);

		// It should have died and been reborn
		Assert.AreEqual(1, current.Board.Count, "Minion should be resummoned on the same slot.");

		var rebornMinion = current.Board[0];

		// Validate reborn state
		Assert.IsFalse(rebornMinion.HasReborn, "Reborn should be consumed and removed.");
		Assert.AreEqual(1, rebornMinion.Health, "Reborn minion should be set to 1 Health.");
		Assert.AreEqual(3, rebornMinion.Attack, "Reborn keeps base attack.");
		Assert.AreEqual("rebornMinion", rebornMinion.OriginalCard.Name);

		// Validate the graveyard contains only the original corpse
		Assert.AreEqual(1, current.Graveyard.Count, "Original minion should be placed in graveyard.");
	}
}
