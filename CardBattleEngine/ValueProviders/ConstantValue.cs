namespace CardBattleEngine;

public class ConstantValue : Value
{
	public int Number { get; }

	public ConstantValue(int number) => Number = number;

	public override int GetValue(GameState state, ActionContext context)
		=> Number;
}
