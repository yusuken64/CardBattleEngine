namespace CardBattleEngine;

public class StealthAction : IGameAction
{
	public bool Canceled { get; set; } = false;
	public EffectTrigger EffectTrigger => EffectTrigger.None;

	public bool IsValid(GameState gameState, ActionContext context)
	{
		// Valid if the target exists and is alive
		return context.Target != null && context.Target.IsAlive;
	}

	public IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (!IsValid(state, context))
			yield break;

		if (context.Target is Minion minion)
		{
			minion.IsStealth = true;
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