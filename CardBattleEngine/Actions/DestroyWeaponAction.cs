namespace CardBattleEngine;

public class DestroyWeaponAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		if (context.Target is Player player)
		{
			return player.EquippedWeapon != null;
		}

		return false;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is Player player)
		{
			player.EquippedWeapon = null;
			//TODO weapon deathrattle;
		}

		return [];
	}
}
