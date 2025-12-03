namespace CardBattleEngine;

public interface IPendingChoice
{
	public Player SourcePlayer { get; set; }
	public IEnumerable<(IGameAction, ActionContext)> Options { get; set; }

	public IEnumerable<(IGameAction, ActionContext)> GetActions(GameState gameState);
}

public class SimpleChoice : IPendingChoice
{
	public IEnumerable<(IGameAction, ActionContext)> Options { get; set; }
	public Player SourcePlayer { get; set; }

	IEnumerable<(IGameAction, ActionContext)> IPendingChoice.GetActions(GameState gameState)
	{
		return Options;
	}
}
