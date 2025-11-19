using System.Reflection;

namespace CardBattleEngine;

public class SummonMinionAction : GameActionBase
{
	public MinionCard Card;
	public override EffectTrigger EffectTrigger => EffectTrigger.SummonMinion;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		// Board not full
		if (actionContext.SourcePlayer.Board.Count >= state.MaxBoardSize)
			return false;

		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext))
			return [];

		// Create minion entity
		var minion = new Minion(Card, actionContext.SourcePlayer);
		if (actionContext.PlayIndex != -1)
		{
			actionContext.SourcePlayer.Board.Insert(actionContext.PlayIndex, minion);
		}
		else
		{
			actionContext.SourcePlayer.Board.Add(minion);
		}
		actionContext.SummonedMinion = minion;

		return [];
	}
}
