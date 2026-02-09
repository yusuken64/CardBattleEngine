namespace CardBattleEngine;

public class VariableValue : IValueProvider
{
	public string VariableName { get; set; }
	public VariableScope VariableScope { get; set; }

	public int GetValue(GameState state, ActionContext context)
	{
		switch (VariableScope)
		{
			case VariableScope.Context:
				return (int)context.GetVar(VariableName);
			case VariableScope.Entity:
				return context.Source.VariableSet.GetVarOrDefault(VariableName);
		}

		return 0;
	}
}