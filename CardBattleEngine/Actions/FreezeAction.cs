namespace CardBattleEngine;

public class FreezeAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnFreeze;

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

		// Apply freeze
		if (context.Targets?.FirstOrDefault() is Minion minion)
		{
			minion.IsFrozen = true;
			minion.MissedAttackFromFrozen = false;
			context.ResolvedStatusChanges.Add(
				new StatusDelta(minion, StatusType.Freeze, true));
		}
		else if (context.Targets?.FirstOrDefault() is Player hero)
		{
			hero.IsFrozen = true;
		}

		yield break; // no side-effects
	}
}
