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
		var minion = actionContext.SummonedMinion
			?? new Minion(Card, actionContext.SourcePlayer);

		var list = actionContext.SourcePlayer.Board;
		int playIndex = actionContext.PlayIndex;

		int clampedIndex =
			(playIndex == -1 || playIndex < 0 || playIndex > list.Count)
				? list.Count
				: playIndex;
		var offsetIndex = clampedIndex + IndexOffset;

		clampedIndex =
			(offsetIndex == -1 || offsetIndex < 0 || offsetIndex > list.Count)
				? list.Count
				: offsetIndex;

		list.Insert(clampedIndex, minion);

		actionContext.PlayIndex = list.IndexOf(minion);

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