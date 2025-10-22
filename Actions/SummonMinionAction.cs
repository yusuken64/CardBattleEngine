namespace CardBattleEngine;

internal class SummonMinionAction : IGameAction
{
	private readonly MinionCard _card;
	private readonly Player _owner;

	public SummonMinionAction(MinionCard card, Player owner)
	{
		_card = card;
		_owner = owner;
	}

	public bool IsValid(GameState state)
	{
		// Board not full
		if (_owner.Board.Count >= state.MaxBoardSize)
			return false;

		return true;
	}

	public IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		if (!IsValid(state))
			return [];

		// Create minion entity
		var minion = new Minion(_card, _owner);
		_owner.Board.Add(minion);

		var sideEffects = new List<IGameAction>
		{
			new MinionSummonedEventAction(minion)
		};

		return sideEffects;
	}
}
