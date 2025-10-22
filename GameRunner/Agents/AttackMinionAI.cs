using CardBattleEngine;

public class AttackMinionAI : IGameAgent
{
	private readonly Player _player;

	public AttackMinionAI(Player player)
	{
		_player = player;
	}

	public IGameAction GetNextAction(GameState game)
	{
		var validActions = game.GetValidActions(_player);
		return validActions.Skip(1).FirstOrDefault();
	}
	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}
