using CardBattleEngine;

namespace GameRunner;

internal class Tournament
{
	private readonly Func<GameEngine, GameState, int, (IGameAgent player1, IGameAgent player2)> _createAgents;

	public Tournament(Func<GameEngine, GameState, int, (IGameAgent player1, IGameAgent player2)> createAgents)
	{
		_createAgents = createAgents;
	}

	public void Run(int runs)
	{
		int player1Win = 0;
		int player2Win = 0;

		for (int i = 0; i < runs; i++)
		{
			var gameState = GameFactory.CreateTestGame();
			var engine = new GameEngine(new XorShiftRNG(1));
			//engine.ActionCallback = GameEngine.PrintState;

			(IGameAgent player1, IGameAgent player2) = _createAgents(engine, gameState, i);

			engine.StartGame(gameState);
			while (!gameState.IsGameOver())
			{
				//GameEngine.PrintState(gameState, null);
				var currentAgent = gameState.CurrentPlayer == gameState.Players[0] ? player1 : player2;
				var action = currentAgent.GetNextAction(gameState);
				engine.Resolve(gameState, gameState.CurrentPlayer, gameState.OpponentPlayer, action);
			}

			GameEngine.PrintState(gameState, null);
			if (gameState.Winner == gameState.Players[0])
			{
				player1Win++;
				player1.OnGameEnd(gameState, true);
				player2.OnGameEnd(gameState, false);
			}
			else if (gameState.Winner == gameState.Players[1])
			{
				player2Win++;
				player1.OnGameEnd(gameState, false);
				player2.OnGameEnd(gameState, true);
			}
			Console.WriteLine($"Winner: {gameState.Winner?.Name}");
		}

		Console.WriteLine($"p1 Win {player1Win}, {(float)player1Win / runs}");
		Console.WriteLine($"p2 Win {player2Win}, {(float)player2Win / runs}");
	}
}
