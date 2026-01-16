namespace CardBattleEngine;

public class WeaponValue : Value
{
	public TeamRelationship Side { get; set; }
	public override int GetValue(GameState state, ActionContext context)
	{
		switch (Side)
		{
			case TeamRelationship.Friendly:
				return context.SourcePlayer.EquippedWeapon?.Attack ?? 0;

			case TeamRelationship.Enemy:
				return state.OpponentOf(context.SourcePlayer).EquippedWeapon?.Attack ?? 0;

			case TeamRelationship.Any:
				return context.SourcePlayer.EquippedWeapon?.Attack ?? 0;

			default:
				return 0;
		}
	}
}