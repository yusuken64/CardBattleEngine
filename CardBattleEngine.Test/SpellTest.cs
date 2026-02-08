namespace CardBattleEngine.Test;

[TestClass]
public class SpellTest
{
	[TestMethod]
	public void DrawCardSpellTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentOf(current);

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var spellCard = cardDatabase.GetSpellCard("DrawCardSpell", current);
		current.Hand.Add(spellCard);

		int initialHandCount = current.Hand.Count;
		int initialDeckCount = current.Deck.Count;

		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = current,
			SourceCard = spellCard,
			Target = current
		}, new PlayCardAction { Card = spellCard });

		Assert.AreEqual(initialHandCount + 2, current.Hand.Count, "Player should have drawn three cards.");
		Assert.AreEqual(initialDeckCount - 3, current.Deck.Count, "Deck should have one less minionCard.");
	}

	[TestMethod]
	public void FrostBoltSpellTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentOf(current);

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var spellCard = cardDatabase.GetSpellCard("FrostBolt", current);
		current.Hand.Add(spellCard);

		var card = cardDatabase.GetMinionCard("TestMinion", opponent);
		card.Health = 5;
		var enemyMinion = new Minion(card, opponent);
		opponent.Board.Add(enemyMinion);

		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = current,
			SourceCard = spellCard,
			Target = enemyMinion,
		}, new PlayCardAction { Card = spellCard });

		// Assert
		Assert.AreEqual(2, opponent.Board[0].Health, "Frostbolt should deal 3 damage.");
		Assert.IsTrue(opponent.Board[0].IsFrozen, "Frostbolt should freeze the target.");
	}

	[TestMethod]
	public void FlamestrikeSpellTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 7; // Assume Flamestrike costs 7
		var opponent = state.OpponentOf(current);

		CardDatabase cardDatabase = new(CardDBTest.DBPath);
		var spellCard = cardDatabase.GetSpellCard("FlameStrike", current);
		current.Hand.Add(spellCard);

		// Create multiple enemy minions with varying health
		for (int i = 0; i < 5; i++)
		{
			var card = cardDatabase.GetMinionCard("TestMinion", opponent);
			var enemyMinion = new Minion(card, opponent)
			{
				Health = 6
			};
			opponent.Board.Add(enemyMinion);
		}

		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = current,
			SourceCard = spellCard,
			Target = null,
		}, new PlayCardAction { Card = spellCard });

		// Assert - All enemy minions should have taken 5 damage
		foreach (var minion in opponent.Board)
		{
			Assert.AreEqual(1, minion.Health, "Flamestrike should deal 5 damage to all enemy minions.");
		}
	}

	[TestMethod]
	public void WhirlwindTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 7;
		var opponent = state.OpponentOf(current);

		var card = new MinionCard("Test", 1, 4, 4);
		current.Board.Add(new Minion(card, opponent));
		current.Board.Add(new Minion(card, opponent));
		current.Board.Add(new Minion(card, opponent));
		opponent.Board.Add(new Minion(card, opponent));
		opponent.Board.Add(new Minion(card, opponent));
		opponent.Board.Add(new Minion(card, opponent));

		var spell = new SpellCard("WhirlWind", 1);
		spell.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [ new DamageAction() {
				Damage = (Value)1
			}],
			AffectedEntitySelector = new TargetOperationSelector()
			{
				ResolutionTiming = TargetResolutionTiming.Once,
				Operations = [new SelectBoardEntitiesOperation() {
					Group = TargetGroup.All,
					Side = TeamRelationship.Any
				}]
			}
		});

		ActionContext context = new()
		{
			SourceCard = spell,
			Source = current,
			SourcePlayer = current,
			AffectedEntitySelector = new TargetOperationSelector()
			{
				ResolutionTiming = TargetResolutionTiming.Once,
				Operations = [new SelectBoardEntitiesOperation() {
					Group = TargetGroup.All,
					Side = TeamRelationship.Any
				}]
			}
		};
		engine.Resolve(state, context, new CastSpellAction());

		foreach (var minion in current.Board)
		{
			Assert.AreEqual(3, minion.Health, "Current player minion did not take 1 damage");
		}

		foreach (var minion in opponent.Board)
		{
			Assert.AreEqual(3, minion.Health, "Opponent minion did not take 1 damage");
		}
	}


	[TestMethod]
	public void SapTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 7;
		var opponent = state.OpponentOf(current);

		var card = new MinionCard("Test", 1, 4, 4);
		current.Board.Add(new Minion(card, opponent));
		opponent.Board.Add(new Minion(card, opponent));

		var spell = new SpellCard("Sap", 1);
		spell.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [ new ReturnMinionToCard()
			{
				TeamRelationship = TeamRelationship.Friendly,
				ZoneType = ZoneType.Hand
			} ],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeTarget = true
			}
		});

		ActionContext context = new()
		{
			SourceCard = spell,
			Source = current,
			SourcePlayer = current,
			Target = opponent.Board[0]
		};

		Assert.AreEqual(0, opponent.Hand.Count(), "Hand is empty");

		engine.Resolve(state, context, new CastSpellAction());

		Assert.AreEqual(0, opponent.Board.Count(), "Minion should return to hand");
		Assert.AreEqual(1, opponent.Hand.Count(), "Minion should return to hand");
	}

	[TestMethod]
	public void GenerateCostTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 7;
		var opponent = state.OpponentOf(current);

		SpellCard fireball = new ("FireBall", 0);
		fireball.ValidTargetSelector = new EntityTypeSelector()
		{
			EntityTypes = EntityType.Player | EntityType.Minion,
			TeamRelationship = TeamRelationship.Any,
		};
		fireball.SpellCastEffects.Add(
			new SpellCastEffect()
			{
				AffectedEntitySelector = new ContextSelector()
				{
					IncludeTarget = true
				},
				GameActions = [new DamageAction()
				{
					Damage = (Value)6,
				}]
			}
		);

		SpellCard test = new("test", 0);
		fireball.SpellCastEffects.Add(
			new SpellCastEffect()
			{
				AffectedEntitySelector = new ContextSelector()
				{
					IncludeSourcePlayer	= true,
				},
				GameActions = [new GainManaAction() {
					Amount = (Value)1,
				}]
			}
		);
		test.Owner = current;
		current.Hand.Add(test);

		var minionCard = new MinionCard("Test", 1, 4, 4)
		{
			MinionTriggeredEffects = [
				new TriggeredEffect()
				{
					EffectTiming = EffectTiming.Post,
					EffectTrigger = EffectTrigger.SpellCast,
					AffectedEntitySelector = new ContextSelector()
					{
						IncludeSourcePlayer = true,
					},
					GameActions = [
						new GainCardAction()
						{
							Card = fireball,
							GenerateNewCard = true
						}
					]
				}
			]
		};
		current.Board.Add(new Minion(minionCard, current));

		engine.Resolve(
			state,
			new ActionContext()
			{
				Target = current,
				SourcePlayer = current,
			},
			new PlayCardAction()
			{
				Card = test
			});

		Assert.IsTrue(current.Hand.Any(x => x.Name == "FireBall"));
		Assert.AreEqual(1, current.Hand.Count());
		
		engine.Resolve(
			state,
			new ActionContext()
			{
				Target = current,
				SourcePlayer = current,
			},
			new PlayCardAction()
			{
				Card = test
			});

		fireball.SpellCastEffects.Add(
			new SpellCastEffect()
			{
				AffectedEntitySelector = new ContextSelector()
				{
					IncludeSourcePlayer = true,
				},
				GameActions = [new GainManaAction() {
					Amount = (Value)1,
				}]
			}
		);
		test.Owner = current;
		current.Hand.Add(test);

		engine.Resolve(
			state,
			new ActionContext()
			{
				Target = current,
				SourcePlayer = current,
			},
			new PlayCardAction()
			{
				Card = test
			});

		Assert.AreEqual(2, current.Hand.Count(x => x.Name == "FireBall"));
		Assert.IsFalse(current.Hand[0] == current.Hand[1]);

		engine.Resolve(
			state,
			new ActionContext()
			{
				Target = current,
				SourcePlayer = current,
			},
			new PlayCardAction()
			{
				Card = current.Hand[0]
			});

		Assert.AreEqual(2, current.Hand.Count(x => x.Name == "FireBall"));
	}
}
