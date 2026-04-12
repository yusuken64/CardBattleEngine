using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardBattleEngine.Test;

[TestClass]
public class ArmorTest
{
	//Whenever a friendly minion takes damage, gain 1 Armor.
	[TestMethod]
	
	public void ArmorSmithTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		MinionCard testMinion = new MinionCard("Test", 3, 3, 3);
		player2.Board.Add(new Minion(testMinion, player2));
		player1.Board.Add(new Minion(testMinion, player1));

		var armorSmithCard = new MinionCard("ArmorTest", 1, 1, 4);
		armorSmithCard.MinionTriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.OnDamage,
			EffectTiming = EffectTiming.Post,
			GameActions = [new GainArmorAction() {
				Amount = (Value)1
			}],
			Condition = new CombinedCondition()
			{
				Right =	new TargetOwnerCondition
				{
					TeamRelationship = TeamRelationship.Friendly
				},
				Operation = CombinationOperation.And,
				Left = new TargetTypeCondition()
				{
					EntityTypes = EntityType.Minion
				}
			}
			,
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeTargetOwner = true,
			}
		});

		player1.Board.Add(new Minion(armorSmithCard, player1));

		engine.Resolve(
			state,
			new ActionContext()
			{
				Source = player2,
				Target = player1.Board[0]
			},
			new DamageAction() { Damage = (Value)1 });

		Assert.AreEqual(1, player1.Armor);

		engine.Resolve(
			state,
			new ActionContext()
			{
				Source = player2,
				Target = player1
			},
			new DamageAction() { Damage = (Value)1 });

		Assert.AreEqual(0, player1.Armor);

		engine.Resolve(
			state,
			new ActionContext()
			{
				Source = player2.Board[0],
				Target = player1
			},
			new DamageAction() { Damage = (Value)1 });

		Assert.AreEqual(0, player1.Armor);
	}
}
