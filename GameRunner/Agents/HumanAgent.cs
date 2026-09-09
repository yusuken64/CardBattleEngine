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

		// Step 1: select the action. GetValidActions() already produces one fully-populated
		// ActionContext per distinct action/target combination, so no further target prompt is needed.
		(IGameAction, ActionContext) selectedAction = SelectFromList(
			actions,
			"Select an action:",
			static (action) => ActionDisplay.Describe(action.Item1, action.Item2));

		return selectedAction;
	}

	// Previous call's option count, so a shorter menu can clear the extra leftover rows below it.
	private static int _lastOptionCount;

	public static T SelectFromList<T>(
		List<T> options,
		string prompt = "",
		Func<T, string>? display = null)
	{
		int selectedIndex = 0;

		if (!string.IsNullOrEmpty(prompt))
		{
			int promptRow = Console.CursorTop;
			Console.SetCursorPosition(0, promptRow);
			Console.Write(new string(' ', Console.BufferWidth - 1));
			Console.SetCursorPosition(0, promptRow);
			Console.WriteLine(prompt);
		}

		// Anchor to the current cursor, not a window-height guess - callers may already have content
		// pinned above this point. Rows below use absolute, clamped SetCursorPosition only; a plain
		// WriteLine "reserve" loop used to live here but risked a real scroll that desynced this row.
		int optionStartTop = Console.CursorTop;

		for (int i = options.Count; i < _lastOptionCount; i++)
		{
			int line = optionStartTop + i;
			if (line >= Console.BufferHeight) break;
			Console.SetCursorPosition(0, line);
			Console.Write(new string(' ', Console.BufferWidth));
		}
		_lastOptionCount = options.Count;

		while (true)
		{
			for (int i = 0; i < options.Count; i++)
			{
				int line = optionStartTop + i;
				if (line >= Console.BufferHeight) line = Console.BufferHeight - 1;

				Console.SetCursorPosition(0, line);
				Console.Write(new string(' ', Console.BufferWidth));
				Console.SetCursorPosition(0, line);

				if (i == selectedIndex)
				{
					Console.ForegroundColor = ConsoleColor.Green;
					Console.Write($"> {(display == null ? options[i] : display(options[i]))}");
					Console.ResetColor();
				}
				else
				{
					Console.Write($"  {(display == null ? options[i] : display(options[i]))}");
				}
			}

			var key = Console.ReadKey(true);
			if (key.Key == ConsoleKey.UpArrow)
				selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
			else if (key.Key == ConsoleKey.DownArrow)
				selectedIndex = (selectedIndex + 1) % options.Count;
			else if (key.Key == ConsoleKey.Enter)
				break;
		}

		Console.SetCursorPosition(0, Math.Min(optionStartTop + options.Count, Console.BufferHeight - 1));
		return options[selectedIndex];
	}

	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}