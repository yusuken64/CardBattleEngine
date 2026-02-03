namespace CardBattleEngine;

public class AddTriggerAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public TriggeredEffect Effect { get; set; }

	public override bool IsValid(GameState state, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		var targets = ResolveTargets(state, context);

		foreach (var triggerSource in targets.OfType<ITriggerSource>())
		{
			triggerSource.TriggeredEffects.Add(Effect.Clone());
		}

		return [];
	}
}
