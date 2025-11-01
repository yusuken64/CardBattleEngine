
using System.Net.Mime;

namespace CardBattleEngine;

public class EquipWeaponAction : GameActionBase
{
	public Weapon Weapon { get; set; }

	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return context.Target is Player;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		var sideEffects = new List<(IGameAction, ActionContext)>();
		if (context.Target is Player player)
		{
			//if (player.EquippedWeapon != null)
			//{
			//	sideEffects.Add((
			//		new DestroyWeaponAction(),
			//		new ActionContext()
			//		{
			//			Source = player,
			//			Target = player.EquippedWeapon
			//		}));
			//}

			player.EquipWeapon(Weapon);
		}

		return sideEffects;
	}
}
