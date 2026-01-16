
namespace CardBattleEngine;

public class SequentialAction : GameActionBase
{
	public List<SequentialEffect> Effects { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		foreach (var effect in Effects)
		{
			foreach (var action in effect.GameActions)
			{
				context.AffectedEntitySelector = effect.AffectedEntitySelector;
				yield return (action, context);
			}
		}
	}
}

public class SequentialEffect
{
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public List<IGameAction> GameActions { get; set; } = new();
}
