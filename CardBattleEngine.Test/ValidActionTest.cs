using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardBattleEngine.Test;

[TestClass]
public class ValidActionTest
{
	[TestMethod]
	public void ValidActionTestBattleCry()
	{

		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.Players[1];

		MinionCard archerCard = new MinionCard("archer", 1, 1, 1);
		archerCard.MinionTriggeredEffects.Add(new TriggeredEffect()
		{
			AffectedEntitySelector = new ContextSelector
			{
				IncludeTarget = true,
			},
			EffectTrigger = EffectTrigger.Battlecry,
			GameActions = [new DamageAction() {
				Damage = (Value)1
			}]
		});
		archerCard.ValidTargetSelector = new EntityTypeSelector()
		{
			EntityTypes = EntityType.Minion | EntityType.Player,
			TeamRelationship = TeamRelationship.Enemy,
		};

		archerCard.Owner = opponent;
		opponent.Hand.Add(archerCard);

		var actions = state.GetValidActions(opponent);

		Assert.AreEqual(2, actions.Count());
	}

	[TestMethod]
	public void ValidActionTestNoTarget()
	{

		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.Players[1];

		MinionCard archerCard = new MinionCard("sergeant", 1, 1, 1);
		archerCard.MinionTriggeredEffects.Add(new TriggeredEffect()
		{
			AffectedEntitySelector = new ContextSelector
			{
				IncludeTarget = true,
			},
			EffectTrigger = EffectTrigger.Battlecry,
			GameActions = [new DamageAction() {
				Damage = (Value)1
			}]
		});
		archerCard.ValidTargetSelector = new EntityTypeSelector()
		{
			EntityTypes = EntityType.Minion,
			TeamRelationship = TeamRelationship.Friendly,
		};

		archerCard.Owner = opponent;
		opponent.Hand.Add(archerCard);

		var actions = state.GetValidActions(opponent);

		Assert.AreEqual(2, actions.Count());
	}
}
