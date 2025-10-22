using CardBattleEngine;
using System.Text.Json.Serialization;

public interface IGameAgent
{
	public IGameAction GetNextAction(GameState game);

	public void OnGameEnd(GameState gamestate, bool win);
}

public class LearningAgent : IGameAgent
{
	private MonteCarloModel _model;
	private List<(GameState state, IGameAction action)> _history;

	public LearningAgent(MonteCarloModel model)
	{
		_model = model;
		_history = new();
	}

	public IGameAction GetNextAction(GameState game)
	{
		List<IGameAction> actions = game.GetValidActions(game.CurrentPlayer);
		var action = _model.ChooseAction(game, actions);
		_history.Add((game, action));

		return action;
	}

	public void OnGameEnd(GameState gameState, bool win)
	{
		_model.UpdateFromResult(_history, win);
	}
}