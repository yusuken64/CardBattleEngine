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
		foreach (var entity in gameState.GetAllEntities())
		{
			foreach (var effect in entity.TriggeredEffects
				.Where(te => te.EffectTrigger == triggeringAction.EffectTrigger &&
							 te.EffectTiming == timing))
			{
				EffectContext effectContext = new()
				{
					EffectOwner = entity,
					SummonedUnit = context.Source as Minion
				};
				if (effect.Condition?.Evaluate(effectContext) == false)
				{
					continue;
				}

				foreach (var action in effect.GameActions)
				{
					var targets = gameState.GetValidTargets(entity, effect.TargetType);
					var target = picker(targets);

					yield return (action, new ActionContext
					{
						Source = entity,
						SourcePlayer = entity.Owner,
						Target = target,
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
