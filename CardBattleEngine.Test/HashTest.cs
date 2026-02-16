namespace CardBattleEngine.Test;

[TestClass]
public class HashTest
{
	[TestMethod]
	public void HashGameStateTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var me = state.Players[0];

		while (!state.IsGameOver())
		{
			var actions = state.GetValidActions(state.CurrentPlayer);
			(IGameAction, ActionContext) topAction = actions[0];
			engine.Resolve(state, topAction.Item2, topAction.Item1);

			var hash = GameStateHasher.HashState(state, me);
			Console.WriteLine($"{topAction.Item1.ToString()} hash {hash}");

			var cloneState = state.Clone();
			var clonePlayer = cloneState.GetEntityById(me.Id) as Player;
			var cloneHash = GameStateHasher.HashState(cloneState, clonePlayer);

			var lightCloneState = state.LightClone();
			var lightClonePlayer = lightCloneState.GetEntityById(me.Id) as Player;
			var lightCloneHash = GameStateHasher.HashState(lightCloneState, lightClonePlayer);

			Assert.AreEqual(hash, cloneHash);
			Assert.AreEqual(hash, lightCloneHash);
		}
	}

	[TestMethod]
	public void HashChangesWhenHealthChanges()
	{
		var state = GameFactory.CreateTestGame();
		var me = state.Players[0];

		var h1 = GameStateHasher.HashState(state, me);

		me.Health--;

		var h2 = GameStateHasher.HashState(state, me);

		Assert.AreNotEqual(h1, h2);
	}

	[TestMethod]
	public void HashIgnoresBoardOrder()
	{
		var state = GameFactory.CreateTestGame();
		var me = state.Players[0];

		var h1 = GameStateHasher.HashState(state, me);

		me.Board.Reverse();

		var h2 = GameStateHasher.HashState(state, me);

		Assert.AreEqual(h1, h2);
	}

	[TestMethod]
	public void HashDependsOnPerspective()
	{
		var state = GameFactory.CreateTestGame();

		var h1 = GameStateHasher.HashState(state, state.Players[0]);
		var h2 = GameStateHasher.HashState(state, state.Players[1]);

		Assert.AreNotEqual(h1, h2);
	}

	[TestMethod]
	public void MinionHashSensitivityTest()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var me = state.Players[0];

		MinionCard testCard = new MinionCard("Test", 1, 1, 1);
		me.Board.Add(new Minion(testCard, me));
		var minion = me.Board.FirstOrDefault();
		Assert.IsNotNull(minion, "Test requires at least one minion on board.");

		// Compute original hash
		var originalHash = GameStateHasher.HashState(state, me);

		// Helper to test if changing a value changes the hash
		void AssertHashChanges(Action<Minion> mutate)
		{
			var cloneState = state.Clone();
			var clonePlayer = cloneState.GetEntityById(me.Id) as Player;
			var cloneMinion = clonePlayer.Board.First(m => m.Id == minion.Id);

			mutate.Invoke(cloneMinion);

			var newHash = GameStateHasher.HashState(cloneState, clonePlayer);
			Assert.AreNotEqual(originalHash, newHash, "Hash did not change after mutation.");
		}

		// 1) Attack
		AssertHashChanges((minion) => minion.Attack++);

		// 2) Health
		AssertHashChanges((minion) => minion.Health++);

		// 3) MaxHealth
		AssertHashChanges((minion) => minion.MaxHealth++);

		// 4) Frozen
		AssertHashChanges((minion) => minion.IsFrozen = !minion.IsFrozen);

		// 5) CanAttack / readiness
		AssertHashChanges((minion) => minion.HasCharge = !minion.HasCharge);

		// 6) Taunt
		AssertHashChanges((minion) => minion.Taunt = !minion.Taunt);

		// 7) Divine Shield
		AssertHashChanges((minion) => minion.HasDivineShield = !minion.HasDivineShield);

		// 8) Poisonous
		AssertHashChanges((minion) => minion.HasPoisonous = !minion.HasPoisonous);

		// 9) Windfury
		AssertHashChanges((minion) => minion.HasWindfury = !minion.HasWindfury);

		// 10) Stealth
		AssertHashChanges((minion) => minion.IsStealth = !minion.IsStealth);

		// 11) Lifesteal
		AssertHashChanges((minion) => minion.HasLifeSteal = !minion.HasLifeSteal);

		// 12) Reborn
		AssertHashChanges((minion) => minion.HasReborn = !minion.HasReborn);

		// 13) Card ID
		//AssertHashChanges((minion) => minion.CardNumericId++);

		// 14) Effects
		//AssertHashChanges(() =>
		//{
		//	minion.Effects ??= new List<ActiveEffect>();
		//	minion.Effects.Add(new ActiveEffect { Id = 999, Stacks = 1, RemainingTurns = 1 });
		//});
	}
}
