namespace CardBattleEngine;

public class DiscoverChoice : IPendingChoice
{
	public Player SourcePlayer { get; set; }
	public IDiscoverSourceProvider SourceProvider { get; set; }
	public IEnumerable<ICardFilter> Filters { get; set; }
	public IActionFactory ActionFactory { get; set; }
	public int OptionCount { get; set; } = 3;
	public IEnumerable<(IGameAction, ActionContext)> Options { get; set; }

	public IEnumerable<(IGameAction, ActionContext)> GetActions(GameState gameState)
	{
		if (Options != null) { return Options; }

		IEnumerable<Card> items = SourceProvider.GetItems(gameState, SourcePlayer);

		if (Filters != null)
		{
			foreach (var filter in Filters)
				items = items.Where(filter.GetFilter());
		}

		var shuffled = items.ToList();
		gameState.Shuffle(shuffled);
		var list = shuffled.Take(OptionCount);

		Options = ActionFactory.GetActions(SourcePlayer, list).ToList();
		return Options;
	}
}
