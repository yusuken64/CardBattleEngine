namespace CardBattleEngine;

public class TribeFilter : ICardFilter
{
	public string Tribe;
	public Func<Card, bool> GetFilter()
	{
		return (card) =>
		{
			if (card is MinionCard minionCard)
			{
				return TribeUtils.Matches(minionCard.MinionTribes, Tribe);
			}
			return false;
		};
	}
}