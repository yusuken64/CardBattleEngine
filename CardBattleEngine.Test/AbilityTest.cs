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
		ActionContext actionContext = new ActionContext()
		{
			SourcePlayer = current,
			SourceCard = card,
			TargetSelector = (gs, player, targetType) =>
			{
				var validTargets = gs.GetValidTargets(player, targetType);
				return validTargets[0];
			}
		};
		engine.Resolve(state, actionContext, play);

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
		engine.Resolve(state, new ActionContext()
		{
			SourceCard = minionCard,
			SourcePlayer = current
		},
		summonMinion);

		// Act: Kill the minion to trigger Deathrattle
		var minionEntity = state.CurrentPlayer.Board[0];
		var damage = new DamageAction() { Damage = minionEntity.Health };
		engine.Resolve(state, new ActionContext
		{
			Target = minionEntity,
			TargetSelector = (s, p, t) =>
			{
				return s.OpponentOf(p);
			}
		}, damage);

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
				new AddStatModifierAction()
				{
					AttackChange = 2,
					Duration = EffectDuration.UntilEndOfTurn
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

		engine.Resolve(state, new ActionContext { SourcePlayer = current }, new EndTurnAction());

		Assert.AreEqual(1, current.Board[0].Attack);
	}

	[TestMethod]
	public void AddMinionToHand()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentPlayer;
		int initialHealth = opponent.Health;

		var elemental = new MinionCard("Elemental", 1, 1, 2);

		var firefly = new MinionCard("FireFly", 1, 1, 2);
		firefly.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTiming = EffectTiming.Post,
			EffectTrigger = EffectTrigger.Battlecry,
			TargetType = TargetType.FriendlyHero,
			GameActions = new List<IGameAction>
			{
				new GainCardAction()
				{
					Card = elemental
				}
			}
		});
		firefly.Owner = current;
		current.Hand.Add(firefly);

		IGameAction playCardAction = new PlayCardAction() { Card = firefly };
		ActionContext actionContext = new ActionContext()
		{
			SourcePlayer = current,
			SourceCard = firefly,
		};
		engine.Resolve(state, actionContext, playCardAction);

		// The FireFly should have been played and removed from hand
		Assert.IsFalse(current.Hand.Contains(firefly), "FireFly should be removed from hand after being played");

		// FireFly should now be on the board
		Assert.IsTrue(current.Board.Contains(current.Board.FirstOrDefault(m => m.Name == "FireFly")),
			"FireFly should be on the board after being played");

		// The Battlecry should have added the Elemental card to the player's hand
		Assert.IsTrue(current.Hand.Contains(elemental), "Elemental should be added to hand by FireFly Battlecry");

		// The opponent's health should be unchanged
		Assert.AreEqual(initialHealth, opponent.Health, "Opponent health should remain unchanged");
	}

	[TestMethod]
	public void BattlecryMinion_Freeze()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		var opponent = state.OpponentPlayer;

		// Create a minion on opponent's board to freeze
		var enemyMinionCard = new MinionCard("EnemyMinion", 1, 2, 2);
		var enemyMinion = new Minion(enemyMinionCard, opponent);
		opponent.Board.Add(enemyMinion);

		// Create the Battlecry minion with Freeze effect
		var freezeMinionCard = new MinionCard("FrostMage", 2, 2, 3);
		freezeMinionCard.TriggeredEffects.Add(new TriggeredEffect
		{
			EffectTiming = EffectTiming.Post,
			EffectTrigger = EffectTrigger.Battlecry,
			TargetType = TargetType.EnemyMinion,
			GameActions = new List<IGameAction>
		{
			new FreezeAction() // assumed to exist
        }
		});
		freezeMinionCard.Owner = current;
		current.Hand.Add(freezeMinionCard);
		current.Mana = 2;

		var playAction = new PlayCardAction { Card = freezeMinionCard };
		var context = new ActionContext
		{
			SourcePlayer = current,
			SourceCard = freezeMinionCard,
			TargetSelector = (gs, player, targetType) => enemyMinion // pick enemy minion
		};

		// Act
		engine.Resolve(state, context, playAction);

		// Assert
		Assert.IsTrue(enemyMinion.IsFrozen, "Enemy minion should be frozen by Battlecry");
		Assert.IsTrue(current.Board.Contains(current.Board.First(m => m.Name == "FrostMage")),
			"FrostMage should be on the board after being played");
		Assert.IsFalse(current.Hand.Contains(freezeMinionCard), "FrostMage should be removed from hand");
	}
}
