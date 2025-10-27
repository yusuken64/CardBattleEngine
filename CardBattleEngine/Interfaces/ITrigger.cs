namespace CardBattleEngine;

public interface ITrigger
{
	bool CheckCondition(GameState state, GameActionBase action);
	GameActionBase GenerateAction(GameState gameState);
}
