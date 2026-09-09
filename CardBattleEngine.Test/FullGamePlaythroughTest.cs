namespace CardBattleEngine.Test;

// Drives full games through the same GetValidActions -> GameEngine.Resolve pipeline the console
// HumanAgent uses, to catch regressions like the mulligan-submission bug (HumanAgent was replacing
// the correctly-populated ActionContext from GetValidActions with an empty one, which made
// IsAllowedChoice reject the SubmitMulliganAction). Console.ReadKey can't be scripted from a test,
// so this substitutes deterministic/random selection for the interactive prompt.
[TestClass]
public class FullGamePlaythroughTest
{
	[TestMethod]
	public void FirstOption_CanCompleteFullGame()
	{
		RunGames(seedCount: 25, pickFirst: true);
	}

	[TestMethod]
	public void RandomOption_CanCompleteFullGame()
	{
		RunGames(seedCount: 50, pickFirst: false);
	}

	private void RunGames(int seedCount, bool pickFirst)
	{
		var actionTypesSeen = new HashSet<string>();
		int completed = 0;

		for (int seed = 0; seed < seedCount; seed++)
		{
			var gameState = GameFactory.CreateTestGame();
			var engine = new GameEngine();
			var rng = new XorShiftRNG((ulong)(seed + 1));

			engine.StartGame(gameState);

			int steps = 0;
			const int maxSteps = 2000;

			while (!gameState.IsGameOver() && steps < maxSteps)
			{
				steps++;
				var player = gameState.PendingChoice?.SourcePlayer ?? gameState.CurrentPlayer;
				var actions = gameState.GetValidActions(player);

				Assert.IsTrue(actions.Count > 0, $"seed={seed} step={steps}: no valid actions available (stuck).");

				(IGameAction action, ActionContext context) chosen = pickFirst
					? actions[0]
					: actions[rng.NextInt(0, actions.Count)];

				actionTypesSeen.Add(chosen.action.GetType().Name);

				engine.Resolve(gameState, chosen.context, chosen.action);
			}

			Assert.IsTrue(gameState.IsGameOver(), $"seed={seed}: game did not finish within {maxSteps} steps (likely stuck on a pending choice).");
			completed++;
		}

		Console.WriteLine($"Completed {completed}/{seedCount} games (pickFirst={pickFirst}).");
		Console.WriteLine("Action types exercised: " + string.Join(", ", actionTypesSeen.OrderBy(x => x)));

		Assert.IsTrue(actionTypesSeen.Contains("SubmitMulliganAction"));
		Assert.IsTrue(actionTypesSeen.Contains("EndTurnAction"));
		Assert.IsTrue(actionTypesSeen.Contains("PlayCardAction"));
	}
}
