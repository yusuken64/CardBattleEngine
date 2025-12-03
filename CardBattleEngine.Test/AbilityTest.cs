namespace CardBattleEngine.Test;

[TestClass]
public class AbilityTest
{
	[TestMethod]
	public void Battlecry_Deals1DamageOnPlay()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentOf(current);
		int initialHealth = opponent.Health;

		var card = new MinionCard("BattlecryMinion", cost: 1, attack: 1, health: 1);
		card.Owner = current;
		card.MinionTriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Battlecry,
			EffectTiming = EffectTiming.Post,
			TargetType = TargetingType.AnyEnemy,
			GameActions = new List<IGameAction>() 
			{
				new DamageAction() { Damage = (Value)1 }
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
			Target = opponent,
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
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);
		int initialHealth = opponent.Health;

		// Create minion with Deathrattle effect
		var minionCard = new MinionCard("DeathrattleMinion", cost: 1, attack: 1, health: 1);
		minionCard.Owner = current;
		minionCard.MinionTriggeredEffects.Add(new TriggeredEffect
		{
			EffectTrigger = EffectTrigger.Deathrattle,
			EffectTiming = EffectTiming.Post,
			TargetType = TargetingType.AnyEnemy,
			AffectedEntitySelector = new TargetOperationSelector
			{
				Operations = [
					new SelectBoardEntitiesOperation() {
						Group = TargetGroup.Hero,
						Side = TeamRelationship.Enemy,
					}]
			},
			GameActions = new List<IGameAction>
			{
				new DamageAction() { Damage = (Value)1 }
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
		var damage = new DamageAction() { Damage = (Value)minionEntity.Health };
		engine.Resolve(state, new ActionContext
		{
			Target = minionEntity,
		}, damage);

		// Assert
		Assert.AreEqual(initialHealth - 1, opponent.Health, "Deathrattle should deal 1 damage");
		Assert.IsFalse(current.Board.Contains(minionEntity), "Minion should be removed from board");
	}

	[TestMethod]
	public void BattleCry_BuffMinion()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentOf(current);
		int initialHealth = opponent.Health;

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var testCard = cardDatabase.GetMinionCard("TestMinion", current); // 1/1

		current.Board.Add(new Minion(testCard, current));

		var abusiveCard = new MinionCard("AbusiveSergeant", 1, 1, 1);
		abusiveCard.MinionTriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTiming = EffectTiming.Post,
			EffectTrigger = EffectTrigger.Battlecry,
			TargetType = TargetingType.FriendlyMinion,
			GameActions = new List<IGameAction>
			{
				new AddStatModifierAction()
				{
					AttackChange = 2,
					ExpirationTrigger = new ExpirationTrigger  ()
					{
						EffectTrigger = EffectTrigger.OnTurnEnd,
						EffectTiming = EffectTiming.Post,
						//GameActions = [new RemoveModifierAction()]
					}
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
			Target = current.Board[0]
		};
		engine.Resolve(state, actionContext, playCardAction);

		Assert.AreEqual(3, current.Board[0].Attack);

		engine.Resolve(state, new ActionContext { SourcePlayer = current }, new EndTurnAction());

		Assert.AreEqual(1, current.Board[0].Attack);
	}

	[TestMethod]
	public void BattleCry_AddMinionToHand()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentOf(current);
		int initialHealth = opponent.Health;

		var elemental = new MinionCard("Elemental", 1, 1, 2);

		var firefly = new MinionCard("FireFly", 1, 1, 2);
		firefly.MinionTriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTiming = EffectTiming.Post,
			EffectTrigger = EffectTrigger.Battlecry,
			TargetType = TargetingType.FriendlyHero,
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
	public void BattleCry_Freeze()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// Create a minion on opponent's board to freeze
		var enemyMinionCard = new MinionCard("EnemyMinion", 1, 2, 2);
		var enemyMinion = new Minion(enemyMinionCard, opponent);
		opponent.Board.Add(enemyMinion);

		// Create the Battlecry minion with Freeze effect
		var freezeMinionCard = new MinionCard("FrostMage", 2, 2, 3);
		freezeMinionCard.MinionTriggeredEffects.Add(new TriggeredEffect
		{
			EffectTiming = EffectTiming.Post,
			EffectTrigger = EffectTrigger.Battlecry,
			TargetType = TargetingType.EnemyMinion,
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
			Target = enemyMinion,
		};

		// Act
		engine.Resolve(state, context, playAction);

		// Assert
		Assert.IsTrue(enemyMinion.IsFrozen, "Enemy minion should be frozen by Battlecry");
		Assert.IsTrue(current.Board.Contains(current.Board.First(m => m.Name == "FrostMage")),
			"FrostMage should be on the board after being played");
		Assert.IsFalse(current.Hand.Contains(freezeMinionCard), "FrostMage should be removed from hand");
	}

	[TestMethod]
	public void StealthMinion()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// Create a stealth minion in hand
		var stealthMinionCard = new MinionCard("StealthMinion", 1, 2, 1);
		stealthMinionCard.IsStealth = true;
		stealthMinionCard.Owner = current;

		current.Mana = 1;
		current.Hand.Add(stealthMinionCard);

		// Play the stealth minion
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = current,
				SourceCard = stealthMinionCard,
			},
			new PlayCardAction() { Card = stealthMinionCard });

