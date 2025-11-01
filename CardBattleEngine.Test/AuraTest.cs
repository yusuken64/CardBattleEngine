namespace CardBattleEngine.Test;

[TestClass]
public class AuraTest
{
	[TestMethod]
	public void MurlocAuraTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		current.Mana = 10; // ensure enough for all summons
		var opponent = state.OpponentPlayer;

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
		auraCard.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Aura,
			EffectTiming = EffectTiming.Persistant,
			GameActions = new List<IGameAction>()
			{
				new AddStatModifierAction()
				{
					AttackChange = +2,
					Duration = EffectDuration.Aura
				}
			},
			AffectedEntitySelector = new TargetOperationSelector()
			{
				Operations = new List<ITargetOperation>()
				{
					new SelectBoardEntitiesOperation() {
						Group = TargetGroup.Minions,
						Side = TargetSide.Friendly
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

		Assert.IsTrue(action.IsValid(state, actionContext));

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
}
