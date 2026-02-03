namespace CardBattleEngine;

public class AddStatModifierAction : GameActionBase
{
	public StatModifierType StatModifierType {  get; set; }
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
		foreach (var target in this.ResolveTargets(state, actionContext).Where(x => x.IsAlive).ToList())
		{
			if (target == null)
			{
				continue;
			}

			StatModifier statModifier = new()
			{
				StatModifierType = StatModifierType,
				AttackChange = AttackChange.GetValueOrNull(state, actionContext),
				HealthChange = HealthChange.GetValueOrNull(state, actionContext),
				CostChange = CostChange.GetValueOrNull(state, actionContext),
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
					Target = target,
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
	public StatModifierType StatModifierType;
	public int? AttackChange;
	public int? HealthChange;
	public int? CostChange;

	public ExpirationTrigger ExpirationTrigger;

	public void ApplyValue(ref int stat, int? change)
	{
		if (!change.HasValue)
		{
			return;
		}

		if (StatModifierType == StatModifierType.Additive)
		{
			stat += change.Value;
		}
		else
		{
			stat = change.Value;
		}
	}
}

public enum StatModifierType
{
	Additive,
	Set
}
