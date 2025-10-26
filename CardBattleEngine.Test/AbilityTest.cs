namespace CardBattleEngine.Test;

[TestClass]
public class AbilityTest
{
	[TestMethod]
	public void BattlecryMinion_Deals1DamageOnPlay()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentPlayer;
		int initialHealth = opponent.Health;

		var card = new MinionCard("BattlecryMinion", cost: 1, attack: 1, health: 1);
		card.Owner = current;
		card.TriggeredEffect.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Battlecry,
			EffectTiming = EffectTiming.Post,
			TargetType = TargetType.AnyEnemy,
			GameActions = new List<IGameAction>() 
			{
				new DamageAction() { Target = opponent, Damage = 1 }
			}
		});

		current.Hand.Add(card);

		var play = new PlayCardAction(card);
		engine.Resolve(state, current, opponent, play);

		Assert.AreEqual(initialHealth - 1, opponent.Health, "Battlecry should deal 1 damage");
		Assert.AreEqual(1, current.Board.Count, "Minion should be summoned");
		Assert.AreEqual("BattlecryMinion", current.Board[0].Name);
	}

	[TestMethod]
	public void DeathrattleMinion_Deals1DamageOnDeath()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		var opponent = state.OpponentPlayer;
		int initialHealth = opponent.Health;

		// Create minion with Deathrattle effect
		var minionCard = new MinionCard("DeathrattleMinion", cost: 1, attack: 1, health: 1);
		minionCard.Owner = current;
		minionCard.TriggeredEffect.Add(new TriggeredEffect
		{
			EffectTrigger = EffectTrigger.Deathrattle,
			EffectTiming = EffectTiming.Post,
			TargetType = TargetType.AnyEnemy,
			GameActions = new List<IGameAction>
			{
				new DamageAction() { Target = opponent, Damage = 1 }
			}
		});
		current.Mana = 1;
		current.Hand.Add(minionCard);

		IGameAction summonMinion = new PlayCardAction(minionCard);
		engine.Resolve(state, current, opponent, summonMinion);

		// Act: Kill the minion to trigger Deathrattle
		var minionEntity = state.CurrentPlayer.Board[0];
		var damage = new DamageAction() { Target = minionEntity, Damage = minionEntity.Health };
		engine.Resolve(state, current, opponent, damage);

		// Assert
		Assert.AreEqual(initialHealth - 1, opponent.Health, "Deathrattle should deal 1 damage");
		Assert.IsFalse(current.Board.Contains(minionEntity), "Minion should be removed from board");
	}
}
