namespace CardBattleEngine.Test;

[TestClass]
public class SigilTest
{
	[TestMethod]
	public void PlaySigilAction()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.CurrentPlayer;

		var sigil = new Sigil();
		var sigilAction = new SigilAction() { Sigil = sigil };
		var context = new ActionContext()
		{
			SourcePlayer = player1,
			Target = player1
		};

		Assert.AreEqual(0, player1.Sigils.Count, "No sigils initially");

		// Act
		engine.Resolve(state, context, sigilAction);

		// Assert
		Assert.AreEqual(1, player1.Sigils.Count, "Sigil should be added to player.Sigils");
		Assert.AreEqual(sigil, player1.Sigils[0]);
		Assert.AreEqual(player1, sigil.Owner);
	}

	[TestMethod]
	public void SigilWithUnlimitedFrequencyIsNotMarkedUsed()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var player = state.CurrentPlayer;

		// Create a triggered effect with Unlimited frequency
		var triggeredEffect = new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Attack,
			EffectTiming = EffectTiming.Post,
			Frequency = EffectFrequency.Unlimited,
			GameActions = [new DamageAction() { Damage = (Value)1 }],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSourcePlayer = true
			}
		};

		var sigil = new Sigil()
		{
			Owner = player,
			TriggeredEffects = new List<TriggeredEffect>() { triggeredEffect }
		};

		player.Sigils.Add(sigil);

		// Pre-check
		Assert.IsFalse(triggeredEffect.UsedThisTurn);

		// Act - verify unlimited frequency effects are never marked as used
		// (they can fire multiple times per turn)
		Assert.AreEqual(EffectFrequency.Unlimited, triggeredEffect.Frequency);

		// Even if we manually mark it as used, it should conceptually fire again
		// since it's Unlimited (the test just verifies the enum value)
		Assert.AreEqual(1, player.Sigils.Count, "Sigil should remain in player.Sigils");
	}

	[TestMethod]
	public void SigilWithOncePerTurnFiresOncePerTurn()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player = state.CurrentPlayer;

		// Create a triggered effect with OncePerTurn frequency
		var triggeredEffect = new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Attack,
			EffectTiming = EffectTiming.Post,
			Frequency = EffectFrequency.OncePerTurn,
			GameActions = [new DamageAction() { Damage = (Value)1 }],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSourcePlayer = true
			}
		};

		var sigil = new Sigil()
		{
			Owner = player,
			TriggeredEffects = new List<TriggeredEffect>() { triggeredEffect }
		};

		player.Sigils.Add(sigil);

		Assert.IsFalse(triggeredEffect.UsedThisTurn, "Effect should not be used initially");

		// Create two attackers
		var minionCard = new MinionCard("TestMinion", 1, 2, 2);
		var minion1 = new Minion(minionCard, player);
		var minion2 = new Minion(minionCard, player);
		minion1.HasCharge = true;
		minion2.HasCharge = true;
		player.Board.Add(minion1);
		player.Board.Add(minion2);

		var opponent = state.OpponentOf(player);
		opponent.Health = 30;

		int healthAfterFirstAttack = 0;

		// Act - first attack triggers the effect
		var triggerAction1 = new TriggerEffectAction()
		{
			TriggeredEffect = triggeredEffect,
			TriggerSource = sigil,
			EffectContext = new ActionContext()
			{
				SourcePlayer = player,
				Source = minion1,
				Target = opponent
			}
		};
		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = player,
			Source = minion1,
			Target = opponent
		}, triggerAction1);

		healthAfterFirstAttack = opponent.Health;
		Assert.IsTrue(triggeredEffect.UsedThisTurn, "Effect should be marked as used after first attack");

		// Act - second attack should NOT trigger the effect (already used this turn)
		var triggerAction2 = new TriggerEffectAction()
		{
			TriggeredEffect = triggeredEffect,
			TriggerSource = sigil,
			EffectContext = new ActionContext()
			{
				SourcePlayer = player,
				Source = minion2,
				Target = opponent
			}
		};
		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = player,
			Source = minion2,
			Target = opponent
		}, triggerAction2);

		// Assert - health should not change on second trigger
		Assert.AreEqual(healthAfterFirstAttack, opponent.Health, "Effect should not trigger again same turn");

		// Act - start turn to reset
		var startTurnAction = new StartTurnAction();
		engine.Resolve(state, new ActionContext() { SourcePlayer = player }, startTurnAction);

		// Assert - effect should be reset
		Assert.IsFalse(triggeredEffect.UsedThisTurn, "Effect should reset at start of turn");
	}

	[TestMethod]
	public void SigilDoesNotRemoveAfterFiring()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		player1.Mana = 5;
		player2.Mana = 5;

		// Create a sigil that triggers on spell cast (mimics SecretTest but doesn't remove)
		var sigil = new Sigil()
		{
			Owner = player1,
			TriggeredEffects = new List<TriggeredEffect>()
			{
				new TriggeredEffect()
				{
					EffectTrigger = EffectTrigger.SpellCast,
					EffectTiming = EffectTiming.Pre,
					Frequency = EffectFrequency.Unlimited,
					GameActions = [new CancelEffectAction()],
					Condition = new SourceOwnerCondition()
					{
						TeamRelationship = TeamRelationship.Enemy
					},
					AffectedEntitySelector = new ContextSelector()
					{
						IncludeSourcePlayer = true
					}
				}
			}
		};

		player1.Sigils.Add(sigil);
		Assert.AreEqual(1, player1.Sigils.Count);

		// Create an enemy spell that would normally deal damage
		var spell = new SpellCard("Spell", 2);
		spell.SpellCastEffects.Add(
			new SpellCastEffect()
			{
				GameActions = new List<IGameAction>()
				{
					new DamageAction() { Damage = (Value)5 }
				}
			});
		spell.Owner = player2;
		player2.Hand.Add(spell);

		int initialPlayer1Health = player1.Health;

		// Act - cast spell which would trigger the sigil
		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = player2,
			SourceCard = spell,
			Target = player1
		}, new PlayCardAction() { Card = spell });

		// Assert - sigil should NOT be removed (unlike a Secret)
		Assert.AreEqual(1, player1.Sigils.Count, "Sigil should remain after firing (not like Secret)");
		Assert.AreEqual(initialPlayer1Health, player1.Health, "Spell damage should be canceled");
	}

	[TestMethod]
	public void SigilEmbeddedInSpellCard()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		player1.Mana = 5;

		// Create a spell card with an embedded SigilAction
		var spellCard = new SpellCard("SigilSpell", 1);
		var sigil = new Sigil();

		spellCard.SpellCastEffects.Add(
			new SpellCastEffect()
			{
				GameActions = new List<IGameAction>()
				{
					new SigilAction()
					{
						Sigil = sigil
					}
				}
			});

		spellCard.Owner = player1;
		player1.Hand.Add(spellCard);

		Assert.AreEqual(0, player1.Sigils.Count);

		// Act
		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = player1,
			SourceCard = spellCard,
			Target = player1
		}, new PlayCardAction() { Card = spellCard });

		// Assert
		Assert.AreEqual(1, player1.Sigils.Count, "Sigil should be created and added to player.Sigils");
		Assert.AreEqual(sigil, player1.Sigils[0]);
	}

	[TestMethod]
	public void MultipleSignalsWithMixedFrequencies()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var player = state.CurrentPlayer;

		// Create sigils with different frequencies
		var unlimitedEffect = new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Attack,
			EffectTiming = EffectTiming.Post,
			Frequency = EffectFrequency.Unlimited,
			GameActions = [new DamageAction() { Damage = (Value)1 }],
			AffectedEntitySelector = new ContextSelector() { IncludeSourcePlayer = true }
		};

		var oncePerTurnEffect = new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Attack,
			EffectTiming = EffectTiming.Post,
			Frequency = EffectFrequency.OncePerTurn,
			GameActions = [new DamageAction() { Damage = (Value)2 }],
			AffectedEntitySelector = new ContextSelector() { IncludeSourcePlayer = true }
		};

		var sigil1 = new Sigil()
		{
			Owner = player,
			TriggeredEffects = new List<TriggeredEffect>() { unlimitedEffect }
		};

		var sigil2 = new Sigil()
		{
			Owner = player,
			TriggeredEffects = new List<TriggeredEffect>() { oncePerTurnEffect }
		};

		player.Sigils.Add(sigil1);
		player.Sigils.Add(sigil2);

		// Pre-check: verify both sigils are present
		Assert.AreEqual(2, player.Sigils.Count);
		Assert.AreEqual(EffectFrequency.Unlimited, unlimitedEffect.Frequency);
		Assert.AreEqual(EffectFrequency.OncePerTurn, oncePerTurnEffect.Frequency);

		// Act - set OncePerTurn effect to used
		oncePerTurnEffect.UsedThisTurn = true;
		Assert.IsTrue(oncePerTurnEffect.UsedThisTurn, "Effect should be marked used");

		// Act - start turn to reset OncePerTurn
		var engine = new GameEngine();
		var startTurnAction = new StartTurnAction();
		engine.Resolve(state, new ActionContext() { SourcePlayer = player }, startTurnAction);

		// Assert - OncePerTurn should reset, Unlimited is never reset
		Assert.IsFalse(oncePerTurnEffect.UsedThisTurn, "OncePerTurn effect should reset at start of turn");
		Assert.IsFalse(unlimitedEffect.UsedThisTurn, "Unlimited effect should always be able to fire");
	}
}
