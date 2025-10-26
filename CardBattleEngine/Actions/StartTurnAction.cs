using System.Numerics;

namespace CardBattleEngine;

internal class StartTurnAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.TurnStart;
	public override bool IsValid(GameState state, ActionContext actionContext) => true;

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		// Reset attack flags
		foreach (var minion in actionContext.SourcePlayer.Board)
		{
			minion.HasAttackedThisTurn = false;
			minion.HasSummoningSickness = false;
		}

		return
		[
			(new IncreaseMaxManaAction() { Amount = 1 }, actionContext),
			(new RefillManaAction(), actionContext),
			(new DrawCardAction(), actionContext)
		];
	}
}
