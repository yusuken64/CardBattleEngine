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

		// Apply damage
		actionContext.Target.Health -= Damage;

		var sideEffects = new List<(IGameAction, ActionContext)>();

		// Check for death triggers
		if (actionContext.Target.Health <= 0)
		{
			sideEffects.Add((new DeathAction(), actionContext));
		}

		return sideEffects;
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
