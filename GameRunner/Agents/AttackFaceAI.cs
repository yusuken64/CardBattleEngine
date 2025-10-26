using CardBattleEngine;

public class AttackFaceAI : IGameAgent
{
	private readonly Player _player;

	public AttackFaceAI(Player player)
	{
		_player = player;
	}

	public GameActionBase GetNextAction(GameState game)
	{
		var validActions = game.GetValidActions(_player);
		return (GameActionBase)validActions[0].Item1;
	}

	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}
