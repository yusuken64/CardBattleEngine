namespace CardBattleEngine;

//the played card targeting must have a valid choice
public class ValidTargetRestriction : ICastRestriction
{
	public bool CanPlay(
		GameState gameState,
		Player player,
		Card castingCard,
		out string reason)
	{
		reason = "";

		// No primary effect nothing to validate
		IValidTargetSelector selector = castingCard.ValidTargetSelector;
		if (selector == null)
		{
			return true;
		}

		// There must exist at least one valid target
		var validTargets = selector.Select(gameState, player, castingCard);
		if (!validTargets.Any())
		{
			reason = "No valid targets";
			return false;
		}

		return true;
	}
}
