namespace CardBattleEngine;

public class ManaCostRestriction : ICastRestriction
{
	public bool CanPlay(GameState gameState, Player player, Card castingCard, out string reason)
	{
		if (player.Mana < castingCard.ManaCost)
		{
			reason = "Not enough mana";
			return false;
		}

		reason = string.Empty;
		return true;
	}
}
