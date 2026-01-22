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
}
