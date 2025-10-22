namespace CardBattleEngine;

// Basic effect/action that can be executed on a GameState
public interface IGameAction
{
	bool IsValid(GameState gameState);
	IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent); //returns side effects
}
