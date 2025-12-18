namespace CardBattleEngine.Test;

[TestClass]
public class CombatTest
{
	[TestMethod]
	public void RushMinion()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// Create a Rush minion
		var rushCard = new MinionCard("RushMinion", 1, 1, 1);
		rushCard.HasRush = true;
		rushCard.Owner = current;

		current.Mana = 1;
		current.Hand.Add(rushCard);

		// Play the Rush minion
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = current,
				SourceCard = rushCard,
			},
			new PlayCardAction() { Card = rushCard });

		var rushMinion = current.Board[0];

		// ----- Rush CAN attack minions -----
		var dummyMinionCard = new MinionCard("Dummy", 1, 1, 1);
		opponent.Board.Add(new Minion(dummyMinionCard, opponent));
		var dummyMinion = opponent.Board[0];

		Assert.IsTrue(rushMinion.CanAttack(),
			"Rush minion should be able to attack minions immediately");
		Assert.IsTrue(rushMinion.AttackBehavior.CanAttack(rushMinion, dummyMinion, state, out string _));

		// ----- Rush CANNOT attack heroes -----;
		Assert.IsFalse(rushMinion.AttackBehavior.CanAttack(rushMinion, opponent, state, out string _));
	}

	[TestMethod]
	public void WindfuryMinion()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// Create a Windfury minion with Charge so it can attack immediately
		var wfCard = new MinionCard("WindfuryMinion", 1, 1, 1);
		wfCard.HasWindfury = true;
		wfCard.HasCharge = true;
		wfCard.Owner = current;

		current.Mana = 1;
		current.Hand.Add(wfCard);

		// Play the minion
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = current,
				SourceCard = wfCard,
			},
			new PlayCardAction() { Card = wfCard });

		var wfMinion = current.Board[0];

		// Should start with 2 attacks allowed
		Assert.AreEqual(0, wfMinion.AttacksPerformedThisTurn);
		Assert.AreEqual(2, wfMinion.AttackBehavior.MaxAttacks(wfMinion));

		// First attack
		var ctx1 = new ActionContext()
		{
			Source = wfMinion,
			Target = opponent,
			SourcePlayer = current
		};
		Assert.IsTrue(new AttackAction().IsValid(state, ctx1, out string _));
		engine.Resolve(state, ctx1, new AttackAction());

		Assert.AreEqual(1, wfMinion.AttacksPerformedThisTurn,
			"Windfury: First attack should register");

		// Second attack
		var ctx2 = new ActionContext()
		{
			Source = wfMinion,
			Target = opponent,
			SourcePlayer = current
		};
		Assert.IsTrue(new AttackAction().IsValid(state, ctx2, out string _),
			"Windfury: Second attack should be valid");
		engine.Resolve(state, ctx2, new AttackAction());

		Assert.AreEqual(2, wfMinion.AttacksPerformedThisTurn,
			"Windfury: Second attack should register");

		// Third attack should FAIL
		var ctx3 = new ActionContext()
		{
			Source = wfMinion,
			Target = opponent,
			SourcePlayer = current
		};
		Assert.IsFalse(new AttackAction().IsValid(state, ctx3, out string _),
			"Windfury: Third attack should NOT be valid");
	}
}