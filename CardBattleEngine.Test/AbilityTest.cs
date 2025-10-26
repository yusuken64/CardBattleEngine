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
		card.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Battlecry,
			EffectTiming = EffectTiming.Post,
			TargetType = TargetType.AnyEnemy,
			GameActions = new List<IGameAction>() 
			{
				new DamageAction() { Damage = 1 }
			}
		});

		current.Hand.Add(card);

		var play = new PlayCardAction()
		{
			Card = card
		};
		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = current,
			Source = current,
			Target = opponent
		}, play);

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
		minionCard.TriggeredEffects.Add(new TriggeredEffect
		{
			EffectTrigger = EffectTrigger.Deathrattle,
			EffectTiming = EffectTiming.Post,
			TargetType = TargetType.AnyEnemy,
			GameActions = new List<IGameAction>
			{
				new DamageAction() { Damage = 1 }
			}
		});
		current.Mana = 1;
		current.Hand.Add(minionCard);

		IGameAction summonMinion = new PlayCardAction()
		{
			Card = minionCard
		};
		//engine.Resolve(state, current, opponent, summonMinion);

		// Act: Kill the minion to trigger Deathrattle
		var minionEntity = state.CurrentPlayer.Board[0];
		var damage = new DamageAction() { Damage = minionEntity.Health };
		//engine.Resolve(state, current, opponent, damage);

		// Assert
		Assert.AreEqual(initialHealth - 1, opponent.Health, "Deathrattle should deal 1 damage");
		Assert.IsFalse(current.Board.Contains(minionEntity), "Minion should be removed from board");
	}

	[TestMethod]
	public void BattleBuffMinion()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentPlayer;
		int initialHealth = opponent.Health;

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var testCard = cardDatabase.GetMinion("TestMinion", current); // 1/1

		current.Board.Add(new Minion(testCard, current));

		var abusiveCard = new MinionCard("AbusiveSergeant", 1, 1, 1);
		abusiveCard.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTiming = EffectTiming.Post,
			EffectTrigger = EffectTrigger.Battlecry,
			TargetType = TargetType.FriendlyMinion,
			GameActions = new List<IGameAction>
			{
				new ModifyStatsAction()
				{
					Target = current.Board[0],
					AttackChange = 2,
				}
			}
		});
		abusiveCard.Owner = current;
		current.Hand.Add(abusiveCard);

		IGameAction playCardAction = new PlayCardAction() { Card = abusiveCard };
		ActionContext actionContext = new ActionContext()
		{
			SourcePlayer = current,
			SourceCard = abusiveCard,
			TargetSelector = (gs, player, targetType) =>
			{
				var validTargets = gs.GetValidTargets(player, targetType);
				return validTargets[0];
			}
		};
		engine.Resolve(state, actionContext, playCardAction);

		Assert.AreEqual(3, current.Board[0].Attack);
	}
}
