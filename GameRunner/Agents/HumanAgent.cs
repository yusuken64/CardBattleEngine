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

	// Tracks how many option rows the previous call at this same screen position drew, so a
	// shorter follow-up menu (e.g. this turn has fewer legal actions than last turn) can clear the
	// extra leftover rows a caller like RemoteGameClient - which redraws the menu at the same fixed
	// position every turn rather than ever-growing scrollback - would otherwise leave stale.
	private static int _lastOptionCount;

	public static T SelectFromList<T>(
		List<T> options,
		string prompt = "",
		Func<T, string>? display = null)
	{
		int selectedIndex = 0;

		if (!string.IsNullOrEmpty(prompt))
			Console.WriteLine(prompt);

		// Anchor to wherever the cursor actually is after the prompt, not a window-height guess -
		// callers (e.g. RemoteGameClient) may have already pinned other content above this point,
		// so "near the bottom of the window" is not necessarily "right after what we just printed".
		int optionStartTop = Console.CursorTop;

		// Reserve lines
		for (int i = 0; i < options.Count; i++)
			Console.WriteLine();

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

		Console.SetCursorPosition(0, optionStartTop + options.Count);
		return options[selectedIndex];
	}

	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}