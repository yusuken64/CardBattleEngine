namespace CardBattleEngine;

public class SummonRelicAction : GameActionBase
{
	public RelicCard Card { get; set; }
	public int IndexOffset { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
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

		var relic = actionContext.SummonedRelic
			?? new Relic(Card, actionContext.SourcePlayer);

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

		list.Insert(clampedIndex, relic);

		actionContext.SummonedRelic = relic;
		actionContext.SetSnapshot("SummonedRelic", relic.Clone());

		return [];
	}
}