		var stealthMinion = current.Board[0];

		// Assert: minion is on board and has stealth
		Assert.IsTrue(stealthMinion.IsStealth, "Minion should have stealth after being played");

		// Attempt to attack the stealth minion with opponent minions
		foreach (var attacker in opponent.Board)
		{
			var attackAction = new AttackAction();
			var context = new ActionContext
			{
				Source = attacker,
				Target = stealthMinion,
				SourcePlayer = opponent
			};

			Assert.IsFalse(attackAction.IsValid(state, context),
				"Opponent should not be able to attack a stealth minion");
		}

		// Stealth ends when the minion attacks
		var attackContext = new ActionContext
		{
			Source = stealthMinion,
			Target = opponent,
			SourcePlayer = current
		};
		var attack = new AttackAction();

		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = current
		}, new StartTurnAction());

		Assert.IsTrue(attack.IsValid(state, attackContext), "Stealth minion should be able to attack normally");

		engine.Resolve(state, attackContext, attack);

		// Assert: attacking minion loses stealth
		Assert.IsFalse(stealthMinion.IsStealth, "Stealth should be removed after minion attacks");
	}
	
	[TestMethod]
	public void ChargeMinion()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// Create a Charge minion in hand
		var chargeMinionCard = new MinionCard("ChargeMinion", 1, 1, 1);
		chargeMinionCard.HasCharge = true;
		chargeMinionCard.Owner = current;

		current.Mana = 1;
		current.Hand.Add(chargeMinionCard);

		// Play the Charge minion
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = current,
				SourceCard = chargeMinionCard,
			},
			new PlayCardAction() { Card = chargeMinionCard });

		var chargeMinion = current.Board[0];

		// Assert: Charge minion can attack immediately
		Assert.IsTrue(chargeMinion.CanAttack(), "Charge minion should be able to attack immediately");

		// Attempt attack
		var attackAction = new AttackAction();
		var attackContext = new ActionContext
		{
			Source = chargeMinion,
			Target = opponent,
			SourcePlayer = current
		};
		Assert.IsTrue(attackAction.IsValid(state, attackContext), "Charge minion attack should be valid");

		// Resolve the attack (optional)
		engine.Resolve(state, attackContext, attackAction);

		// Assert: minion has attacked
		Assert.AreEqual(1, chargeMinion.AttacksPerformedThisTurn, "Charge minion should have attacked");
	}

	[TestMethod]
	public void DivineShieldMinion()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// Create a Charge minion in hand
		var divineShieldCard = new MinionCard("DivineShieldMinion", 1, 1, 2);
		divineShieldCard.HasDivineShield = true;
		divineShieldCard.Owner = current;

		current.Mana = 1;
		current.Hand.Add(divineShieldCard);

		// Play the Charge minion
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = current,
				SourceCard = divineShieldCard,
			},
			new PlayCardAction() { Card = divineShieldCard });

		var divineMinion = current.Board[0];

		// Verify it entered with shield
		Assert.IsTrue(divineMinion.HasDivineShield, "Minion should have Divine Shield initially.");
		Assert.AreEqual(2, divineMinion.Health, "Health should be full before damage.");

		// Deal 1 damage (should pop the shield)
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = opponent,
				Target = divineMinion
			},
			new DamageAction() { Damage = (Value)1 });

		Assert.IsFalse(divineMinion.HasDivineShield, "First hit should remove Divine Shield.");
		Assert.AreEqual(2, divineMinion.Health, "Health should remain unchanged after Divine Shield absorbed damage.");

		// Deal 1 damage again (should now lose health)
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = opponent,
				Target = divineMinion
			},
			new DamageAction() { Damage = (Value)1 });

		Assert.AreEqual(1, divineMinion.Health, "Second hit should reduce health after Divine Shield is gone.");
	}

	[TestMethod]
	public void PoisonMinion()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// Create a Poisonous minion in hand
		var poisonCard = new MinionCard("PoisonMinion", 1, 1, 2);
		poisonCard.HasPoisonous = true;
		poisonCard.HasCharge = true;
		poisonCard.Owner = current;

		current.Mana = 1;
		current.Hand.Add(poisonCard);

		// Play the Poisonous minion
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = current,
				SourceCard = poisonCard,
			},
			new PlayCardAction() { Card = poisonCard });

		var poisonMinion = current.Board[0];

		// Create a target minion for the poison minion to attack
		var targetCard = new MinionCard("TargetDummy", 1, 3, 3);
		targetCard.Owner = opponent;
		var targetMinion = new Minion(targetCard, opponent);
		opponent.Board.Add(targetMinion);

		// Act: poison minion attacks target minion
		var attackAction = new AttackAction();
		var attackContext = new ActionContext
		{
			Source = poisonMinion,
			Target = targetMinion,
			SourcePlayer = current
		};

		engine.Resolve(state, attackContext, attackAction);

		// Assert: both should die (since the poison minion also takes damage)
		Assert.IsFalse(state.GetAllMinions().Contains(targetMinion), "Target should die instantly due to poison.");
		Assert.IsFalse(state.GetAllMinions().Contains(poisonMinion), "Poison minion should die from combat damage.");
	}
}
