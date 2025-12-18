
namespace CardBattleEngine;

public class DebugLambaAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public Func<GameState, ActionContext, bool> IsValidFunc;
	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return IsValidFunc(gameState, context);
	}

	public Func<GameState, ActionContext, IEnumerable<(IGameAction, ActionContext)>> ResolveFuce;
	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		return ResolveFuce(state, context);
	}
}