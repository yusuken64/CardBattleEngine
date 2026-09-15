using CardBattleEngine.AI;

namespace CardBattleEngine.Test;

/// <summary>
/// Regression tests for the policy-space action encoding.
///
/// BACKGROUND — the bug these exist to prevent:
/// The original layout packed four 4-bit fields and used -1 as the
/// "not applicable" sentinel. Because (-1 &amp; 0b1111) == 15, and 15 was
/// also a legitimate target index (friendly board slot 7, encoded as
/// 9 + 6), an untargeted action and a play targeting your own seventh
/// minion produced an IDENTICAL index. Training labels would silently
/// merge the two, and nothing in the loss curve would show it.
///
/// The current layout reserves 0 as the sentinel in every index field
/// and widens TARGET to 5 bits. Total width is still 16 bits, so
/// PolicySize stays at 65536.
///
/// CHOICE ACTIONS: a pending choice (Discover, Choose One, target
/// prompt) encodes as TYPE = Choice with the option's index in TARGET.
/// Option identity is deliberately NOT encoded — see the design note
/// above EncodeChoice for why, and what it costs the policy head.
/// </summary>
[TestClass]
public class GameStateVectorizerTests
{
	// Mirrors the private layout in GameStateVectorizer. If you change the
	// layout there, change it here — that is the point of the duplication.
	const int TARGET_SHIFT = 0, TARGET_BITS = 5;
	const int SOURCE_SHIFT = 5, SOURCE_BITS = 4;
	const int HAND_SHIFT = 9, HAND_BITS = 4;
	const int TYPE_SHIFT = 13, TYPE_BITS = 3;

	static int Pack(int type, int hand, int source, int target) =>
		((type & ((1 << TYPE_BITS) - 1)) << TYPE_SHIFT) |
		((hand & ((1 << HAND_BITS) - 1)) << HAND_SHIFT) |
		((source & ((1 << SOURCE_BITS) - 1)) << SOURCE_SHIFT) |
		((target & ((1 << TARGET_BITS) - 1)) << TARGET_SHIFT);

	/// <summary>Every (type, hand, source, target) tuple the engine can produce.</summary>
	static IEnumerable<(int t, int h, int s, int g)> ReachableDomain()
	{
		// EndTurn / Attack / PlayCard / HeroPower — full cross product.
		for (int type = 0; type <= (int)ActionType.HeroPower; type++)
		for (int hand = 0; hand <= GameStateVectorizer.MaxHandSize; hand++)          // 0 = none
		for (int source = 0; source <= 1 + GameStateVectorizer.MaxMinions; source++) // 0 = none
		for (int target = 0; target <= 2 + 2 * GameStateVectorizer.MaxMinions; target++)
			yield return (type, hand, source, target);

		// Choice — hand and source unused; target carries the option index.
		for (int option = 1; option <= GameStateVectorizer.MaxChoiceOptions; option++)
			yield return ((int)ActionType.Choice, 0, 0, option);
	}

	/// <summary>
	/// Exhaustive: no two distinct tuples in the reachable domain may share an
	/// index. This is the test that would have caught the original aliasing bug.
	/// </summary>
	[TestMethod]
	public void ActionIndices_AreUniqueAcrossEntireDomain()
	{
		var seen = new Dictionary<int, (int, int, int, int)>();

		foreach (var tuple in ReachableDomain())
		{
			int idx = Pack(tuple.t, tuple.h, tuple.s, tuple.g);

			Assert.IsFalse(
				seen.ContainsKey(idx),
				$"Index collision at {idx}: {seen.GetValueOrDefault(idx)} vs {tuple}");

			seen[idx] = tuple;
		}

		Assert.AreEqual(6763, seen.Count, "Reachable action-space size changed unexpectedly");
	}

	/// <summary>Every reachable index must fit the declared policy vector.</summary>
	[TestMethod]
	public void AllActionIndices_FitWithinPolicySize()
	{
		int maxIndex = ReachableDomain().Max(t => Pack(t.t, t.h, t.s, t.g));

		Assert.IsTrue(
			maxIndex < GameStateVectorizer.PolicySize,
			$"Max action index {maxIndex} exceeds PolicySize {GameStateVectorizer.PolicySize}");
	}

	/// <summary>
	/// The specific historical failure: "no target" must never encode to the
	/// same value as a real target. Pinned as its own test so the intent
	/// survives any future refactor of the layout.
	/// </summary>
	[TestMethod]
	public void NoTarget_DoesNotAliasAnyRealTarget()
	{
		int noTarget = Pack((int)ActionType.PlayCard, 1, 0, GameStateVectorizer.NONE);

		for (int target = 1; target <= 2 + 2 * GameStateVectorizer.MaxMinions; target++)
		{
			int withTarget = Pack((int)ActionType.PlayCard, 1, 0, target);
			Assert.AreNotEqual(
				noTarget, withTarget,
				$"Untargeted action aliases target index {target}");
		}
	}

