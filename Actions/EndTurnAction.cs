namespace CardBattleEngine;

public class EndTurnAction : IGameAction
{
	public bool IsValid(GameState state) => true; // always valid

	IEnumerable<IGameAction> IGameAction.Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		// Increment turn, switch current player
		var temp = state.CurrentPlayer;
		state.CurrentPlayer = state.OpponentPlayer;
		state.OpponentPlayer = temp;

		state.turn++;

		// Queue start-of-turn effects
		return new IGameAction[]
		{
			new StartTurnAction(state.CurrentPlayer)
		};
	}
}