namespace CardBattleEngine;

public class CastCardCostValue : Value
{
	public override int GetValue(GameState state, ActionContext context)
	{
		return context.SourceCard.ManaCost;
	}
}
