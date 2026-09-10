namespace CardBattleEngine.Test;

[TestClass]
public class HistoryTest
{
	[TestMethod]
	public void HistoryRecordACtionTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		ActionContext actionContext = new();
		IGameAction action = new DebugLambaAction()
		{
			IsValidFunc = (state, context) => true,
			ResolveFunc = (state, context) => { return []; }
		};

		Assert.IsTrue(action.IsValid(state, actionContext, out string _));
		engine.Resolve(state, actionContext, action);
		
		Assert.AreEqual(1, state.History.Count);
		Assert.AreEqual(action, state.History[0].Action);
	}

	[TestMethod]
	public void HistoryLogsSideEffects()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		// Arrange: Create an action that returns another action as side effect
		var mainAction = new DebugLambaAction
		{
			IsValidFunc = (s, c) => true,
			ResolveFunc = (s, c) => new List<(IGameAction, ActionContext)>
			{
				(new DebugLambaAction 
				{
					IsValidFunc = (s2, c2) => true,
					ResolveFunc = (s2, c2) => new List<(IGameAction, ActionContext)>()
				}, c)
			}
		};

		// Act
		engine.Resolve(state, new ActionContext { SourcePlayer = state.CurrentPlayer }, mainAction);

		// Assert
		Assert.AreEqual(1, state.History.Count);
		Assert.AreEqual(mainAction, state.History[0].Action);
	}

	[TestMethod]
	public void OnTurnEndPostTriggers_ResolveBeforeOpponentStartTurn()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;

		var minionCard = new MinionCard("Test", 1, 1, 1);
		minionCard.Owner = current;
		var minion = new Minion(minionCard, current);
		current.Board.Add(minion);

		// Arrange: temporary buff that expires on OnTurnEnd/Post, same pattern the
		// engine uses for "until end of turn" effects (see AbilityTest.BattleCry_BuffMinion)
		engine.Resolve(state, new ActionContext { Targets = [minion] }, new AddStatModifierAction()
		{
			AttackChange = (Value)2,
			ExpirationTrigger = new ExpirationTrigger()
			{
				EffectTrigger = EffectTrigger.OnTurnEnd,
				EffectTiming = EffectTiming.Post,
			}
		});
		Assert.AreEqual(3, minion.Attack);

		int endingTurn = state.turn;

		// Act
		engine.Resolve(state, new ActionContext { SourcePlayer = current }, new EndTurnAction());

		// Assert: the buff expired...
		Assert.AreEqual(1, minion.Attack);

		// ...and it expired as part of the turn that just ended, before the opponent's
		// StartTurnAction cascade (mana/draw) ran - not after it.
		int removeModifierIndex = state.History.FindIndex(h => h.Action is RemoveModifierAction);
		int startTurnIndex = state.History.FindIndex(h => h.Action is StartTurnAction);

		Assert.AreNotEqual(-1, removeModifierIndex, "Expected the modifier expiration to be recorded in history");
		Assert.AreNotEqual(-1, startTurnIndex, "Expected the opponent's StartTurnAction to be recorded in history");
		Assert.IsTrue(removeModifierIndex < startTurnIndex,
			"OnTurnEnd Post-trigger should resolve before the opponent's StartTurnAction");
		Assert.AreEqual(endingTurn, state.History[removeModifierIndex].Turn,
			"Modifier expiration should be tagged with the turn that just ended, not the new turn");
	}
}
