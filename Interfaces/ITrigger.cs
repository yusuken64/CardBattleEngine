namespace CardBattleEngine;

public interface ITrigger
{
	bool CheckCondition(GameState state, IGameAction action);
	IGameAction GenerateAction(GameState gameState);
}