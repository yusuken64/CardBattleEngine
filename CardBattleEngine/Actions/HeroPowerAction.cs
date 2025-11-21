namespace CardBattleEngine;

public class HeroPowerAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnHeroPower;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		if (context.SourcePlayer.HeroPower == null)
		{
			return false;
		}

		return !context.SourcePlayer.HeroPower.UsedThisTurn &&
			context.SourcePlayer.Mana >= context.SourcePlayer.HeroPower.ManaCost;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		HeroPower heroPower = context.SourcePlayer.HeroPower;
		heroPower.UsedThisTurn = true;
		context.SourcePlayer.Mana -= heroPower.ManaCost;

		foreach (var action in heroPower.GameActions)
		{
			IEnumerable<IGameEntity> targets;
			if (heroPower.AffectedEntitySelector != null)
			{
				targets = heroPower.AffectedEntitySelector.Select(state, context);
			}
			else
			{
				targets = [context.Target];
			}

			foreach (var target in targets)
			{
				ActionContext heroPowerActionContext = new()
				{
					SourcePlayer = context.SourcePlayer,
					Source = context.Source,
					Target = target,
					SourceCard = context.SourceCard,
				};

				yield return (action, heroPowerActionContext);
			}
		}
	}
}
