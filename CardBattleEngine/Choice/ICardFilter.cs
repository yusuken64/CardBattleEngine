namespace CardBattleEngine;

public interface ICardFilter
{
	Func<Card, bool> GetFilter();
}
