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
		if (!IsValid(state, context, out var _)) {	yield break; }

		if (context.SourceCard is SpellCard spellcard) {
			foreach (SpellCastEffect spellCastEffect in spellcard.SpellCastEffects)
			{
				IEnumerable<IGameEntity> targets;
				if (spellCastEffect.AffectedEntitySelector != null)
				{
					targets = spellCastEffect.AffectedEntitySelector.Select(state, context);
				}
				else
				{
					targets = [context.Target];
				}

				foreach (var target in targets)
				{
					ActionContext spellActionContext = new()
					{
						SourcePlayer = context.SourcePlayer,
						Source = context.Source,
						Target = target,
						SourceCard = context.SourceCard,
						AffectedEntitySelector = spellCastEffect.AffectedEntitySelector,
					};

					foreach (var action in spellCastEffect.GameActions)
					{
						yield return (action, spellActionContext);
					}
				}
			}
		}
	}
}