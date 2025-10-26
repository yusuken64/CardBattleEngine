namespace CardBattleEngine;

internal class SummonMinionAction : GameActionBase
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
		minion.Name = Card.Name;
		actionContext.SourcePlayer.Board.Add(minion);

		return [];
	}
}
