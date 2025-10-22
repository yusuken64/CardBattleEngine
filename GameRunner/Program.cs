using CardBattleEngine;
using GameRunner;

MonteCarloModel model = new MonteCarloModel("../../../Data/Model");

Tournament tournament = new Tournament((gameState, i) =>
{
	//var player1 = new RandomAI(gameState.Players[0], new XorShiftRNG((ulong)i));
	var player1 = new LearningAgent(model);
	//var player2 = new RandomAI(gameState.Players[1], new XorShiftRNG((ulong)i + 1));
	var player2 = new LearningAgent(model);
	return (player1, player2);
});
tournament.Run(20000);

model.SaveWeights();