using CardBattleEngine;

public class RandomAI : IGameAgent
{
	private readonly Player _player;
	private readonly IRNG _rng;

	public RandomAI(Player player, IRNG rng)
	{
		_player = player;
		_rng = rng;
	}

	public GameActionBase GetNextAction(GameState game)
	{
		var validActions = game.GetValidActions(_player);

		return ChooseRandom(validActions);
	}
	public T ChooseRandom<T>(IReadOnlyList<T> options)
	{
		if (options.Count == 0) return default!;
		return options[_rng.NextInt(0, options.Count)];
	}
	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}
