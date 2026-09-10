namespace CardBattleEngine.Test;

/// <summary>
/// End-to-end test for the card-driven multi-target selection flow.
/// Demonstrates that a real card requiring 2 targets (SwapHealthAction) works through
/// the generic pause/resume mechanism without bespoke "begin selection" plumbing.
/// </summary>
[TestClass]
public class MultiTargetTest
{
	/// <summary>
	/// Test: SwapHealthAction with two minions — full end-to-end flow.
	///
	/// Verifies:
	/// - A spell card with RequiredTargetCount = 2 and SwapHealthAction enters target selection
	/// - PlayCardAction is offered as an ordinary action (no pre-filled Targets)
	/// - GameEngine.Resolve automatically creates a PendingChoice on the first resolution
	/// - GetValidActions returns SupplyTargetAction options for each pick
	/// - After 2 picks, the flow finalizes and SwapHealthAction executes
	/// - Health values are correctly swapped and clamped to MaxHealth
	/// </summary>
	[TestMethod]
	public void SwapHealthTwoPickedMinions()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 10;

		var bodySwap = new SpellCard("BodySwap", 2)
		{
			RequiredTargetCount = 2,
			ValidTargetSelector = new EntityTypeSelector
			{
				EntityTypes = EntityType.Minion,
				TeamRelationship = TeamRelationship.Any
			},
		};
		bodySwap.SpellCastEffects.Add(new SpellCastEffect
		{
			GameActions = new List<IGameAction> { new SwapHealthAction() }
		}); // AffectedEntitySelector left null on purpose — exercises the CastSpellAction passthrough branch

		current.Hand.Add(bodySwap);
		bodySwap.Owner = current;

		var big = new Minion(new MinionCard("Big", 1, 1, 10), current);
		var small = new Minion(new MinionCard("Small", 1, 1, 2), current);
		current.Board.Add(big);
		current.Board.Add(small);

		// Act: Select the card itself — a single ordinary PlayCardAction, no pre-filled Targets.
		// NTR-007 attaches a PendingTargetRequirement; NTR-005's central interception in
		// GameEngine.Resolve pauses automatically once this action is dequeued.
		var playCard = state.GetValidActions(current).First(a => a.Item1 is PlayCardAction pca && pca.Card == bodySwap);
		engine.Resolve(state, playCard.Item2, playCard.Item1);

		// state.GetValidActions now surfaces SupplyTargetAction options via the pending TargetSelectionChoice.
		var pick1 = state.GetValidActions(current).First(a => ((SupplyTargetAction)a.Item1).Candidate == big);
		engine.Resolve(state, pick1.Item2, pick1.Item1);

		var pick2 = state.GetValidActions(current).First(a => ((SupplyTargetAction)a.Item1).Candidate == small);
		engine.Resolve(state, pick2.Item2, pick2.Item1);

		// Assert
		// After swap: big gets small's health (2), small gets big's health (10 clamped to small's MaxHealth 2)
		Assert.AreEqual(2, small.Health);
		Assert.AreEqual(2, big.Health);
	}
}
