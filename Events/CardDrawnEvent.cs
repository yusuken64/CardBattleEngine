namespace CardBattleEngine;

public class CardDrawnEvent : IGameEvent
{
	public Player Player;
	public Card Card;
	public CardDrawnEvent(Player p, Card c)
	{
		Player = p; Card = c;
	}
}
