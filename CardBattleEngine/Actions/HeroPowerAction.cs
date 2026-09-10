namespace CardBattleEngine;

public class HeroPowerAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnHeroPower;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		if (context.SourcePlayer.HeroPower == null)
		{
			reason = null;
			return false;
		}

		if (context.Targets is { Count: > 0 } &&
			context.Targets?.FirstOrDefault() is Minion minion &&
			(minion.IsStealth || minion.Elusive) &&
			minion.Owner != context.SourcePlayer)
		{
			reason = "Minion can't be targeted";
			return false;
		}

		reason = null;
		return !context.SourcePlayer.HeroPower.UsedThisTurn &&
			context.SourcePlayer.Mana >= context.SourcePlayer.HeroPower.ManaCost;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		HeroPower heroPower = context.SourcePlayer.HeroPower;
		heroPower.UsedThisTurn = true;
		yield return (new SpendManaAction() { Amount = heroPower.ManaCost }, context);

		foreach (var action in heroPower.GameActions)
		{
			IEnumerable<IGameEntity> targets;
			if (heroPower.AffectedEntitySelector != null)
			{
				targets = heroPower.AffectedEntitySelector.Select(state, context);
			}
			else
			{
				targets = context.Targets is { Count: > 0 } ? context.Targets : [null];
			}

			foreach (var target in targets)
			{
				ActionContext heroPowerActionContext = new()
				{
					SourcePlayer = context.SourcePlayer,
					Source = context.Source,
					Targets = [target],
					SourceCard = context.SourceCard,
				};

				yield return (action, heroPowerActionContext);
			}
		}
	}
}
