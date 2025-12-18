
namespace CardBattleEngine;

//this is a passthrough wrapper for triggeredeffects
//for the ui to animate
public class TriggerEffectAction : GameActionBase
{
	public TriggeredEffect TriggeredEffect;
	public ITriggerSource TriggerSource;
	public ActionContext EffectContext;
	public override EffectTrigger EffectTrigger => EffectTrigger.None;


	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return TriggeredEffect.AffectedEntitySelector != null;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (TriggeredEffect.AffectedEntitySelector == null) yield break;

		foreach (var action in TriggeredEffect.GameActions)
		{
			yield return (action, new ActionContext
			{
				Source = TriggerSource.Entity,
				SourcePlayer = TriggerSource.Entity.Owner,
				AffectedEntitySelector = TriggeredEffect.AffectedEntitySelector,
				Target = context.Target,
				OriginalAction = context.OriginalAction,
				OriginalSource = context.Source,
			});
		}
	}
}
