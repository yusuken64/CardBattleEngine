namespace CardBattleEngine.Test;

[TestClass]
public class RelicTest
{
	[TestMethod]
	public void PlayRelicFromHand()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player = state.CurrentPlayer;

		var relicCard = new RelicCard("TestRelic", 1, 5) { Owner = player };
		player.Hand.Add(relicCard);
		player.Mana = 2;

		var action = new PlayCardAction()
		{
			Card = relicCard,
		};
		var context = new ActionContext()
		{
			SourcePlayer = player,
			SourceCard = relicCard,
			Targets = [player],
		};

		// Act
		Assert.IsTrue(action.IsValid(state, context, out string _));
		engine.Resolve(state, context, action);

		// Assert - Relic should be on board
		Assert.AreEqual(1, player.Board.Count, "Relic should be added to board");
		var relic = (Relic)player.Board[0];
		Assert.AreEqual("TestRelic", relic.Name);
		Assert.AreEqual(5, relic.Health);
		Assert.AreEqual(0, relic.Attack);
	}

	[TestMethod]
	public void RelicPositionedCorrectlyBetweenMinions()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player = state.CurrentPlayer;

		var minionCard = new MinionCard("TestMinion", 1, 2, 2) { Owner = player };
		var relicCard = new RelicCard("TestRelic", 1, 5) { Owner = player };

		// Play first minion
		player.Board.Add(new Minion(minionCard, player) { Name = "minion1" });
		// Play relic - will be at index 1
		player.Board.Add(new Relic(relicCard, player) { Name = "relic1" });
		// Play second minion - will be at index 2
		player.Board.Add(new Minion(minionCard, player) { Name = "minion2" });

		// Assert positioning
		Assert.AreEqual(3, player.Board.Count);
		var minion1 = (Minion)player.Board[0];
		var relic1 = (Relic)player.Board[1];
		var minion2 = (Minion)player.Board[2];

		Assert.AreEqual("minion1", minion1.Name);
		Assert.AreEqual("relic1", relic1.Name);
		Assert.AreEqual("minion2", minion2.Name);
	}

	[TestMethod]
	public void RelicCannotAttack()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var relicCard = new RelicCard("TestRelic", 1, 5);
		var player = state.CurrentPlayer;
		var relic = new Relic(relicCard, player);

		// Act & Assert
		Assert.IsFalse(relic.CanAttack(), "Relic should not be able to attack");
	}

	[TestMethod]
	public void MinionCannotAttackRelic()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.CurrentPlayer;
		var player2 = state.OpponentOf(player1);

		var minionCard = new MinionCard("TestMinion", 1, 2, 2);
		var relicCard = new RelicCard("TestRelic", 1, 5);

		var minion = new Minion(minionCard, player1);
		minion.HasCharge = true;

		var relic = new Relic(relicCard, player2);
		player1.Board.Add(minion);
		player2.Board.Add(relic);

		var minionAttackBehavior = new MinionAttackBehavior();

		// Act & Assert
		Assert.IsFalse(minionAttackBehavior.IsValidAttackTarget(minion, relic, state, out string reason),
			"Minion should not be able to attack Relic");
		Assert.AreEqual("Relic can't be attacked", reason);
	}

	[TestMethod]
	public void HeroCannotAttackRelic()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var player1 = state.CurrentPlayer;
		var player2 = state.OpponentOf(player1);

		var relicCard = new RelicCard("TestRelic", 1, 5);
		var relic = new Relic(relicCard, player2);
		player2.Board.Add(relic);

		player1.Attack = 5;

		var heroAttackBehavior = new HeroAttackBehavior();

		// Act & Assert
		Assert.IsFalse(heroAttackBehavior.IsValidAttackTarget(player1, relic, state, out string reason),
			"Hero should not be able to attack Relic");
		Assert.AreEqual("Relic can't be attacked", reason);
	}

	[TestMethod]
	public void RelicCorrectlyPositionedBetweenMinions()
	{
		// Test: when a relic is placed between two minions on the board,
		// the adjacent minions (left and right) are correctly positioned,
		// proving that relics share the same board as minions with true left-to-right adjacency

		// Arrange
		var state = GameFactory.CreateTestGame();
		var player = state.CurrentPlayer;

		var testCard = new MinionCard("Test", 1, 5, 5);
		var relicCard = new RelicCard("Relic", 1, 5);

		// Player board: minion1 - relic - minion3 - minion4 - minion5
		var minion1 = new Minion(testCard, player) { Name = "minion1" };
		var relic = new Relic(relicCard, player) { Name = "relic1" };
		var minion3 = new Minion(testCard, player) { Name = "minion3" };
		var minion4 = new Minion(testCard, player) { Name = "minion4" };
		var minion5 = new Minion(testCard, player) { Name = "minion5" };

		player.Board.Add(minion1);
		player.Board.Add(relic);
		player.Board.Add(minion3);
		player.Board.Add(minion4);
		player.Board.Add(minion5);

		// Act & Assert - verify the board layout
		Assert.AreEqual(5, player.Board.Count);
		Assert.AreEqual(minion1, player.Board[0]);
		Assert.AreEqual(relic, player.Board[1]);
		Assert.AreEqual(minion3, player.Board[2]);
		Assert.AreEqual(minion4, player.Board[3]);
		Assert.AreEqual(minion5, player.Board[4]);

		// The relic is at position 1, with minion1 at position 0 and minion3 at position 2
		// This proves that relics and minions share the board with true left-to-right adjacency
		Assert.IsTrue(player.Board[0] is Minion, "Position 0 should be a minion");
		Assert.IsTrue(player.Board[1] is Relic, "Position 1 should be a relic");
		Assert.IsTrue(player.Board[2] is Minion, "Position 2 should be a minion");
	}

	[TestMethod]
	public void DamageCanTargetRelic()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player = state.CurrentPlayer;
		var opponent = state.OpponentOf(player);

		var relicCard = new RelicCard("TestRelic", 1, 5);
		var relic = new Relic(relicCard, opponent);
		opponent.Board.Add(relic);

		int initialHealth = relic.Health;

		var damageAction = new DamageAction() { Damage = (Value)2 };
		var context = new ActionContext()
		{
			SourcePlayer = player,
			Targets = [relic],
		};

		// Act
		engine.Resolve(state, context, damageAction);

		// Assert
		Assert.AreEqual(initialHealth - 2, relic.Health, "Relic should take damage");
	}

	[TestMethod]
	public void RelicDeathWhenHealthReachesZero()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var opponent = state.OpponentOf(state.CurrentPlayer);

		var relicCard = new RelicCard("TestRelic", 1, 3);
		var relic = new Relic(relicCard, opponent);
		opponent.Board.Add(relic);

		Assert.AreEqual(3, relic.Health);
		Assert.AreEqual(1, opponent.Board.Count);

		// Act - damage to 0
		var damageAction = new DamageAction() { Damage = (Value)5 };
		var context = new ActionContext()
		{
			SourcePlayer = state.CurrentPlayer,
			Targets = [relic],
		};
		engine.Resolve(state, context, damageAction);

		// Assert
		Assert.AreEqual(0, opponent.Board.Count, "Relic should be removed from board when health reaches 0");
	}

	[TestMethod]
	public void RelicAbilityCanBeActivatedOnce()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player = state.CurrentPlayer;

		var relicCard = new RelicCard("TestRelic", 1, 5);
		relicCard.Ability = new RelicAbility()
		{
			Name = "TestAbility",
			ManaCost = 2,
			UsedThisTurn = false,
			GameActions = [new DamageAction() { Damage = (Value)1 }],
			AffectedEntitySelector = new ContextSelector() { IncludeTarget = true }
		};

		var opponent = state.OpponentOf(player);
		var relic = new Relic(relicCard, player);
		player.Board.Add(relic);
		player.Mana = 2;

		int initialOpponentHealth = opponent.Health;

		var action = new RelicAbilityAction();
		var context = new ActionContext()
		{
			SourcePlayer = player,
			Source = relic,
			Targets = [opponent],
		};

		// Act - activate ability first time
		Assert.IsTrue(action.IsValid(state, context, out string _));
		engine.Resolve(state, context, action);

		// Assert
		Assert.IsTrue(relic.Ability.UsedThisTurn, "Ability should be marked as used");
		Assert.AreEqual(initialOpponentHealth - 1, opponent.Health, "Damage should be applied");
		Assert.AreEqual(0, player.Mana, "Mana should be spent");

		// Act - try to activate again same turn
		Assert.IsFalse(action.IsValid(state, context, out string _), "Should not be valid to use ability again same turn");
	}

	[TestMethod]
	public void RelicAbilityResetsAtStartOfTurn()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player = state.CurrentPlayer;

		var relicCard = new RelicCard("TestRelic", 1, 5);
		relicCard.Ability = new RelicAbility()
		{
			Name = "TestAbility",
			ManaCost = 1,
			UsedThisTurn = false,
			GameActions = [],
			AffectedEntitySelector = new ContextSelector() { IncludeTarget = true }
		};

		var relic = new Relic(relicCard, player);
		player.Board.Add(relic);

		// Simulate using the ability
		relic.Ability.UsedThisTurn = true;
		Assert.IsTrue(relic.Ability.UsedThisTurn, "Pre-check: ability should be marked used");

		// Act - execute start turn logic
		var action = new StartTurnAction();
		var context = new ActionContext()
		{
			SourcePlayer = player
		};
		engine.Resolve(state, context, action);

		// Assert
		Assert.IsFalse(relic.Ability.UsedThisTurn, "Ability should reset at start of turn");
	}

	[TestMethod]
	public void RelicWithChargesDecrementsAndDiesAtZero()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player = state.CurrentPlayer;

		var relicCard = new RelicCard("ChargedRelic", 1, 5);
		relicCard.Charges = 3;
		relicCard.RelicTriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Attack,
			EffectTiming = EffectTiming.Post,
			Scope = TriggerScope.Self,
			GameActions = [new DamageAction() { Damage = (Value)1 }],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSourcePlayer = true
			}
		});

		var relic = new Relic(relicCard, player);
		player.Board.Add(relic);
		var opponent = state.OpponentOf(player);

		Assert.AreEqual(3, relic.Charges);
		Assert.AreEqual(1, player.Board.Count);

		// Act - trigger effect 3 times
		for (int i = 0; i < 3; i++)
		{
			var triggerAction = new TriggerEffectAction()
			{
				TriggeredEffect = relicCard.RelicTriggeredEffects[0],
				TriggerSource = relic,
				EffectContext = new ActionContext()
				{
					SourcePlayer = player,
					Source = relic,
					Targets = [opponent]
				}
			};
			var context = new ActionContext()
			{
				SourcePlayer = player,
				Source = relic,
				Targets = [opponent]
			};
			engine.Resolve(state, context, triggerAction);

			if (i < 2)
			{
				Assert.AreEqual(3 - (i + 1), relic.Charges, $"Charges should decrement on fire {i + 1}");
				Assert.AreEqual(1, player.Board.Count, $"Relic should still be on board after fire {i + 1}");
			}
		}

		// Assert - relic should be removed after third trigger
		Assert.AreEqual(0, player.Board.Count, "Relic should be removed when charges reach 0");
	}

	[TestMethod]
	public void PlayRelicWhenBoardFull()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var player = state.CurrentPlayer;

		var minionCard = new MinionCard("TestMinion", 1, 2, 2);
		var relicCard = new RelicCard("TestRelic", 1, 5);
		relicCard.Owner = player;
		relicCard.ManaCost = 1;

		// Fill board with minions up to MaxBoardSize
		for (int i = 0; i < state.MaxBoardSize; i++)
		{
			player.Board.Add(new Minion(minionCard, player));
		}

		Assert.AreEqual(state.MaxBoardSize, player.Board.Count);

		// Add relic card to hand
		player.Hand.Add(relicCard);
		player.Mana = 10; // Ensure enough mana

		// Create action to play relic
		var action = new PlayCardAction() { Card = relicCard };
		var context = new ActionContext()
		{
			SourcePlayer = player,
			SourceCard = relicCard,
			Targets = [player]
		};

		// Act & Assert
		Assert.IsFalse(action.CanCast(state, context, out string reason),
			"Should not be able to play relic when board is full");
		Assert.AreEqual("Board is Full", reason);
	}

	[TestMethod]
	public void PlayMinionWhenBoardFullOfRelics()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var player = state.CurrentPlayer;

		var minionCard = new MinionCard("TestMinion", 1, 2, 2);
		var relicCard = new RelicCard("TestRelic", 1, 5);
		minionCard.Owner = player;
		minionCard.ManaCost = 1;

		// Fill board with relics up to MaxBoardSize
		for (int i = 0; i < state.MaxBoardSize; i++)
		{
			player.Board.Add(new Relic(relicCard, player));
		}

		Assert.AreEqual(state.MaxBoardSize, player.Board.Count);

		// Add minion card to hand
		player.Hand.Add(minionCard);
		player.Mana = 10; // Ensure enough mana

		// Create action to play minion
		var action = new PlayCardAction() { Card = minionCard };
		var context = new ActionContext()
		{
			SourcePlayer = player,
			SourceCard = minionCard,
			Targets = [player]
		};

		// Act & Assert
		Assert.IsFalse(action.CanCast(state, context, out string reason),
			"Should not be able to play minion when board is full of relics");
		Assert.AreEqual("Board is Full", reason);
	}
}
