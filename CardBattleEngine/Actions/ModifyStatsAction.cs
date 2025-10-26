namespace CardBattleEngine;

public class ModifyStatsAction : GameActionBase
{
	public IGameEntity Target { get; set; }
	public int AttackChange { get; set; }
	public int HealthChange { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		// Valid if target is still alive / on board
		return Target != null && Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state	, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext))
			return [];

		Target.Attack += AttackChange;
		Target.Health += HealthChange;

		var sideEffects = new List<(IGameAction, ActionContext)>();

		// Check for death triggers
		if (Target.Health <= 0)
		{
			sideEffects.Add((new DeathAction(), new ActionContext()
			{
				SourcePlayer = Target.Owner,
				Source = Target,
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