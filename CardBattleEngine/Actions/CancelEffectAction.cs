
namespace CardBattleEngine;

public class CancelEffectAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.SpellCountered;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return !context.OriginalAction.Canceled;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		context.OriginalAction.Canceled = true;

		return [];
	}
}