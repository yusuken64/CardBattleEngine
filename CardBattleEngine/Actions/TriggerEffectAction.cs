
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
		return TriggeredEffect.AffectedEntitySelector != null || TriggeredEffect.TargetRequirement != null;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (TriggeredEffect.TargetRequirement != null)
		{
			if (TriggeredEffect.GameActions.Count > 0)
			{
				foreach (var action in TriggeredEffect.GameActions)
				{
					yield return (action, new ActionContext
					{
						Source = TriggerSource.Entity,
						SourcePlayer = TriggerSource.Entity.Owner,
						SourceCard = TriggerSource.Entity is Minion m ? m.OriginalCard : null,
						PendingTargetRequirement = TriggeredEffect.TargetRequirement,
						OriginalAction = context.OriginalAction,
						OriginalSource = context.Source,
					});
				}
			}
			else
			{
				// If TargetRequirement is set but GameActions is empty, yield a no-op action
				// to allow the target selection flow to proceed
				yield return (new DebugLambaAction
				{
					IsValidFunc = (s, c) => true,
					ResolveFunc = (s, c) => Enumerable.Empty<(IGameAction, ActionContext)>()
				}, new ActionContext
				{
					Source = TriggerSource.Entity,
					SourcePlayer = TriggerSource.Entity.Owner,
					SourceCard = TriggerSource.Entity is Minion m ? m.OriginalCard : null,
					PendingTargetRequirement = TriggeredEffect.TargetRequirement,
					OriginalAction = context.OriginalAction,
					OriginalSource = context.Source,
				});
			}
			yield break;
		}

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
