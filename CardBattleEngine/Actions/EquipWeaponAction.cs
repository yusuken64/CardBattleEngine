
namespace CardBattleEngine;

public class EquipWeaponAction : GameActionBase 
{
	public Weapon Weapon { get; set; }

	public override EffectTrigger EffectTrigger => EffectTrigger.EquipWeapon;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		if (!context.AuthorizedToEquipWeapon)
		{
			throw new InvalidOperationException(
				"EquipWeaponAction must be issued via AcquireWeaponAction, not yielded directly.");
		}

		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Targets?.FirstOrDefault() is Player player)
		{
			player.EquipWeapon(Weapon);
		}

		if (Weapon.TriggeredEffects != null)
		{
			foreach (var effect in Weapon.TriggeredEffects.Where(x => x.EffectTrigger == EffectTrigger.Battlecry))
			{
				IEnumerable<IGameEntity> targets;
				if (effect.AffectedEntitySelector != null)
				{
					targets = effect.AffectedEntitySelector.Select(state, context);
				}
				else
				{
					targets = context.Targets is { Count: > 0 } ? context.Targets : [null];
				}

				foreach (var target in targets)
				{
					var effectContext = new ActionContext
					{
						SourceCard = null,
						Source = context.SummonedMinion,
						SourcePlayer = context.SourcePlayer,
						Targets = [target],
					};

					foreach (var gameAction in effect.GameActions)
						yield return (gameAction, effectContext);
				}
			}
		}

		//return [];
	}
}