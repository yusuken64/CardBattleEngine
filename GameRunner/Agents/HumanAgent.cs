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

		if (state.PendingChoice != null)
		{
			// Pending-choice actions (mulligan toggles, Discover picks, ...) aren't verified to
			// group safely by (ActionType, Source) - fall back to the flat picker.
			return SelectFromList(
				actions,
				"Select an action:",
				static (action) => ActionDisplay.Describe(action.Item1, action.Item2));
		}

		// Step 1: pick the action/source ("Attack with Wisp"). Step 2, only if that source has
		// more than one legal target, picks which one.
		return SelectGrouped(
			actions,
			keySelector: static (a) => (a.Item1.GetType(), a.Item2.Source?.Id),
			groupLabel: static (key, items) => ActionDisplay.DescribeGroup(items[0].Item1, items[0].Item2),
			// Stage-2 text doesn't vary by target for non-Attack actions (ActionDisplay.Describe
			// falls back to ToString(), which ignores context.Target) - a pre-existing ambiguity,
			// not introduced here.
			itemLabel: static (a) => ActionDisplay.Describe(a.Item1, a.Item2),
			stage1Prompt: "Select an action:",
			stage2Prompt: "Select a target:",
			rowsDrawn: out _);
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

	// Groups options by keySelector, shows one row per group (groupLabel), then - only if the
	// chosen group has more than one item - shows a second SelectFromList over just that group's
	// items (itemLabel). Singleton groups return immediately with no stage 2. rowsDrawn reports
	// the total console rows both stages actually occupied, for callers that track leftover-row
	// cleanup instead of guessing before the fact.
	public static T SelectGrouped<T, TKey>(
		List<T> options,
		Func<T, TKey> keySelector,
		Func<TKey, List<T>, string> groupLabel,
		Func<T, string> itemLabel,
		string stage1Prompt,
		string stage2Prompt,
		out int rowsDrawn)
	{
		var groups = options
			.GroupBy(keySelector)
			.Select(g => new { g.Key, Items = g.ToList() })
			.ToList();

		int anchorRow = Console.CursorTop;
		var chosenGroup = SelectFromList(groups, stage1Prompt, g => groupLabel(g.Key, g.Items));

		if (chosenGroup.Items.Count == 1)
		{
			rowsDrawn = 1 + groups.Count;
			return chosenGroup.Items[0];
		}

		// Stage 2 redraws at the same anchor as stage 1 instead of stacking below it - otherwise
		// the combined footprint of both stages can run off the bottom of the console. Resetting
		// to the shared anchor also means SelectFromList's own leftover-row clearing (built for
		// "this call's list is shorter than last call's") handles the stage 1 -> stage 2 shrink for
		// free, same as it already does across turns.
		Console.SetCursorPosition(0, anchorRow);
		var chosenItem = SelectFromList(chosenGroup.Items, stage2Prompt, itemLabel);
		rowsDrawn = 1 + chosenGroup.Items.Count;
		return chosenItem;
	}

	public void OnGameEnd(GameState gamestate, bool win)
	{
	}
}