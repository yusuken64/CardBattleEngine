namespace CardBattleEngine;

public class CardDBProvider : IDiscoverSourceProvider
{
	public IEnumerable<Card> GetItems(GameState gameState, Player sourcePlayer)
	{
		return gameState.CardDB;
	}
}
