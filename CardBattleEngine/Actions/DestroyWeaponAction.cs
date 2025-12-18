namespace CardBattleEngine;

public class DestroyWeaponAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		if (context.Target is Player player)
		{
			reason = null;
			return player.EquippedWeapon != null;
		}

		reason = null;
		return false;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is not Player player)
			yield break;

		var weapon = player.EquippedWeapon;
		if (weapon == null || weapon.TriggeredEffects == null)
			yield break;

		// Trigger deathrattle effects
		foreach (var effect in weapon.TriggeredEffects
			.Where(e => e.EffectTrigger == EffectTrigger.Deathrattle))
		{
			foreach (var gameAction in effect.GameActions)
			{
				// Build a selector context for the effect
				var selectorContext = new ActionContext
				{
					SourcePlayer = weapon.Owner,
					Source = weapon.Owner,
				};

				// Get the targets for this effect
				IEnumerable<IGameEntity> targets;
				if (effect.AffectedEntitySelector != null)
				{
					targets = effect.AffectedEntitySelector.Select(state, selectorContext);
				}
				else
				{
					targets = [weapon.Owner];
				}

				foreach (var target in targets)
				{
					yield return (gameAction, new ActionContext
					{
						SourcePlayer = weapon.Owner,
						Source = weapon.Owner,
						Target = target
					});
				}
			}
		}

		// Unequip weapon after triggering effects
		player.UnequipWeapon();
	}
}
