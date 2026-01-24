namespace CardBattleEngine;

public class DeferredResolveAction : GameActionBase
{
	public IGameAction Action { get; set; }
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;
	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		var newContext = new ActionContext(context);
		newContext.AffectedEntitySelector = AffectedEntitySelector;
		yield return (Action, newContext);
	}
}
