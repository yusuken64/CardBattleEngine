namespace CardBattleEngine;

public class DeckProvider : IDiscoverSourceProvider
{
	public IEnumerable<Card> GetItems(GameState gameState, Player sourcePlayer)
	{
		return sourcePlayer.Deck;
	}
}
