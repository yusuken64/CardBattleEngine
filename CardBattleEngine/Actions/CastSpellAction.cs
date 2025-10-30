namespace CardBattleEngine;

public class CastSpellAction : IGameAction
{
	public bool Canceled { get; set; }

	public EffectTrigger EffectTrigger => EffectTrigger.SpellCast;

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public Dictionary<string, object> EmitParams()
	{
		return new();
	}

	public bool IsValid(GameState gameState, ActionContext context)
	{
		return context.SourceCard is SpellCard spellcard;
	}

	public IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (!IsValid(state, context)) {	yield break; }

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