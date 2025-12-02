namespace CardBattleEngine;

public class AddStatModifierAction : GameActionBase
{
	public int AttackChange { get; set; }
	public int HealthChange { get; set; }
	public int CostChange { get; set; }
	public ExpirationTrigger ExpirationTrigger { get; set;}
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		// Valid if target is still alive / on board
		return actionContext.Target != null && actionContext.Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext))
			return [];

		if (actionContext.Target != null)
		{
			if (actionContext.IsAuraEffect)
			{
				actionContext.Target.AddAuraModifier(new StatModifier
				{
					AttackChange = AttackChange,
					HealthChange = HealthChange,
					CostChange = CostChange,
					ExpirationTrigger = ExpirationTrigger,
				});
			}
			else
			{
				actionContext.Target.AddModifier(new StatModifier
				{
					AttackChange = AttackChange,
					HealthChange = HealthChange,
					CostChange = CostChange,
					ExpirationTrigger = ExpirationTrigger,
				});
			}
		}


		var sideEffects = new List<(IGameAction, ActionContext)>();

		// Check for death triggers
		if(actionContext.Target.Health <= 0)
		{
			sideEffects.Add((new DeathAction(), new ActionContext()
			{
				SourcePlayer = actionContext.Target.Owner,
				Source = actionContext.Target,
			}));
		}

		return sideEffects;
	}

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		AttackChange = JsonParamHelper.GetValue<int>(actionParam, nameof(AttackChange));
		HealthChange = JsonParamHelper.GetValue<int>(actionParam, nameof(HealthChange));
		CostChange = JsonParamHelper.GetValue<int>(actionParam, nameof(CostChange));
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(AttackChange), AttackChange },
			{ nameof(HealthChange), HealthChange },
			{ nameof(CostChange), CostChange }
		};
	}
}
public class AddStatModifierValueAction : GameActionBase
{
	public IValueProvider AttackChange { get; set; }
	public IValueProvider HealthChange { get; set; }
	public IValueProvider CostChange { get; set; }
	public ExpirationTrigger ExpirationTrigger { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		// Valid if target is still alive / on board
		return actionContext.Target != null && actionContext.Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext))
			return [];

		if (actionContext.Target != null)
		{
			if (actionContext.IsAuraEffect)
			{
				actionContext.Target.AddAuraModifier(new StatModifier
				{
					AttackChange = AttackChange.GetValueOrZero(state, actionContext),
					HealthChange = HealthChange.GetValueOrZero(state, actionContext),
					CostChange = CostChange.GetValueOrZero(state, actionContext),
					ExpirationTrigger = ExpirationTrigger,
				});
			}
			else
			{
				actionContext.Target.AddModifier(new StatModifier
				{
					AttackChange = AttackChange.GetValueOrZero(state, actionContext),
					HealthChange = HealthChange.GetValueOrZero(state, actionContext),
					CostChange = CostChange.GetValueOrZero(state, actionContext),
					ExpirationTrigger = ExpirationTrigger,
				});
			}
		}


		var sideEffects = new List<(IGameAction, ActionContext)>();

		// Check for death triggers
		if (!actionContext.Target.IsAlive)
		{
			sideEffects.Add((new DeathAction(), new ActionContext()
			{
				SourcePlayer = actionContext.Target.Owner,
				Source = actionContext.Target,
			}));
		}

		return sideEffects;
	}
}

public class RemoveModifierAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
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