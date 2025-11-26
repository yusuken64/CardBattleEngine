using CardBattleEngine;

var gameState = GameFactory.CreateTestGame();
var engine = new GameEngine();

engine.ActionResolvedCallback = (gameStat) =>
{
	GameEngine.PrintState(gameState, null);
};

IGameAgent player1 = new HumanAgent(gameState.Players[0]);
IGameAgent player2 = new RandomAI(gameState.Players[1], new XorShiftRNG(0));

engine.StartGame(gameState);
while (!gameState.IsGameOver())
{
	var currentAgent = gameState.CurrentPlayer == gameState.Players[0] ? player1 : player2;

	(IGameAction action, ActionContext context) = currentAgent.GetNextAction(gameState);
	engine.Resolve(gameState, context, action);
}

GameEngine.PrintState(gameState, null);
Console.WriteLine($"Winner: {gameState.Winner?.Name}");