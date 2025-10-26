using CardBattleEngine;

public class AttackMinionAI : IGameAgent
{
	private readonly Player _player;

	public AttackMinionAI(Player player)
	{
		_player = player;
	}

	public GameActionBase GetNextAction(GameState game)
	{
		var validActions = game.GetValidActions(_player);
		return (GameActionBase)validActions.Skip(1).FirstOrDefault().Item1;
	}
	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}
