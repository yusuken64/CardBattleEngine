using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardBattleEngine.Test;

[TestClass]
public class ControlActionTest
{
	[TestMethod]
	public void RepeatTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];
		int count = 0;
		player1.Mana = 1;

		SpellCard spellCard = new SpellCard("Repeat Spell", 1);
		spellCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [new RepeatAction()
			{
				Count = (Value)3,
				ChildActions = [new DebugLambaAction() {
					IsValidFunc = (state, context) => true,
					ResolveFunc = (state, context) =>
					{
						count++;
						return [];
					}
				}]
			}]
		});

		spellCard.Owner = player1;
		player1.Hand.Add(spellCard);

		engine.Resolve(
			state,
			new ActionContext()
			{
				SourcePlayer =  player1,
				Source = player1
			},
			new PlayCardAction()
			{
				Card = spellCard
			});

		Assert.AreEqual(3, count);
	}
}
