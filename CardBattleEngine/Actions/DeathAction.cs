namespace CardBattleEngine;

internal class DeathAction : IGameAction
{
	private readonly IGameEntity _target;

	public DeathAction(IGameEntity target)
	{
		_target = target;
	}

	public bool IsValid(GameState state)
	{
		return _target.IsAlive;
	}

	public IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		var sideEffects = new List<IGameAction>();
		_target.IsAlive = false;
		if (_target is Minion minion)
		{
			minion.Owner.Board.Remove(minion);
			minion.Owner.Graveyard.Add(minion);
		}
		return sideEffects;
	}
}