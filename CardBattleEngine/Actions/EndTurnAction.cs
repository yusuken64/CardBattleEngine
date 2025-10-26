namespace CardBattleEngine;

public class EndTurnAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnTurnEnd;

	public override bool IsValid(GameState state) => true;

	public override IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		// Increment turn, switch current player
		var temp = state.CurrentPlayer;
		state.CurrentPlayer = state.OpponentPlayer;
		state.OpponentPlayer = temp;

		state.turn++;

		// Queue start-of-turn effects
		return new GameActionBase[]
		{
			new StartTurnAction(state.CurrentPlayer)
		};
	}
}