
namespace CardBattleEngine;

//this is a passthrough wrapper for triggeredeffects
//for the ui to animate
public class TriggerEffectAction : GameActionBase
{
	public TriggeredEffect TriggeredEffect;
	public ITriggerSource TriggerSource;
	public ActionContext EffectContext;
	public override EffectTrigger EffectTrigger => EffectTrigger.None;


	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return TriggeredEffect.AffectedEntitySelector != null;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (TriggeredEffect.AffectedEntitySelector == null) yield break;

		foreach (var action in TriggeredEffect.GameActions)
		{
			var affectedTargets = TriggeredEffect.AffectedEntitySelector.Select(state, EffectContext);

			foreach (var target in affectedTargets)
			{
				yield return (action, new ActionContext
				{
					Source = TriggerSource.Entity,
					SourcePlayer = TriggerSource.Entity.Owner,
					Target = target as IGameEntity,
					OriginalAction = context.OriginalAction,
					OriginalSource = context.Source,
				});
			}
		}
	}
}
