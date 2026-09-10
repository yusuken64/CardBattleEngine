namespace CardBattleEngine;

public class EventBus
{
	internal IEnumerable<(IGameAction, ActionContext)> EvaluatePersistentEffects(GameState gameState)
	{
		var sideEffects = new List<(IGameAction, ActionContext)>();

		// 1. Clear all aura modifiers that were previously applied
		List<IGameEntity> entities = gameState.GetAllEntities().ToList();
		foreach (var entity in entities)
		{
			entity.ClearAuras(true);
		}

		// 2. Find all aura sources on the battlefield
		foreach (var source in gameState.GetAllTriggerSources())
		{
			foreach (var auraEffect in source.TriggeredEffects
				.Where(te => te.EffectTrigger == EffectTrigger.Aura &&
							 te.EffectTiming == EffectTiming.Persistant))
			{
				// Build an effect context for condition checks
				var effectContext = new ActionContext()
				{
					SourcePlayer = source.Entity.Owner,
					Source = source.Entity,
					Target = null,
				};

				if (auraEffect.Condition != null && !auraEffect.Condition.Evaluate(effectContext))
				{
					continue;
				}

				foreach (var action in auraEffect.GameActions)
				{
					ActionContext context = new()
					{
						Source = (IGameEntity)source,
						SourcePlayer = source.Entity.Owner,
						AffectedEntitySelector = auraEffect.AffectedEntitySelector,
						IsAuraEffect = true,
					};
					sideEffects.AddRange(action.Resolve(gameState, context));
				}
			}
		}

		foreach (var entity in entities)
		{
			entity.RecalculateStats();
		}

		return sideEffects;
	}

	/// <summary>
	/// Returns all triggers for a given action, filtered by timing (Pre/Post/Other),
	/// including both normal triggered effects and modifier-triggered effects.
	/// </summary>
	internal IEnumerable<(IGameAction action, ActionContext context)> GetTriggers(
		GameState gameState,
		IGameAction triggeringAction,
		ActionContext context,
		EffectTiming timing)
	{
		// Normal card effects
		foreach (var triggerSource in gameState.GetAllTriggerSources())
		{
			foreach (var effect in triggerSource.TriggeredEffects
				.Where(te => te.EffectTrigger == triggeringAction.EffectTrigger &&
							 te.EffectTiming == timing &&
							 (te.Scope != TriggerScope.Self || triggerSource.Entity == context.Source) &&
							 (te.Frequency != EffectFrequency.OncePerTurn || !te.UsedThisTurn)))
			{
				var effectContext = new ActionContext()
				{
					SourcePlayer = triggerSource.Entity.Owner,
					Source = triggerSource.Entity,
					Target = context.Target,
					SummonedMinion = context.SummonedMinion,
					PlayIndex = context.PlayIndex,
					SourceCard = context.SourceCard,
					OriginalAction = context.OriginalAction,
					OriginalContext = context.OriginalContext,
					OriginalSource = context.Source
				};

				if (effect.Condition?.Evaluate(effectContext) == false)
				{
					continue;
				}

				yield return (new TriggerEffectAction()
				{
					TriggeredEffect = effect,
					TriggerSource = triggerSource,
					EffectContext = effectContext,
				}, context);
			}
		}

		// Modifier-triggered effects
		foreach (var minion in gameState.GetAllMinions())
		{
			foreach (var effect in minion.ModifierTriggeredEffects
				.Where(te => te.Item1.EffectTrigger == triggeringAction.EffectTrigger &&
							 te.Item1.EffectTiming == timing).ToList())
			{
				foreach (IGameAction action in effect.Item1.GameActions)
				{
					yield return (action, new ActionContext
					{
						Source = minion,
						SourcePlayer = minion.Owner,
						Target = minion,
						Modifier = effect.Item2,
						AffectedEntitySelector = null
					});
				}
			}
		}
	}
}