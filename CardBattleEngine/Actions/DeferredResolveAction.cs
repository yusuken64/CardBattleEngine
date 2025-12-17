using System.Transactions;

namespace CardBattleEngine;

public class DeferredResolveAction : GameActionBase
{
	public IGameAction Action;
	public TargetOperationSelector AffectedEntitySelector { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;


	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		var targets = context.AffectedEntitySelector.Select(state, context);
		foreach (var target in targets)
		{
			var newContext = new ActionContext(context);
			newContext.AffectedEntitySelector = AffectedEntitySelector;
			yield return (Action, newContext);
		}
	}
}
