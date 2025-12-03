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
				return minionCard.MinionTribes.Select(x => x.ToString()).Contains(Tribe);
			}
			return false;
		};
	}
}