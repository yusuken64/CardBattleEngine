namespace CardBattleEngine;

public class StartTurnAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.TurnStart;
	public override bool IsValid(GameState state, ActionContext actionContext) => true;

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		// Increment turn, switch current player
		Player player = actionContext.SourcePlayer;
		state.CurrentPlayer = player;
		state.turn++;

		if (player.HeroPower != null)
		{
			player.HeroPower.UsedThisTurn = false;
		}
		player.HasAttackedThisTurn = false;
		if (player.IsFrozen &&
			player.MissedAttackFromFrozen)
		{
			player.IsFrozen = false;
			player.MissedAttackFromFrozen = false;
		}
		else if (player.IsFrozen &&
			!player.MissedAttackFromFrozen)
		{
			player.MissedAttackFromFrozen = true;
		}

		// Reset attack flags
		foreach (var minion in actionContext.SourcePlayer.Board)
		{
			minion.AttacksPerformedThisTurn = 0;
			minion.HasSummoningSickness = false;

			if (minion.IsFrozen &&
				minion.MissedAttackFromFrozen)
			{
				minion.IsFrozen = false;
				minion.MissedAttackFromFrozen = false;
			}
			else if (minion.IsFrozen &&
				!minion.MissedAttackFromFrozen)
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