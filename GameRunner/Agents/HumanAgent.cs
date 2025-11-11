using CardBattleEngine;

public class HumanAgent : IGameAgent
{
	private readonly Player _player;

	public HumanAgent(Player player)
	{
		_player = player;
	}


	(IGameAction, ActionContext) IGameAgent.GetNextAction(GameState state)
	{
		var actions = state.GetValidActions(_player).ToList();
		int selectedIndex = 0;

		
		// Reserve bottom lines of the console for menu
		int optionStartTop = Math.Max(0, Console.WindowTop + Console.WindowHeight - actions.Count - 1);

		for (int i = 0; i < actions.Count; i++)
		{
			Console.WriteLine();
		}
		while (true)
		{
			// Draw each option in place
			for (int i = 0; i < actions.Count; i++)
			{
				int line = optionStartTop + i;
				if (line >= Console.BufferHeight) line = Console.BufferHeight - 1;

				Console.SetCursorPosition(0, line);

				// Clear the line first
				Console.Write(new string(' ', Console.BufferWidth));
				Console.SetCursorPosition(0, line);

				// Draw the option
				if (i == selectedIndex)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write($"> {actions[i]}");
					Console.ResetColor();
				}
				else
				{
					Console.Write($"  {actions[i]}");
				}
			}

			// Read input
			var key = Console.ReadKey(true);
			if (key.Key == ConsoleKey.UpArrow)
				selectedIndex = (selectedIndex - 1 + actions.Count) % actions.Count;
			else if (key.Key == ConsoleKey.DownArrow)
				selectedIndex = (selectedIndex + 1) % actions.Count;
			else if (key.Key == ConsoleKey.Enter)
				break;
		}

		// Move cursor below menu
		Console.SetCursorPosition(0, optionStartTop + actions.Count);
		return actions[selectedIndex];
	}
	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}