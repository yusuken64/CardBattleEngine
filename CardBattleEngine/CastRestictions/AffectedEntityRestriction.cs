namespace CardBattleEngine;

public class AffectedEntityRestriction : ICastRestriction
{
	public IAffectedEntitySelector IAffectedEntitySelector { get; set; }
	public bool CanPlay(GameState gameState, Player player, Card castingCard, out string reason)
	{
		var affected = IAffectedEntitySelector.Select(gameState, new ActionContext()
		{
			SourcePlayer = player,
		});

		if (affected.Count() == 0)
		{
			reason = "Invalid Cast";
			return false;
		}

		reason = "";
		return true;
	}
}