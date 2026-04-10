using CardBattleEngine.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardBattleEngine.Test;

[TestClass]
public class AITest
{
	[TestMethod]
	public void EncodingTest()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		const int expectedVectorLength = 256;

		// Act & Assert while the game runs
		while (!state.IsGameOver())
		{
			// Encode the state
			var vector = GameStateVectorizer.ToVector(state);

			// --- Basic sanity checks ---
			Assert.AreEqual(expectedVectorLength, vector.Length, "State vector length mismatch");

			// Hero health should be 0..1
			Assert.IsTrue(vector[0] >= 0f && vector[0] <= 1f, "Current hero health out of range");
			Assert.IsTrue(vector[1] >= 0f && vector[1] <= 1f, "Opponent hero health out of range");

			// Mana checks
			Assert.IsTrue(vector[2] >= 0f && vector[2] <= 1f, "Current mana out of range");
			Assert.IsTrue(vector[3] >= 0f && vector[3] <= 1f, "Current max mana out of range");

			// Board and hand slots: check exists flags (0 or 1)
			for (int i = 4; i < 256; i += 6) // minion slots
			{
				Assert.IsTrue(vector[i] == 0f || vector[i] == 1f, $"Minion existence flag at index {i} invalid");
			}

			// Choose first valid action
			var actions = state.GetValidActions(state.CurrentPlayer);
			if (actions.Count == 0)
				break;

			var firstAction = actions[0];
			engine.Resolve(state, firstAction.Item2, firstAction.Item1);
		}
	}

	[TestMethod]
	public void ActionsToPolicyTest()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var current = state.CurrentPlayer;

		const int policySize = 1 << 16;

		while (!state.IsGameOver())
		{
			var validActions = state.GetValidActions(state.CurrentPlayer);
			var actionProbs = new Dictionary<int, float>();

			for (int i = 0; i < validActions.Count; i++)
			{
				int idx = GameStateVectorizer.EncodeAction(validActions[i], state);
				actionProbs[idx] = 1f; // simple uniform probability
			}

			// Act
			var policy = GameStateVectorizer.ActionsToPolicy(state, actionProbs);

			// --- Assertions ---

			// 1. Policy vector has correct length
			Assert.AreEqual(policySize, policy.Length, "Policy vector length mismatch");

			// 2. All probabilities are between 0 and 1
			foreach (var p in policy)
			{
				Assert.IsTrue(p >= 0f && p <= 1f, "Policy value out of range 0..1");
			}

			// 3. Probabilities for invalid actions should be 0
			foreach (var action in validActions)
			{
				int idx = GameStateVectorizer.EncodeAction(action, state);
				Assert.IsTrue(policy[idx] > 0f, $"Valid action index {idx} has zero probability");
			}

			// 4. Sum of policy should be 1
			float sum = policy.Sum();
			Assert.IsTrue(Math.Abs(sum - 1f) < 1e-6, $"Policy not normalized, sum={sum}");

			var firstAction = validActions[0];
			engine.Resolve(state, firstAction.Item2, firstAction.Item1);
		}
	}

	[TestMethod]
	public void ActionsEncodeDecodeTest()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var current = state.CurrentPlayer;
		while (!state.IsGameOver())
		{
			var validActions = state.GetValidActions(state.CurrentPlayer);

			foreach (var action in validActions)
			{
				// Encode
				int encodedAction = GameStateVectorizer.EncodeAction(action, state);

				// Decode
				var decodedAction = GameStateVectorizer.DecodeAction(encodedAction, state);

				// --- Assertions ---

				// 1. Action types match
				Assert.AreEqual(action.Item1.GetType(), decodedAction.Item1.GetType(),
					$"Action type mismatch: {action.Item1.GetType()} vs {decodedAction.Item1.GetType()}");

				if (action.Item1 is PlayCardAction play)
				{
					Assert.AreEqual(play.Card, ((PlayCardAction)decodedAction.Item1).Card,
						"PlayCard card mismatch");
				}

				Assert.AreEqual(action.Item2.Source, decodedAction.Item2.Source,
					"Action source mismatch");
				// 3. Target entity matches (nullable)
				Assert.AreEqual(action.Item2.Target, decodedAction.Item2.Target,
					"Action target mismatch");
			}

			var firstAction = validActions[0];
			engine.Resolve(state, firstAction.Item2, firstAction.Item1);
		}
	}

	[TestMethod]
	public void Decode_AllIndices_DoesNotCrash()
	{
		var state = GameFactory.CreateTestGame();

		for (int i = 0; i < 256; i++)
		{
			try
			{
				var action = GameStateVectorizer.DecodeAction(i, state);
				var isValid = state.GetValidActions(state.CurrentPlayer)
					.Any(a => GameStateVectorizer.EncodeAction(a, state) == i);
			}
			catch (Exception ex)
			{
				Assert.Fail($"Index {i} crashed: {ex.Message}");
			}
		}
	}
}
