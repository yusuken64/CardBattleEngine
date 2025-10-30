using System.Net.Sockets;
using System.Numerics;

namespace CardBattleEngine;

public class EventBus
{
	/// <summary>
	/// Returns all triggers for a given action, filtered by timing (Pre/Post/Other),
	/// including both normal triggered effects and modifier-triggered effects.
	/// </summary>
	internal IEnumerable<(IGameAction action, ActionContext context)> GetTriggers(
		GameState gameState,
		IGameAction triggeringAction,
		ActionContext context,
		EffectTiming timing,
		Func<List<IGameEntity>, IGameEntity> picker)
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
					EffectOwner = triggerSource.Owner,
					SummonedUnit = context.Source as Minion,
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
					var targets = gameState.GetValidTargets(triggerSource.Owner, effect.TargetType);
					var target = picker(targets);

					yield return (action, new ActionContext
					{
						Source = triggerSource.Owner,
						SourcePlayer = triggerSource.Owner,
						Target = target,
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
							 te.Item1.EffectTiming == timing))
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