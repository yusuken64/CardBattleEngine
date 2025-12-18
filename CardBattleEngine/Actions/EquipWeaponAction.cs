
namespace CardBattleEngine;

public class EquipWeaponAction : GameActionBase 
{
	public Weapon Weapon { get; set; }

	public override EffectTrigger EffectTrigger => EffectTrigger.EquipWeapon;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is Player player)
		{
			player.EquipWeapon(Weapon);
		}

		return [];
	}
}