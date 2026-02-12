namespace CardBattleEngine;

public class VariableValue : IValueProvider
{
	public string VariableName { get; set; }
	public VariableScope VariableScope { get; set; }

	public int GetValue(GameState state, ActionContext context)
	{
		if (context == null)
			return 0;

		switch (VariableScope)
		{
			case VariableScope.Context:
				return GetIntSafely(context.GetVar(VariableName));

			case VariableScope.Entity:
				if (context.Source?.VariableSet == null)
					return 0;

				return context.Source.VariableSet.GetVarOrDefault(VariableName);

			default:
				return 0;
		}
	}

	private int GetIntSafely(object value)
	{
		if (value == null)
			return 0;

		if (value is int i)
			return i;

		// Optional: allow numeric coercion if your engine benefits from it
		if (value is float f) return (int)f;
		if (value is double d) return (int)d;

		// At this point it's a real bug. Log it.
		throw new InvalidOperationException(
			$"Variable '{VariableName}' is not an int. Type was {value.GetType().Name}");
	}
}