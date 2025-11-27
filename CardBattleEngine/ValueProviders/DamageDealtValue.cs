namespace CardBattleEngine;

public class DamageDealtValue : Value
{
	public override int GetValue(GameState state, ActionContext context)
	{
		return context.DamageDealt;
	}
}