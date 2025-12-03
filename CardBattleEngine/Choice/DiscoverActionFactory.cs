namespace CardBattleEngine;

public class DiscoverActionFactory : IActionFactory
{
	public DiscoverAction DiscoverAction;
	public IEnumerable<(IGameAction, ActionContext)> GetActions(Player sourcePlayer, IEnumerable<Card> list)
	{
		foreach (var item in list) {
			switch (DiscoverAction)
			{
				case DiscoverAction.DrawTargetFromDeck:
					yield return (
						new DrawTargetCardFromDeckAction() { },
						new ActionContext()
						{
							SourcePlayer = sourcePlayer,
							Target = item
						});
					break;
				case DiscoverAction.Gain:
					yield return (
						new GainCardAction 
						{
							Card = item.Clone(),
						},
						new ActionContext()
						{
							SourcePlayer = sourcePlayer
						});
					break;
				case DiscoverAction.Summon:
					yield return (
						new SummonMinionAction
						{
							Card = item as MinionCard
						},
						new ActionContext()
						{
							SourcePlayer = sourcePlayer
						});
					break;
			}
		}
	}
}
