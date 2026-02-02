namespace CardBattleEngine.Test;

[TestClass]
public class AuraTest
{
	[TestMethod]
	public void MurlocAuraTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Mana = 10; // ensure enough for all summons
		var opponent = state.OpponentOf(current);

		// Create normal Murloc
		var card = new MinionCard("Murloc", cost: 0, attack: 1, health: 1);
		card.MinionTribes = [MinionTribe.Murloc];
		card.Owner = current;

		// Create non-Murloc
		var card2 = new MinionCard("TestMinion", cost: 0, attack: 1, health: 1);
		card.MinionTribes = [MinionTribe.None];
		card2.Owner = current;

		// Create aura Murloc
		var auraCard = new MinionCard("AuraMurloc", cost: 0, attack: 1, health: 1);
		card.MinionTribes = [MinionTribe.Murloc];
		auraCard.Owner = current;
		auraCard.MinionTriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Aura,
			EffectTiming = EffectTiming.Persistant,
			GameActions = new List<IGameAction>()
			{
				new AddStatModifierAction()
				{
					AttackChange = (Value)2
				}
			},
			AffectedEntitySelector = new TargetOperationSelector()
			{
				Operations = new List<ITargetOperation>()
				{
					new SelectBoardEntitiesOperation() {
						Group = TargetGroup.Minions,
						Side = TeamRelationship.Friendly
					},
					new TribeOperation()
					{
						Tribe = MinionTribe.Murloc,
						ExcludeSelf = true
					}
				}
			}
		});

		current.Hand.Add(card);
		current.Hand.Add(card2);
		current.Hand.Add(auraCard);

		// Summon the normal Murloc and non-Murloc first
		PlayCardAction action = new() { Card = card };
		ActionContext actionContext = new() { SourcePlayer = current };

		Assert.IsTrue(action.IsValid(state, actionContext, out string _));

		engine.Resolve(state, actionContext , action);
		engine.Resolve(state, actionContext, new PlayCardAction() { Card = card2 });

		var murloc = current.Board.First(m => m.Name == "Murloc");
		var nonMurloc = current.Board.First(m => m.Name == "TestMinion");

		Assert.AreEqual(1, murloc.Attack);
		Assert.AreEqual(1, nonMurloc.Attack);

		// Now summon the aura Murloc — its aura should immediately apply
		engine.Resolve(state, actionContext, new PlayCardAction() { Card = auraCard });

		var auraMurloc = current.Board.First(m => m.Name == "AuraMurloc");

		// Aura should buff only other friendly Murlocs
		Assert.AreEqual(3, murloc.Attack, "Murloc should gain +2 attack from aura");
		Assert.AreEqual(1, nonMurloc.Attack, "Non-Murloc should not gain buff");
		Assert.AreEqual(1, auraMurloc.Attack, "Aura minion should not buff itself");

		// Remove aura minion (simulate death)
		engine.Resolve(state, new ActionContext()
		{
			Target = auraMurloc
		}, new DeathAction());

		// Aura should be gone, so attack returns to normal
		Assert.AreEqual(1, murloc.Attack, "Murloc should lose aura buff when AuraMurloc dies");
		Assert.AreEqual(1, nonMurloc.Attack, "Non-Murloc remains unchanged");
	}

	[TestMethod]
	public void StatModiferTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		MinionCard testMinion = new MinionCard("Test", 1, 3, 3);
		Minion minion = new(testMinion, current);
		current.Board.Add(minion);

		engine.Resolve(state, new ActionContext() { Target = minion }, new AddStatModifierAction()
		{
			AttackChange = (Value)1,
			HealthChange = (Value)1,
			StatModifierType = StatModifierType.Additive
		});
	}

	[TestMethod]
	public void StatModifier_MixedAdditiveAndSet_ResolvesInOrder()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var testCard = new MinionCard("Test", attack: 3, health: 5, cost: 1);
		var minion = new Minion(testCard, current);
		current.Board.Add(minion);

		// Base sanity check
		Assert.AreEqual(3, minion.Attack);
		Assert.AreEqual(5, minion.MaxHealth);
		Assert.AreEqual(5, minion.Health);

		// Act 1: +2/+2 (additive)
		engine.Resolve(
			state,
			new ActionContext { Target = minion },
			new AddStatModifierAction
			{
				AttackChange = (Value)2,
				HealthChange = (Value)2,
				StatModifierType = StatModifierType.Additive
			});

		// Assert 1
		Assert.AreEqual(5, minion.Attack);
		Assert.AreEqual(7, minion.MaxHealth);
		Assert.AreEqual(7, minion.Health);

		// Act 2: Set attack to 1 (set, attack only)
		engine.Resolve(
			state,
			new ActionContext { Target = minion },
			new AddStatModifierAction
			{
				AttackChange = (Value)1,
				HealthChange = null,
				StatModifierType = StatModifierType.Set
			});

		// Assert 2
		Assert.AreEqual(1, minion.Attack, "Set should override previous additive attack");
		Assert.AreEqual(7, minion.MaxHealth, "Health should be untouched by attack-only set");
		Assert.AreEqual(7, minion.Health);

		// Act 3: +3 attack (additive after set)
		engine.Resolve(
			state,
			new ActionContext { Target = minion },
			new AddStatModifierAction
			{
				AttackChange = (Value)3,
				HealthChange = null,
				StatModifierType = StatModifierType.Additive
			});

		// Assert 3
		Assert.AreEqual(4, minion.Attack, "Additive after set should apply");
		Assert.AreEqual(7, minion.MaxHealth);
		Assert.AreEqual(7, minion.Health);

		// Act 4: Set health to 4 (set health only)
		engine.Resolve(
			state,
			new ActionContext { Target = minion },
			new AddStatModifierAction
			{
				AttackChange = null,
				HealthChange = (Value)4,
				StatModifierType = StatModifierType.Set
			});

		// Assert 4
		Assert.AreEqual(4, minion.Attack, "Attack should be untouched by health-only set");
		Assert.AreEqual(4, minion.MaxHealth);
		Assert.AreEqual(4, minion.Health);
	}
}
