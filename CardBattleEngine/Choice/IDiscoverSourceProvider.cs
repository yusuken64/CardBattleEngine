namespace CardBattleEngine;

public interface IDiscoverSourceProvider
{
	IEnumerable<Card> GetItems(GameState gameState, Player sourcePlayer);
}
