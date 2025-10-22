namespace CardBattleEngine;

internal class PlayCardAction : IGameAction
{
	private readonly Card _card;

	public PlayCardAction(Card card)
	{
		_card = card;
	}

	public bool IsValid(GameState state)
	{
		var player = _card.Owner;
		if (!player.Hand.Contains(_card))
			return false;

		if (_card.ManaCost > player.Mana)
			return false;

		if (_card is MinionCard minionCard && player.Board.Count >= state.MaxBoardSize)
			return false;

		return true;
	}

	public IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		if (!IsValid(state))
			return [];

		_card.Owner.Mana -= _card.ManaCost;
		_card.Owner.Hand.Remove(_card);

		var effects = _card.GetPlayEffects(state, currentPlayer, opponent).ToList();

		return effects;
	}

	public override string ToString()
	{
		if (_card is MinionCard minionCard)
		{
			return $"Playcard {_card.Name} ({_card.ManaCost}){minionCard.Attack}/{minionCard.Health}";
		}

		return base.ToString();
	}
}