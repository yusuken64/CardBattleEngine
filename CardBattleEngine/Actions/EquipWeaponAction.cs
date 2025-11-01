
namespace CardBattleEngine;

internal class EquipWeaponAction : GameActionBase 
{
	public Weapon Weapon { get; set; }

	public override EffectTrigger EffectTrigger => EffectTrigger.EquipWeapon;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
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