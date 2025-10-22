using CardBattleEngine;

public class AttackFaceAI : IGameAgent
{
	private readonly Player _player;

	public AttackFaceAI(Player player)
	{
		_player = player;
	}

	public IGameAction GetNextAction(GameState game)
	{
		var validActions = game.GetValidActions(_player);
		return validActions[0];
	}

	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}
