using CardBattleEngine;

var gameState = GameFactory.CreateTestGame();
var engine = new GameEngine(new XorShiftRNG(1));

//IGameAI player1 = new RandomAI(gameState.Players[0], new XorShiftRNG(1));
IGameAgent player1 = new HumanAgent(gameState.Players[0]);
IGameAgent player2 = new RandomAI(gameState.Players[1], new XorShiftRNG(1));

bool stopPerAction = false;

engine.StartGame(gameState);
while (!gameState.IsGameOver())
{
	var currentAgent = gameState.CurrentPlayer == gameState.Players[0] ? player1 : player2;
	var action = currentAgent.GetNextAction(gameState);
	engine.Resolve(gameState, gameState.CurrentPlayer, gameState.OpponentPlayer, action);

	if (stopPerAction)
	{
		Console.ReadLine();
	}
}

Console.WriteLine($"Winner: {gameState.Winner?.Name}");