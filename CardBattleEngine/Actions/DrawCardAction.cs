namespace CardBattleEngine;

public class DrawCardAction : IGameAction
{
	private readonly Player _player;

	public DrawCardAction(Player player) => _player = player;

	public bool IsValid(GameState state) => _player.Deck.Count > 0;

	public IEnumerable<IGameAction> Resolve(GameState state, Player current, Player opponent)
	{
		var card = _player.Deck[0];
		_player.Deck.RemoveAt(0);
		_player.Hand.Add(card);

		// Could return further side effects (triggers)
		return [];
	}
}