	/// <summary>Choice indices must not collide with any ordinary action index.</summary>
	[TestMethod]
	public void ChoiceIndices_DoNotAliasOrdinaryActions()
	{
		var ordinary = new HashSet<int>(
			ReachableDomain()
				.Where(t => t.t != (int)ActionType.Choice)
				.Select(t => Pack(t.t, t.h, t.s, t.g)));

		for (int option = 1; option <= GameStateVectorizer.MaxChoiceOptions; option++)
		{
			int idx = Pack((int)ActionType.Choice, 0, 0, option);
			Assert.IsFalse(
				ordinary.Contains(idx),
				$"Choice option {option - 1} aliases an ordinary action index ({idx})");
		}
	}

	/// <summary>
	/// Round-trips every option of a synthetic pending choice. Uses SimpleChoice
	/// so the test doesn't depend on a card that happens to Discover.
	/// </summary>
	[TestMethod]
	public void ChoiceActions_RoundTrip()
	{
		var state = GameFactory.CreateTestGame();
		var player = state.CurrentPlayer;

		var options = new List<(IGameAction, ActionContext)>();
		for (int i = 0; i < 3; i++)
		{
			options.Add((
				new EndTurnAction(),
				new ActionContext { SourcePlayer = player, Source = player }));
		}

		state.PendingChoice = new SimpleChoice
		{
			SourcePlayer = player,
			Options = options
		};

		var legal = state.GetValidActions(player);
		Assert.AreEqual(options.Count, legal.Count,
			"A pending choice should be the only source of legal actions");

		var encoded = new HashSet<int>();

		for (int i = 0; i < legal.Count; i++)
		{
			int idx = GameStateVectorizer.EncodeAction(legal[i], state);

			Assert.IsTrue(encoded.Add(idx), $"Option {i} encoded to a duplicate index");

			var decoded = GameStateVectorizer.DecodeAction(idx, state);

			Assert.AreSame(
				legal[i].Item1, decoded.Item1,
				$"Option {i} did not round-trip to the same action instance");
		}
	}

	/// <summary>A Choice index is meaningless without a pending choice, and must say so.</summary>
	[TestMethod]
	public void DecodingChoice_WithoutPendingChoice_Throws()
	{
		var state = GameFactory.CreateTestGame();
		state.PendingChoice = null;

		int idx = Pack((int)ActionType.Choice, 0, 0, 1);

		Assert.ThrowsException<InvalidOperationException>(
			() => GameStateVectorizer.DecodeAction(idx, state));
	}

	/// <summary>
	/// Live round-trip against a real game: every legal action must encode and
	/// decode back to an equivalent action.
	/// </summary>
	[TestMethod]
	public void EncodeDecode_RoundTrips_OverRealGame()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		while (!state.IsGameOver())
		{
			var validActions = state.GetValidActions(state.CurrentPlayer);
			if (validActions.Count == 0) break;

			foreach (var action in validActions)
			{
				int encoded = GameStateVectorizer.EncodeAction(action, state);
				var decoded = GameStateVectorizer.DecodeAction(encoded, state);

				Assert.AreEqual(
					action.Item1.GetType(), decoded.Item1.GetType(),
					"Action type mismatch after round-trip");

				if (action.Item1 is PlayCardAction play)
					Assert.AreEqual(
						play.Card, ((PlayCardAction)decoded.Item1).Card,
						"PlayCard card mismatch after round-trip");

				Assert.AreEqual(
					action.Item2.Targets?.FirstOrDefault(),
					decoded.Item2.Targets?.FirstOrDefault(),
					"Target mismatch after round-trip");
			}

			var next = validActions[0];
			engine.Resolve(state, next.Item2, next.Item1);
		}
	}

	/// <summary>Policy output must be a normalised distribution over legal actions only.</summary>
	[TestMethod]
	public void ActionsToPolicy_NormalisesOverLegalActionsOnly()
	{
		var state = GameFactory.CreateTestGame();

		var validActions = state.GetValidActions(state.CurrentPlayer);
		var probs = new Dictionary<int, float>();

		foreach (var a in validActions)
			probs[GameStateVectorizer.EncodeAction(a, state)] = 1f;

		Assert.IsTrue(probs.Count > 0, "Opening state produced no encodable actions");

		var policy = GameStateVectorizer.ActionsToPolicy(state, probs);

		Assert.AreEqual(GameStateVectorizer.PolicySize, policy.Length);
		Assert.AreEqual(1f, policy.Sum(), 1e-5f, "Policy is not normalised");

		foreach (var key in probs.Keys)
			Assert.IsTrue(policy[key] > 0f, $"Legal action {key} has zero probability");
	}
}
