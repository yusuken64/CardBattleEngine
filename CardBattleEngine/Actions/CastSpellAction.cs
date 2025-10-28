
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
		return true;
	}

	public IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.SourceCard is SpellCard spellcard) {
			foreach (var action in spellcard.SpellCastEffects
									.SelectMany(x => x.GameActions))
			{
				yield return (action, context);
			}
		}
	}
}