namespace CardBattleEngine;

internal class StartGameAction : GameActionBase
{
	private Player _currentPlayer;
	private Action<IList<Card>> _shuffleFunction;

	public StartGameAction(Player currentPlayer, Action<IList<Card>> shuffleFunction)
	{
		this._currentPlayer = currentPlayer;
		this._shuffleFunction = shuffleFunction;
	}

	public override EffectTrigger EffectTrigger => EffectTrigger.GameStart;

	public override bool IsValid(GameState state) => true;

	public override IEnumerable<GameActionBase> Resolve(GameState state, Player currentPlayer, Player opponent)
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