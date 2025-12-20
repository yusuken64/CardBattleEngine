namespace CardBattleEngine;

public class DamageAction : GameActionBase
{
	public IValueProvider Damage { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.OnDamage;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		return
			actionContext.AffectedEntitySelector != null ||
			(actionContext.Target != null && actionContext.Target.IsAlive);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext, out string _))
			yield break;

		var source = actionContext.Source;

		var targets = this.ResolveTargets(state, actionContext);

		foreach (var target in targets)
		{
			int damageToApply = Damage.GetValue(state, actionContext);

			// Divine Shield negates damage completely
			if (target is Minion targetMinion && targetMinion.HasDivineShield)
			{
				targetMinion.HasDivineShield = false;
				damageToApply = 0; // no damage actually applied
			}

			int armorBlocked = 0;
			if (target is Player player)
			{
				armorBlocked = Math.Min(player.Armor, damageToApply);

				player.Armor -= armorBlocked;
				damageToApply -= armorBlocked;
			}

			// Apply damage
			int originalHealth = target.Health;
			target.Health -= damageToApply;
			int actualDamageDealt = Math.Max(0, originalHealth - target.Health);
			actionContext.DamageDealt = actualDamageDealt + armorBlocked;
			actionContext.HealthDamageDealt = actualDamageDealt;
			actionContext.ArmorDamageDealt = armorBlocked;
			actionContext.Target = target;

			// Lifesteal: heal source for actual damage dealt
			if (source is not null &&
				source is Minion attackingMinion &&
				attackingMinion.HasLifeSteal &&
				actualDamageDealt > 0)
			{
				yield return (
					new HealAction()
					{
						Amount = (Value)actualDamageDealt
					},
					new ActionContext()
					{
						SourcePlayer = attackingMinion.Owner,
						Source = attackingMinion,
						Target = attackingMinion.Owner
					});
			}

			// Determine death
			bool shouldDie = false;

			if (target.Health <= 0)
				shouldDie = true;

			// Poisonous effect
			if (source is not null && 
				source is Minion attacker && 
				attacker.HasPoisonous && 
				target is Minion && 
				actualDamageDealt > 0)
				shouldDie = true;

			if (shouldDie)
			{
				yield return (new DeathAction(), new ActionContext
				{
					SourcePlayer = actionContext.SourcePlayer,
					Source = target,
					Target = target,
					AffectedEntitySelector = actionContext.AffectedEntitySelector
				});
			}
		}
	}

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		Damage = (Value)JsonParamHelper.GetValue<int>(actionParam, nameof(Damage));
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(Damage), Damage }
		};
	}
}
