namespace CardBattleEngine;

public class AddStatModifierAction : GameActionBase
{
	public IValueProvider AttackChange { get; set; }
	public IValueProvider HealthChange { get; set; }
	public IValueProvider CostChange { get; set; }
	public ExpirationTrigger ExpirationTrigger { get; set;}
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		return this.ResolveTargets(state, actionContext).Any(x => x.IsAlive);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		//if (!IsValid(state, actionContext))
		//{
		//	yield break;
		//}

		var sideEffects = new List<(IGameAction, ActionContext)>();

		foreach (var target in this.ResolveTargets(state, actionContext).ToList())
		{
			if (target == null)
			{
				continue;
			}

			StatModifier statModifier = new()
			{
				AttackChange = AttackChange.GetValueOrZero(state, actionContext),
				HealthChange = HealthChange.GetValueOrZero(state, actionContext),
				CostChange = CostChange.GetValueOrZero(state, actionContext),
				ExpirationTrigger = ExpirationTrigger,
			};

			if (actionContext.IsAuraEffect)
			{
				target.AddAuraModifier(statModifier);
			}
			else
			{
				target.AddModifier(statModifier);
			}

			// Check for death triggers
			if (target.Health <= 0)
			{
				yield return (new DeathAction(), new ActionContext()
				{
					SourcePlayer = target.Owner,
					Source = target,
				});
			}
		}
	}
}

public class RemoveModifierAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return context.Target.HasModifier(context.Modifier);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		context.Target.RemoveModifier(context.Modifier);

		return [];
	}
}

public class StatModifier
{
	public int AttackChange;
	public int HealthChange;
	public int CostChange;

	public ExpirationTrigger ExpirationTrigger;
}