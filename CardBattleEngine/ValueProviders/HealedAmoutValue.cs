namespace CardBattleEngine;

public class HealedAmoutValue : Value
{
	public override int GetValue(GameState state, ActionContext context)
	{
		return context.HealedAmount;
	}
}
