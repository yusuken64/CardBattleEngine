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
		var actions = state.GetUntargetedActions(_player).ToList();

		// Step 1: select the action
		(IGameAction, ActionContext) selectedAction = SelectFromList(
			actions,
			"Select an action:",
			static (action) => action.Item1.ToString());

		var context = new ActionContext();

		// Step 2: if the action requires a target, prompt for one
		//if (selectedAction.Item1 is PlayCardAction playCardAction &&
		//	playCardAction.Card.TriggeredEffects.Any(x => x.TargetType != TargetingType.None))
		//{
		//	var effect = playCardAction.Card.TriggeredEffects[0];
		//	var possibleTargets = state.GetValidTargets(_player, effect.TargetType).ToList();
		//	if (possibleTargets.Count > 0)
		//	{
		//		var selectedTarget = SelectFromList(possibleTargets, "Select a target:");
		//		context.SourcePlayer = _player;
		//		context.SourceCard = playCardAction.Card;
		//		context.Target = selectedTarget as IGameEntity;
		//	}
		//}
		//else if (selectedAction.Item1 is AttackAction attackAction)
		//{
		//	//var effect = attackAction.Card.TriggeredEffects[0];
		//	var possibleTargets = state.GetValidTargets(_player, TargetingType.AnyEnemy).ToList();
		//	if (possibleTargets.Count > 0)
		//	{
		//		var selectedTarget = SelectFromList(possibleTargets, "Select a target:");
		//		context.SourcePlayer = _player;
		//		context.SourceCard = null;
		//		context.Source = selectedAction.Item2.Source;
		//		context.Target = selectedTarget as IGameEntity;
		//	}
		//}
		//else
		//{
		//	context .SourcePlayer = _player;
		//}

		selectedAction.Item2 = context;
		return selectedAction;
	}

	private static T SelectFromList<T>(
		List<T> options,
		string prompt = "",
		Func<T, string>? display = null)
	{
		int selectedIndex = 0;

		int optionStartTop = Math.Max(0, Console.WindowTop + Console.WindowHeight - options.Count - 1);

		if (!string.IsNullOrEmpty(prompt))
			Console.WriteLine(prompt);

		// Reserve lines
		for (int i = 0; i < options.Count; i++)
			Console.WriteLine();

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