namespace CardBattleEngine;

public class FreezeAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnFreeze;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		// Valid if the target exists and is alive
		return context.Target != null && context.Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (!IsValid(state, context))
			yield break;

		// Apply freeze
		if (context.Target is Minion minion)
		{
			minion.IsFrozen = true;
			minion.MissedAttackFromFrozen = false;
		}
		else if (context.Target is Player hero)
		{
			hero.IsFrozen = true;
		}

		yield break; // no side-effects
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>();
	}

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		// Nothing to consume in this simple action
	}
}
