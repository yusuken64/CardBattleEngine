namespace CardBattleEngine;

public class RelicAbilityAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnRelicAbility;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		if (context.Source is not Relic relic || relic.Ability == null)
		{
			reason = null;
			return false;
		}

		if (context.Targets?.FirstOrDefault() is Minion minion &&
			(minion.IsStealth || minion.Elusive) &&
			minion.Owner != context.SourcePlayer)
		{
			reason = "Minion can't be targeted";
			return false;
		}

		reason = null;
		return !relic.Ability.UsedThisTurn &&
			context.SourcePlayer.Mana >= relic.Ability.ManaCost;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		Relic relic = (Relic)context.Source;
		RelicAbility ability = relic.Ability;
		ability.UsedThisTurn = true;
		yield return (new SpendManaAction() { Amount = ability.ManaCost }, context);

		foreach (var action in ability.GameActions)
		{
			IEnumerable<IGameEntity> targets;
			if (ability.AffectedEntitySelector != null)
			{
				targets = ability.AffectedEntitySelector.Select(state, context);
			}
			else
			{
				targets = context.Targets is { Count: > 0 } ? context.Targets : [null];
			}

			foreach (var target in targets)
			{
				ActionContext abilityActionContext = new()
				{
					SourcePlayer = context.SourcePlayer,
					Source = context.Source,
					Targets = [target],
					SourceCard = context.SourceCard,
				};

				yield return (action, abilityActionContext);
			}
		}
	}
}
