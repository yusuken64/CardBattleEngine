namespace CardBattleEngine;

public class CastSpellAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.SpellCast;

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new();
	}

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return context.SourceCard is SpellCard spellcard;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (!IsValid(state, context, out var _)) { yield break; }

		if (context.SourceCard is not SpellCard spellcard)
		{
			yield break;
		}

		foreach (SpellCastEffect spellCastEffect in spellcard.SpellCastEffects)
		{
			var selector = spellCastEffect.AffectedEntitySelector;

			if (selector == null ||
				selector.ResolutionTiming == TargetResolutionTiming.Once)
			{
				context.AffectedEntitySelector = selector;
				var targets = ResolveTargets(state, context);

				foreach (var target in targets)
				{
					var spellActionContext = new ActionContext
					{
						SourcePlayer = context.SourcePlayer,
						Source = context.Source,
						Target = target,
						SourceCard = context.SourceCard,
						AffectedEntitySelector = null,
					};

					foreach (var action in spellCastEffect.GameActions)
						yield return (action, spellActionContext);
				}
			}
			else
			{
				var spellActionContext = new ActionContext
				{
					SourcePlayer = context.SourcePlayer,
					Source = context.Source,
					Target = null,
					SourceCard = context.SourceCard,
					AffectedEntitySelector = selector,
				};

				foreach (var action in spellCastEffect.GameActions)
				{
					yield return (action, spellActionContext);
				}
			}
		}
	}
}