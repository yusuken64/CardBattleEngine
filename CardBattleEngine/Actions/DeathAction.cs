namespace CardBattleEngine;

public class DeathAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnDeath;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		return actionContext.Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		var sideEffects = new List<(IGameAction, ActionContext)>();
		actionContext.Target.IsAlive = false;

		if (actionContext.Target is Minion minion)
		{
			minion.Owner.Board.Remove(minion);
			minion.Owner.Graveyard.Add(minion);

			foreach (var effect in minion.TriggeredEffects
				.Where(e => e.EffectTrigger == EffectTrigger.Deathrattle))
			{
				foreach (var gameAction in effect.GameActions)
				{
					ActionContext selectorContext = new ActionContext()
					{
						SourcePlayer = minion.Owner,
						Source = minion,
					};
					var targets = effect.AffectedEntitySelector.Select(state, selectorContext);

					foreach (var target in targets)
					{
						sideEffects.Add((gameAction, new ActionContext
						{
							SourcePlayer = minion.Owner,
							Source = minion,
							Target = target
						}));
					}
				}
			}
		}

		return sideEffects;
	}
}