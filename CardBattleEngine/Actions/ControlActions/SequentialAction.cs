
namespace CardBattleEngine;

public class SequentialAction : ControlActionBase
{
	public override EffectTrigger EffectTrigger => throw new NotImplementedException();

	public override bool IsValid(GameState state, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		foreach (var action in ChildActions)
		{
			yield return (action, context);
		}
	}
}