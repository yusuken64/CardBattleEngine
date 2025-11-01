﻿namespace CardBattleEngine.Test;

public class DebugLambaAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public Func<GameState, ActionContext, bool> IsValidFunc;
	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return IsValidFunc(gameState, context);
	}

	public Func<GameState, ActionContext, IEnumerable<(IGameAction, ActionContext)>> ResolveFunc;
	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		return ResolveFunc(state, context);
	}
}