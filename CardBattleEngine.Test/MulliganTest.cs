namespace CardBattleEngine.Test;

[TestClass]
public class MulliganTest
{
	[TestMethod]
	public void TestMulligan()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		engine.StartGame(state);
		var originalCard = current.Hand[0];

		//assert is true
		Assert.IsTrue(state.PendingChoice.GetActions(state).First().Item1 is SubmitMulliganAction);

		var submitMullilgan = state.PendingChoice.GetActions(state).First();
		((SubmitMulliganAction)submitMullilgan.Item1).CardsToReplace = [current.Hand[0]];

		engine.Resolve(state, submitMullilgan.Item2, submitMullilgan.Item1);

		// Player 2 must also mulligan before the game proceeds to turn 1.
		Assert.IsNotNull(state.PendingChoice, "Player 2 must be prompted to mulligan next.");
		var submitOpponentMulligan = state.PendingChoice.GetActions(state).First();
		engine.Resolve(state, submitOpponentMulligan.Item2, submitOpponentMulligan.Item1);

		Assert.IsNull(state.PendingChoice, "PendingChoice should be null after both players have mulliganed.");

		Assert.IsFalse(
			current.Hand.Contains(originalCard),
			"Original card should not remain in hand after mulligan.");

		Assert.AreEqual(
			4,
			current.Hand.Count,
			"Hand size should not change after mulligan.");

		var newCard = current.Hand[0];
		Assert.AreNotEqual(
			originalCard,
			newCard,
			"The new card after mulligan should not be the same card.");

		Assert.IsTrue(
			current.Deck.Contains(originalCard),
			"Original card should have been returned to the deck.");
	}
}
