using CardBattleEngine;

if (args.Length > 0 && args[0] == "--remote")
{
	string serverUrl = args.Length > 1 ? args[1]
		: Environment.GetEnvironmentVariable("GAMESERVER_URL") ?? "http://localhost:5299";
	Console.Write("Enter your player name: ");
	string playerName = Console.ReadLine() is { Length: > 0 } name ? name : "Player";
	await RemoteGameClient.RunAsync(serverUrl, playerName);
	return;
}

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