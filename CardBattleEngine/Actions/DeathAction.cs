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
		// Kill target
		actionContext.Target.IsAlive = false;

		if (actionContext.Target is Player player)
		{
			yield return (new EndGameAction(), new ActionContext()
			{
				SourcePlayer = player,
			});
		}
		if (actionContext.Target is Minion minion)
		{
			// Move to graveyard
			int index = minion.Owner.Board.IndexOf(minion);

			minion.Owner.Board.Remove(minion);
			minion.Owner.Graveyard.Add(minion);

			// --- Deathrattles ---
			foreach (var effect in minion.TriggeredEffects
				.Where(e => e.EffectTrigger == EffectTrigger.Deathrattle))
			{
				foreach (var gameAction in effect.GameActions)
				{
					ActionContext selectorContext = new ActionContext
					{
						SourcePlayer = minion.Owner,
						Source = minion
					};

					var targets = effect.AffectedEntitySelector.Select(state, selectorContext);

					foreach (var target in targets)
					{
						yield return (
							gameAction,
							new ActionContext
							{
								SourcePlayer = minion.Owner,
								Source = minion,
								Target = target
							});
					}
				}
			}

			// --- Reborn ---
			if (minion.HasReborn)
			{
				yield return (
					new RebornAction(),      // (typo fixed: ReboardAction)
					new ActionContext
					{
						SourcePlayer = minion.Owner,
						SourceCard = minion.OriginalCard,
						Source = minion,
						PlayIndex = index
					});
			}
		}
	}
}
