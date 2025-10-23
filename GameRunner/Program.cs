using CardBattleEngine;
using GameRunner;

MonteCarloModel model = new MonteCarloModel("../../../Data/Model");
OracleAgent.OracleBrain brain = new OracleAgent.OracleBrain()
{
	CardConservationWeight = 0.2f
};

Tournament tournament = new Tournament((engine, gameState, i) =>
{
	var player1 = new OracleAgent(engine, brain);
	var player2 = new RandomAI(gameState.Players[1], new XorShiftRNG((ulong)i + 1));
	//var player2 = new LearningAgent(model);
	return (player1, player2);
});
tournament.Run(200);

model.SaveWeights();