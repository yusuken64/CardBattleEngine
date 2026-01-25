namespace CardBattleEngine;

public class WeaponCastRestriction : ICastRestriction
{
	public TeamRelationship TeamRelationship { get; set; }

	public bool CanPlay(GameState gameState, Player player, Card castingCard, out string reason)
	{
		switch (TeamRelationship)
		{
			case TeamRelationship.Friendly:
				if (player.EquippedWeapon == null)
				{
					reason = "Requires Weapon";
					return false;
				}
				break;
			case TeamRelationship.Enemy:
				if (gameState.OpponentOf(player).EquippedWeapon == null)
				{
					reason = "Requires Opponent Weapon";
					return false;
				}
				break;
			case TeamRelationship.Any:
				//fall through to default case
				break;
		}

		reason = "";
		return true;
	}
}
