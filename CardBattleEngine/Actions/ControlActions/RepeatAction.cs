
namespace CardBattleEngine;

public class RepeatAction : ControlActionBase
{
	public IValueProvider Count { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		int count = Count.GetValue(state, context);

		for (int i = 0; i < count; i++)
		{
			foreach (var action in ChildActions)
			{
				yield return (action, new ActionContext(context)
				{
					AffectedEntitySelector = context.AffectedEntitySelector
				});
			}
		}
	}
}