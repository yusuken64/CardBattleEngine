﻿namespace CardBattleEngine;

public class EventBus
{
	internal void EvaluatePersistentEffects(GameState gameState)
	{
		// 1. Clear all aura modifiers that were previously applied
		foreach (var minion in gameState.GetAllMinions())
		{
			minion.ClearAuras();
		}

		// 2. Find all aura sources on the battlefield
		foreach (var source in gameState.GetAllTriggerSources())
		{
			foreach (var auraEffect in source.TriggeredEffects
				.Where(te => te.EffectTrigger == EffectTrigger.Aura &&
							 te.EffectTiming == EffectTiming.Persistant))
			{
				// Build an effect context for condition checks
				var effectContext = new EffectContext
				{
					EffectOwner = source as IGameEntity,
					OriginalOwner = source.Owner,
				};

				if (auraEffect.Condition != null && !auraEffect.Condition.Evaluate(effectContext))
					continue;

				// 3. Apply the aura's actions to valid targets
				var actionContext = new ActionContext
				{
					Source = (IGameEntity)source,
					SourcePlayer = source.Owner
				};
				var targets = auraEffect.AffectedEntitySelector.Select(gameState, actionContext);
				foreach (var target in targets)
				{
					foreach(var action in auraEffect.GameActions)
					{
						action.Resolve(gameState, new ActionContext() { Target = target });
					}
				}
			}
		}
	}


	/// <summary>
	/// Returns all triggers for a given action, filtered by timing (Pre/Post/Other),
	/// including both normal triggered effects and modifier-triggered effects.
	/// </summary>
	internal IEnumerable<(IGameAction action, ActionContext context)> GetTriggers(
		GameState gameState,
		IGameAction triggeringAction,
		ActionContext context,
		EffectTiming timing,
		Func<List<ITriggerSource>, ITriggerSource> picker)
	{
		// Normal card effects
		foreach (var triggerSource in gameState.GetAllTriggerSources())
		{
			foreach (var effect in triggerSource.TriggeredEffects
				.Where(te => te.EffectTrigger == triggeringAction.EffectTrigger &&
							 te.EffectTiming == timing))
			{
				EffectContext effectContext = new()
				{
					EffectOwner = triggerSource as IGameEntity,
					SummonedUnit = context.SummonedMinion,
					SecretOwner = triggerSource.Owner,
					TriggeringAction = triggeringAction,
					OriginalOwner = context.SourcePlayer,
				};

				if (effect.Condition?.Evaluate(effectContext) == false)
				{
					continue;
				}

				foreach (var action in effect.GameActions)
				{
					var targets = gameState.GetValidTargets(triggerSource, effect.TargetType);
					var target = picker(targets);

					yield return (action, new ActionContext
					{
						Source = triggerSource.Owner,
						SourcePlayer = triggerSource.Owner,
						Target = target as IGameEntity,
						OriginalAction = context.OriginalAction,
						AffectedEntitySelector = null // engine injects default/random if needed
					});
				}
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