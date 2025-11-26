namespace CardBattleEngine.Test;

[TestClass]
public class HeroPowerTests
{
	[TestMethod]
	// hero doesn't have a power
	public void NoHeroPowerTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 2;
		var opponent = state.OpponentOf(current);
		int initialHealth = opponent.Health;

		var action = new HeroPowerAction();
		var context = new ActionContext()
		{
			SourcePlayer = current,
			SourceHeroPower = current.HeroPower, // null
			Target = opponent
		};

		// Should NOT be valid
		Assert.IsFalse(action.IsValid(state, context));

		// Resolve should do nothing (no crash, no effect)
		engine.Resolve(state, context, action);

		Assert.AreEqual(initialHealth, opponent.Health, "Opponent should not take damage");
		Assert.AreEqual(2, current.Mana, "Player should not spend mana");
	}

	[TestMethod]
	public void TargetedHeroPowerTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 2;
		current.HeroPower = new HeroPower()
		{
			TargetingType = TargetingType.Any,
			ManaCost = 2,
			UsedThisTurn = false,
			GameActions =
			[
				new DamageAction()
				{
					Damage = (Value)1
				}
			]
		};

		var opponent = state.OpponentOf(current);
		int initialHealth = opponent.Health;

		var action = new HeroPowerAction();
		var context = new ActionContext()
		{
			SourcePlayer = current,
			SourceHeroPower = current.HeroPower,
			Target = opponent
		};

		Assert.IsTrue(action.IsValid(state, context));

		engine.Resolve(state, context, action);

		// Assert damage applied
		Assert.AreEqual(initialHealth - 1, opponent.Health, "Opponent should take 1 damage");

		// Assert mana spent
		Assert.AreEqual(0, current.Mana, "Hero power should cost 2 mana");

		// Assert hero power was marked used
		Assert.IsTrue(current.HeroPower.UsedThisTurn, "Hero power should now be used");
	}

	[TestMethod]
	public void HeroPowerResetsAtStartOfTurn()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;

		// Give the player a hero power
		current.HeroPower = new HeroPower()
		{
			TargetingType = TargetingType.None,
			ManaCost = 2,
			UsedThisTurn = false,
			GameActions = []
		};

		// Simulate using it
		current.HeroPower.UsedThisTurn = true;
		Assert.IsTrue(current.HeroPower.UsedThisTurn, "Pre-check: hero power should be marked used");

		// Execute start turn logic
		var action = new StartTurnAction();
		var context = new ActionContext()
		{
			SourcePlayer = current
		};

		engine.Resolve(state, context, action);

		// Hero power should reset
		Assert.IsFalse(current.HeroPower.UsedThisTurn, "Hero power should reset at the beginning of the turn");
	}
}
