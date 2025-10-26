namespace CardBattleEngine;

internal class SummonMinionAction : GameActionBase
{
	private readonly MinionCard _card;
	private readonly Player _owner;

	public SummonMinionAction(MinionCard card, Player owner)
	{
		_card = card;
		_owner = owner;
	}

	public override EffectTrigger EffectTrigger => EffectTrigger.SummonMinion;

	public override bool IsValid(GameState state)
	{
		// Board not full
		if (_owner.Board.Count >= state.MaxBoardSize)
			return false;

		return true;
	}

	public override IEnumerable<GameActionBase> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		if (!IsValid(state))
			return [];

		// Create minion entity
		var minion = new Minion(_card, _owner);
		minion.Name = _card.Name;
		_owner.Board.Add(minion);

		return [];
	}
}
