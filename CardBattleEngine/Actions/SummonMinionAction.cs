using System.Collections.Generic;

namespace CardBattleEngine;

public class SummonMinionAction : GameActionBase
{
	public MinionCard Card { get; set; }
	public int IndexOffset { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.SummonMinion;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		// Board not full
		if (actionContext.SourcePlayer.Board.Count >= state.MaxBoardSize)
		{
			reason = null;
			return false;
		}

		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext, out var _))
			return [];

		// Create minion entity
		var minion = new Minion(Card, actionContext.SourcePlayer);

		var list = actionContext.SourcePlayer.Board;
		int requested = actionContext.PlayIndex + IndexOffset;

		int clampedIndex =
			(requested == -1 || requested < 0 || requested > list.Count)
				? list.Count
				: requested;

		list.Insert(clampedIndex, minion);

		actionContext.PlayIndex = clampedIndex;

		if (actionContext.IsReborn)
		{
			minion.HasReborn = false;
			minion.Health = 1;
		}

		actionContext.SummonedMinion = minion;
		actionContext.SummonedMinionSnapShot = minion.Clone();

		return [];
	}
}
