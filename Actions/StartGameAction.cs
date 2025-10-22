namespace CardBattleEngine;

internal class StartGameAction : IGameAction
{
	private Player _currentPlayer;
	private Action<IList<Card>> _shuffleFunction;

	public StartGameAction(Player currentPlayer, Action<IList<Card>> shuffleFunction)
	{
		this._currentPlayer = currentPlayer;
		this._shuffleFunction = shuffleFunction;
	}

	public bool IsValid(GameState state) => true;

	public IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		_shuffleFunction(_currentPlayer.Deck);

		return
		[
			new DrawCardAction(_currentPlayer),
			new DrawCardAction(_currentPlayer),
			new DrawCardAction(_currentPlayer),
		];
	}
}