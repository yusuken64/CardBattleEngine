namespace CardBattleEngine;

public class EndTurnAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnTurnEnd;

	public override bool IsValid(GameState state, ActionContext actionContext) => true;

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		// Increment turn, switch current player
		var temp = state.CurrentPlayer;
		state.CurrentPlayer = state.OpponentPlayer;
		state.OpponentPlayer = temp;

		state.turn++;

		// Queue start-of-turn effects
		return new (IGameAction, ActionContext)[]
		{
			(new StartTurnAction(), new ActionContext()
			{
				SourcePlayer = state.CurrentPlayer
			})
		};
	}
}