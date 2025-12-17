using System.Net.Http.Headers;

namespace CardBattleEngine.Test;

[TestClass]
public class CostReduction
{
	[TestMethod]
	public void CardCostReduction()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Hand.Add(new MinionCard("Test", cost: 1, attack: 1, health: 1));
		current.Hand.Add(new MinionCard("Test", cost: 1, attack: 1, health: 1));
		current.Hand.Add(new MinionCard("Test", cost: 1, attack: 1, health: 1));
		current.Hand.Add(new MinionCard("Test", cost: 1, attack: 1, health: 1));
		current.Hand.Add(new MinionCard("Test", cost: 1, attack: 1, health: 1));
		current.Hand.Add(new MinionCard("Test", cost: 1, attack: 1, health: 1));
		current.Hand.Add(new MinionCard("Test", cost: 1, attack: 1, health: 1));

		var mountainGiantCard = new MinionCard("MountainGiant", cost: 12, attack: 8, health: 8);
		mountainGiantCard.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Aura,
			TargetType = TargetingType.None,
			EffectTiming = EffectTiming.Persistant,
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSource = true
			},
			GameActions = [new AddStatModifierAction()
			{
				CostChange = new NegativeValue()
				{
					OriginalValue = new NumberOfCardsInHand()
					{
						TeamRelationship = TeamRelationship.Friendly,
					}
				}
			}]
		});
		current.Hand.Add(mountainGiantCard);
		mountainGiantCard.Owner = current;

		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = current,
		}, new DebugLambaAction()
		{
			IsValidFunc = (gamestate, context) => true,
			ResolveFunc = (gamestate, context) => []
		});

		Assert.AreEqual(4, mountainGiantCard.ManaCost);
	}
}
