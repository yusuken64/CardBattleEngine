namespace CardBattleEngine;

public interface IValueProvider
{
	public int GetValue(GameState state, ActionContext context);
}
public abstract class Value : IValueProvider
{
	public abstract int GetValue(GameState state, ActionContext context);

	// Implicit conversion: int → ConstantValue
	public static implicit operator Value(int v)
		=> new ConstantValue(v);
}
