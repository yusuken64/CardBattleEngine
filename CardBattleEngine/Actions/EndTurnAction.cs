namespace CardBattleEngine;

public class EndTurnAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnTurnEnd;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		var player = state.CurrentPlayer;
		if (player.IsFrozen &&
			player.MissedAttackFromFrozen)
		{
			player.IsFrozen = false;
			player.MissedAttackFromFrozen = false;
		}

		foreach (var minion in actionContext.SourcePlayer.Board)
		{
			if (minion.IsFrozen &&
				minion.MissedAttackFromFrozen)
			{
				minion.IsFrozen = false;
				minion.MissedAttackFromFrozen = false;
			}
			else
			{
				minion.MissedAttackFromFrozen = true;
			}
		}

		// Queue start-of-turn effects
		Player opponent = state.OpponentOf(state.CurrentPlayer);
		return new (IGameAction, ActionContext)[]
		{
			(new StartTurnAction(), new ActionContext()
			{
				Source = opponent,
				SourcePlayer = opponent
			})
		};
	}
}