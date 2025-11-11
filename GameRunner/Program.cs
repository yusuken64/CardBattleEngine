using GameRunner;

MonteCarloModel model = new MonteCarloModel("../../../Data/Model");
OracleAgent.OracleBrain brain = new OracleAgent.OracleBrain()
{
	CardConservationWeight = 0.2f
};

OracleAgent.OracleBrain brain2 = new OracleAgent.OracleBrain()
{
	CardConservationWeight = 0.2f,
	AggressionWeight = 2.0f
};


Tournament tournament = new Tournament((engine, gameState, i) =>
{
	var player1 = new OracleAgent(engine, brain);
	var player2 = new OracleAgent(engine, brain2);
	return (player1, player2);
});
tournament.Run(20);

model.SaveWeights();