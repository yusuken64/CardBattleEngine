namespace CardBattleEngine;

public class NegativeValue : Value
{
	public IValueProvider OriginalValue { get; set; }
	public override int GetValue(GameState state, ActionContext context)
	{
		if (OriginalValue == null)
			return 0;

		return -OriginalValue.GetValue(state, context);
	}
}
