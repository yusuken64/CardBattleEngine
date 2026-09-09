using CardBattleEngine;

// Usage:
//   dotnet run --project GamePlayer                              local single-process game (HumanAgent vs RandomAI)
//   dotnet run --project GamePlayer -- --host [url]               spin up a GameServer + two --remote player windows
//   dotnet run --project GamePlayer -- --host [url] --list        same, but both windows use the scrolling debug view
//   dotnet run --project GamePlayer -- --remote [url]              connect to a running GameServer (live overwrite-in-place view)
//   dotnet run --project GamePlayer -- --remote [url] --list       same, but with the scrolling debug view (shows every
//                                                                   broadcast unfiltered, including redundant ones)
// [url] defaults to $GAMESERVER_URL, or http://localhost:5299 if unset.
// dotnet run --project GamePlayer -- --host

if (args.Length > 0 && args[0] == "--host")
{
	bool useListView = args.Contains("--list");
	string hostUrl = args.Skip(1).FirstOrDefault(a => a != "--list")
		?? Environment.GetEnvironmentVariable("GAMESERVER_URL") ?? "http://localhost:5299";
	await LocalMultiplayerHost.RunAsync(hostUrl, useListView);
	return;
}

if (args.Length > 0 && args[0] == "--remote")
{
	bool useListView = args.Contains("--list");
	string serverUrl = args.Skip(1).FirstOrDefault(a => a != "--list")
		?? Environment.GetEnvironmentVariable("GAMESERVER_URL") ?? "http://localhost:5299";
	Console.Write("Enter your player name: ");
	string playerName = Console.ReadLine() is { Length: > 0 } name ? name : "Player";
	await RemoteGameClient.RunAsync(serverUrl, playerName, useListView);
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