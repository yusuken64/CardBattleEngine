using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardBattleEngine.Test;

[TestClass]
public class GaneStartTest
{
	[TestMethod]
	public void GameStartTriggerTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var p1 = state.Players[0];
		var p2 = state.Players[1];

		int count = 0;
		p2.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.GameStart,
			EffectTiming = EffectTiming.Post,
			GameActions = [new DebugLambaAction() {
				IsValidFunc = (s, a) => { return true; },
				ResolveFunc = (s, a) =>
					{
						count++;
						return [];
					}
			}],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSourcePlayer = true,
			}
		});

		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = p1,
			Source = p1,
		}, new StartGameAction());
		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = p2,
			Source = p2,
		}, new StartGameAction());

		Assert.AreEqual(2, count);
	}

	[TestMethod]
	public void GameStartTriggerConditionTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var p1 = state.Players[0];
		var p2 = state.Players[1];

		int count = 0;
		p2.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.GameStart,
			EffectTiming = EffectTiming.Post,
			GameActions = [new DebugLambaAction() {
				IsValidFunc = (s, a) => { return true; },
				ResolveFunc = (s, a) =>
					{
						count++;
						return [];
					}
			}],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSourcePlayer = true,
			},
			Condition = new OriginalSourceCondition()
		});

		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = p1,
			Source = p1,
		}, new StartGameAction());

		Assert.AreEqual(0, count);

		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = p2,
			Source = p2,
		}, new StartGameAction());

		Assert.AreEqual(1, count);
	}
}
