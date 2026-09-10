namespace CardBattleEngine;

public class DeathAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnDeath;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		return actionContext.Targets?.FirstOrDefault()?.IsAlive == true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		// Kill target
		var firstTarget = actionContext.Targets?.FirstOrDefault();
		if (firstTarget != null)
		{
			firstTarget.IsAlive = false;
		}

		if (firstTarget is Player player)
		{
			yield return (new EndGameAction(), new ActionContext()
			{
				SourcePlayer = state.OpponentOf(player),
			});
		}
		if (firstTarget is Minion minion)
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
						Source = minion,
						PlayIndex = index,
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
								Targets = [target],
								PlayIndex = index,
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
		if (firstTarget is Relic relic)
		{
			relic.Owner.Board.Remove(relic);
		}
	}
}
