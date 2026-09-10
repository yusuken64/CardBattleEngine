
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
				Targets = context.Targets,
				OriginalAction = context.OriginalAction,
				OriginalContext = context.OriginalContext,
				OriginalSource = context.Source,
			});
		}

		if (TriggeredEffect.Frequency == EffectFrequency.OncePerTurn)
		{
			TriggeredEffect.UsedThisTurn = true;
		}

		if (TriggerSource is Relic relic && relic.Charges.HasValue)
		{
			relic.Charges--;
			if (relic.Charges <= 0)
			{
				yield return (new DeathAction(), new ActionContext
				{
					SourcePlayer = relic.Owner,
					Source = relic,
					Targets = [relic],
				});
			}
		}
	}
}
