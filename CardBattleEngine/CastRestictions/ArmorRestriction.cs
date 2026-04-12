namespace CardBattleEngine;

public class ArmorRestriction : ICastRestriction
{
	public bool CanPlay(GameState gameState, Player player, Card castingCard, out string reason)
	{
		if (player.Armor <= 0)
		{
			reason = "Need Armor";
			return false;
		}

		reason = string.Empty;
		return true;
	}
}
