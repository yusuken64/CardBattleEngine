using CardBattleEngine;
using System.Text.Json.Serialization;

public interface IGameAgent
{
	public GameActionBase GetNextAction(GameState game);

	public void OnGameEnd(GameState gamestate, bool win);
}

public class OracleAgent : IGameAgent
{
	private readonly GameEngine _engine;
	private readonly OracleBrain _brain;

	public OracleAgent(GameEngine engine, OracleBrain brain)
	{
		this._engine = engine;
		_brain = brain;
	}

	public GameActionBase GetNextAction(GameState game)
	{
		var actions = game.GetValidActions(game.CurrentPlayer).ToList();
		if (actions.Count == 0) return new EndTurnAction();

		float bestScore = float.NegativeInfinity;
		GameActionBase bestAction = null;

		foreach (var action in actions)
		{
			float score = _brain.Evaluate(_engine, game, (GameActionBase)action.Item1);
			if (score > bestScore)
			{
				bestScore = score;
				bestAction = (GameActionBase)action.Item1;
			}
		}

		return bestAction ?? new EndTurnAction();
	}

	public void OnGameEnd(GameState gamestate, bool win)
	{
	}

	public class OracleBrain
	{
		public float PreserveHealthWeight { get; set; } = 1.0f;
		public float BoardControlWeight { get; set; } = 1.0f;
		public float AggressionWeight { get; set; } = 1.0f;
		public float CardConservationWeight { get; set; } = 1.0f;

		public float Evaluate(GameEngine engine, GameState state, GameActionBase action)
		{
			Player player = state.CurrentPlayer;
			Player opponent = state.OpponentOf(player);

			GameEngine engineCopy = engine.Clone();
			GameState simulated = state.Clone();

			// Get valid actions in the cloned state
			var validActionsInClone = simulated.GetValidActions(simulated.CurrentPlayer);

			// Find the corresponding cloned action
			var clonedAction = validActionsInClone
				.First(a => a.ToString() == action.ToString());

			Player simulatedPlayer = simulated.CurrentPlayer;
			Player simulatedOpponent = simulated.OpponentOf(simulatedPlayer);
			//engineCopy.Resolve(simulated, simulatedPlayer, simulatedOpponent, clonedAction);

			float score = 0;

			// Example heuristics
			score += PreserveHealthWeight * (simulatedPlayer.Health - player.Health);
			score += BoardControlWeight * (simulated.GetBoardStrength(simulatedPlayer) - state.GetBoardStrength(player));
			score += AggressionWeight * (simulatedOpponent.Health - simulatedOpponent.Health);
			score -= CardConservationWeight * (simulatedPlayer.Hand.Count - player.Hand.Count);

			return score;
		}
	}
}