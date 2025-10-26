namespace CardBattleEngine;

internal class DeathAction : GameActionBase
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
				.Where(x => x.EffectTrigger == EffectTrigger.Deathrattle))
			{
				sideEffects.AddRange(effect.GameActions.Select(x =>
				{
					var target = actionContext.TargetSelector(state, minion.Owner, effect.TargetType);

					return (x, new ActionContext()
					{
						SourcePlayer = minion.Owner,
						Source = minion,
						Target = target
					});
				}));
			}
			
		}

		return sideEffects;
	}
}