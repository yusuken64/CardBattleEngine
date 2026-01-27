namespace CardBattleEngine;

public class KeywordSelector : IValidTargetSelector
{
	public Keyword Keyword { get; set; }
	public bool HasKeyword { get; set; }

	public List<IGameEntity> Select(GameState gameState, Player player, Card castingCard)
	{
		return gameState.GetAllMinions()
			.Where(m => m.Matches(Keyword, HasKeyword))
			.Cast<IGameEntity>()
			.ToList();
	}
}