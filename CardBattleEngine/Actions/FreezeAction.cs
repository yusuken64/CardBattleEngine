namespace CardBattleEngine;

public class FreezeAction : IGameAction
{
	public bool Canceled { get; set; } = false;
	public EffectTrigger EffectTrigger => EffectTrigger.OnFreeze;

	public bool IsValid(GameState gameState, ActionContext context)
	{
		// Valid if the target exists and is alive
		return context.Target != null && context.Target.IsAlive;
	}

	public IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
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

	public Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>();
	}

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
		// Nothing to consume in this simple action
	}
}