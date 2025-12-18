namespace CardBattleEngine;

public class AcquireWeaponAction : GameActionBase
{
	public Weapon Weapon { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext context, out string reason)
	{
		reason = null;
		return context.Target is Player player;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is not Player player)
			yield break;

		// If there's a weapon equipped, destroy it first
		if (player.EquippedWeapon != null)
		{
			yield return (new DestroyWeaponAction { },
				new ActionContext { Source = player, Target = player });
		}

		// After destruction resolves (deathrattle etc.), engine continues
		yield return (new EquipWeaponAction { Weapon = Weapon },
			new ActionContext { Source = context.Source, Target = player });
	}
}
