namespace CardBattleEngine;

public class StealthAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		// Valid if the target exists and is alive
		var single = context.Targets?.FirstOrDefault();
		return single is { IsAlive: true };
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (!IsValid(state, context, out string _))
			yield break;

		if (context.Targets?.FirstOrDefault() is Minion minion)
		{
			minion.IsStealth = true;
		}

		yield break; // no side-effects
	}
}