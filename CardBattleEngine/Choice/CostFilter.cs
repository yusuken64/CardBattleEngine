namespace CardBattleEngine;

public class CostFilter : ICardFilter
{
	public int Cost;
	public Func<Card, bool> GetFilter()
	{
		return (card) => card.ManaCost == Cost;
	}
}
