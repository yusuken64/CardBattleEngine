namespace CardBattleEngine;

public class StartTurnAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.TurnStart;
	public override bool IsValid(GameState state, ActionContext actionContext) => true;

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		Player player = actionContext.SourcePlayer;
		player.HasAttackedThisTurn = false;
		if (player.IsFrozen)
		{
			player.MissedAttackFromFrozen = true;
		}

		// Reset attack flags
		foreach (var minion in actionContext.SourcePlayer.Board)
		{
			minion.HasAttackedThisTurn = false;
			minion.HasSummoningSickness = false;

			if (minion.IsFrozen)
			{
				minion.MissedAttackFromFrozen = true;
			}
		}

		return
		[
			(new IncreaseMaxManaAction() { Amount = 1 }, actionContext),
			(new RefillManaAction(), actionContext),
			(new DrawCardFromDeckAction(), actionContext)
		];
	}
}
