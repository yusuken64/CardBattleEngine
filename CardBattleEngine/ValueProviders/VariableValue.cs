namespace CardBattleEngine;

public class VariableValue : IValueProvider
{
	public string VariableName;

	public int GetValue(GameState state, ActionContext context)
	{
		return (int)context.GetVar(VariableName);
	}
}