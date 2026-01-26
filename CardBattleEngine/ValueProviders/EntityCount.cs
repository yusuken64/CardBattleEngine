namespace CardBattleEngine;

public class EntityCount : Value
{
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public override int GetValue(GameState state, ActionContext context)
	{
		return AffectedEntitySelector.Select(state, context).Count();
	}
}