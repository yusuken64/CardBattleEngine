namespace CardBattleEngine.Test;

[TestClass]
public class BattleEffectTest
{
	[TestMethod]
	public void FreezeTest_AllCases()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		engine.StartGame(state);

		// Add minions to both boards
		var minionCard1 = new MinionCard("TestMinion1", 1, 2, 2) { Owner = player1 };
		var minionCard2 = new MinionCard("TestMinion2", 1, 2, 2) { Owner = player2 };

		var minion1 = new Minion(minionCard1, player1);
		var minion2 = new Minion(minionCard2, player2);

		player1.Board.Add(minion1);
		player2.Board.Add(minion2);

		var freezeMinionAction = new FreezeAction();
		engine.Resolve(state, new ActionContext { Target = minion2, SourcePlayer = player1 }, freezeMinionAction);

		//enemy minion is frozen and hasn't missed attack yet
		Assert.IsTrue(minion2.IsFrozen, "Enemy minion should be frozen");
		Assert.IsFalse(minion2.MissedAttackFromFrozen, "Minion has not yet missed an attack");

		//Freeze enemy hero
		var freezeHeroAction = new FreezeAction();
		engine.Resolve(state, new ActionContext { Target = player2, SourcePlayer = player1 }, freezeHeroAction);

		Assert.IsTrue(player2.IsFrozen, "Enemy hero should be frozen");

		// Act 2: Start frozen player's turn -> mark that freeze caused missed attack
		engine.Resolve(state, new ActionContext { SourcePlayer = player2 }, new StartTurnAction());

		// Assert 2: minion is still frozen but now counted as missed attack
		Assert.IsTrue(minion2.IsFrozen, "Enemy minion should still be frozen at start of turn");
		Assert.IsTrue(minion2.MissedAttackFromFrozen, "Minion missed attack due to freeze");

		// Act 3: Attempt attack with frozen minion
		var attackAction = new AttackAction();
		var attackContext = new ActionContext { Source = minion2, Target = minion1, SourcePlayer = player2 };
		Assert.IsFalse(attackAction.IsValid(state, attackContext), "Frozen minion cannot attack");

		// Act 5: End frozen player's turn -> freeze should wear off
		engine.Resolve(state, new ActionContext { SourcePlayer = player2 }, new StartTurnAction());

		// Assert 4: freeze removed from minion and hero
		Assert.IsFalse(minion2.IsFrozen, "Enemy minion freeze should wear off at end of turn");
		Assert.IsFalse(minion2.MissedAttackFromFrozen, "MissedAttackFromFrozen flag should be reset");
		Assert.IsFalse(player2.IsFrozen, "Enemy hero freeze should wear off at end of turn");
	}

	[TestMethod]
	public void TempBuff()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		var testCard = new MinionCard("Test", 1, 1, 1);
		var card = new MinionCard("TempBuff", 1, 1, 1);
		card.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Battlecry,
			EffectTiming = EffectTiming.Post,
			TargetType = TargetingType.None,
			GameActions = [new AddStatModifierAction() {
				AttackChange = 2,
				ExpirationTrigger = new TriggeredEffect()
				{
					EffectTrigger = EffectTrigger.OnTurnEnd,
					EffectTiming = EffectTiming.Post,
					GameActions = [new RemoveModifierAction()]
				}
			}],
			AffectedEntitySelector = new TargetOperationSelector()
			{
				Operations = [new SelectBoardEntitiesOperation()
				{
					Group = TargetGroup.Minions,
					Side = TeamRelationship.Friendly,
					ExcludeSelf = true
				}]
			}
		});
		card.Owner = player1;
		player1.Hand.Add(card);

		player1.Board.Add(new Minion(testCard, player1));
		player1.Board.Add(new Minion(testCard, player1));
		player1.Board.Add(new Minion(testCard, player1));

		ActionContext actionContext = new()
		{
			SourcePlayer = player1
		};
		PlayCardAction playCardAction = new()
		{
			Card = card,
		};

		engine.Resolve(state, actionContext, playCardAction);
		
		// Assert
		Assert.AreEqual(4, player1.Board.Count);
		Assert.AreEqual(3, player1.Board[0].Attack);
		Assert.AreEqual(3, player1.Board[1].Attack);
		Assert.AreEqual(3, player1.Board[2].Attack);
		Assert.AreEqual(1, player1.Board[3].Attack);
	}

	[TestMethod]
	public void CleaveTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		var testCard = new MinionCard("Test", 1, 5, 5);
		player2.Board.Add(new Minion(testCard, player2) { Name = "minion1" });
		player2.Board.Add(new Minion(testCard, player2) { Name = "minion2" });
		player2.Board.Add(new Minion(testCard, player2) { Name = "minion3" });
		player2.Board.Add(new Minion(testCard, player2) { Name = "minion4" });
		player2.Board.Add(new Minion(testCard, player2) { Name = "minion5" });

		var cleaveCard = new MinionCard("Cleave", 1, 4, 4);
		cleaveCard.TriggeredEffects.Add(new TriggeredEffect()
		{
			TargetType = TargetingType.None,
			EffectTiming = EffectTiming.Pre,
			EffectTrigger = EffectTrigger.Attack,
			Condition = new OriginalSourceCondition(),
			GameActions =
			[
				new DeferredResolveAction()
				{
					Action = new DamageAction()
					{
						Damage = new StatValue()
						{
							EntityStat = Stat.Attack,
							EntityContextProvider = ContextProvider.Source
						}
					},
					AffectedEntitySelector = new TargetOperationSelector()
					{
						Operations =
						[new CleaveOperation()
						{
							IncludeCenter = false
						}]
					}
				}
			],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeTarget = true
			}

		});
		cleaveCard.HasCharge = true;
		player1.Board.Add(new Minion(cleaveCard, player1));

		var attackAction = new AttackAction();
		var actionContext = new ActionContext()
		{
			Source = player1.Board[0],
			Target = player2.Board[2],
		};
		engine.Resolve(state, actionContext, attackAction);

		Assert.AreEqual(5, player2.Board[0].Health);
		Assert.AreEqual(1, player2.Board[1].Health);
		Assert.AreEqual(1, player2.Board[2].Health);
		Assert.AreEqual(1, player2.Board[3].Health);
		Assert.AreEqual(5, player2.Board[4].Health);
	}

	[TestMethod]
	public void SummonTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		var testCard = new MinionCard("Test", 1, 5, 5);
		testCard.TriggeredEffects.Add(
			new TriggeredEffect()
			{
				TargetType = TargetingType.None,
				EffectTiming = EffectTiming.Post,
				EffectTrigger = EffectTrigger.Battlecry,
				GameActions = 
				[
					new SummonMinionAction()
					{
						Card = new MinionCard("Summoned", 1, 2, 2)
					}
				],
				AffectedEntitySelector = null
			});
		player1.Mana = 1;
		player1.Hand.Add(testCard);
		testCard.Owner = player1;

		IGameAction playCardAction = new PlayCardAction()
		{
			Card = testCard,
		};
		ActionContext actionContext = new()
		{
			SourcePlayer = player1,
		};
		engine.Resolve(state, actionContext, playCardAction);

		Assert.AreEqual(2, player1.Board.Count());
	}
}
