namespace CardBattleEngine;

public class DamageAction : GameActionBase
{
	public int Damage { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.OnDamage;
	
	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		// Valid if target is still alive / on board
		return actionContext.Target != null && actionContext.Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext))
			return [];

		var source = actionContext.Source;
		var target = actionContext.Target;

		// Divine Shield negates damage completely
		if (target is Minion targetMinion && targetMinion.HasDivineShield)
		{
			targetMinion.HasDivineShield = false;
			return [];
		}

		// Apply damage
		target.Health -= Damage;

		bool shouldDie = false;

		// Check lethal HP
		if (target.Health <= 0)
			shouldDie = true;

		// Check poisonous damage
		if (source is Minion attacker && attacker.HasPoisonous && target is Minion && Damage > 0)
			shouldDie = true;

		if (shouldDie)
		{
			return [(new DeathAction(), new ActionContext
			{
				SourcePlayer = actionContext.SourcePlayer,
				Source = target,
				Target = target,
				TargetSelector = actionContext.TargetSelector
			})];
		}

		return [];
	}

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		Damage = Convert.ToInt32(actionParam[nameof(Damage)]);
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(Damage), Damage }
		};
	}
}
