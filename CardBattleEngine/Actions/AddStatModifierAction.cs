namespace CardBattleEngine;

public class AddStatModifierAction : GameActionBase
{
	public int AttackChange { get; set; }
	public int HealthChange { get; set; }
	public EffectDuration Duration = EffectDuration.Permanent;
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		// Valid if target is still alive / on board
		return actionContext.Target != null && actionContext.Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state	, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext))
			return [];

		if (actionContext.Target is Minion minion)
		{
			minion.AddModifier(new StatModifier
			{
				AttackChange = AttackChange,
				HealthChange = HealthChange,
				Duration = Duration,
				SourcePlayer = actionContext.SourcePlayer
			});
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
		AttackChange = Convert.ToInt32(actionParam[nameof(AttackChange)]);
		HealthChange = Convert.ToInt32(actionParam[nameof(HealthChange)]);
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(AttackChange), AttackChange },
			{ nameof(HealthChange), HealthChange }
		};
	}
}

public class RemoveModifierAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		if (context.Target is not Minion minion)
		{
			return false;
		}

		return minion.HasModifier(context.Modifier);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is not Minion minion)
		{
			return [];
		}

		minion.RemoveModifier(context.Modifier);

		return [];
	}
}

public class StatModifier
{
	public int AttackChange;
	public int HealthChange;
	public EffectDuration Duration;
	public Player SourcePlayer;
}

public enum EffectDuration
{
	Permanent,
	UntilEndOfTurn,
}