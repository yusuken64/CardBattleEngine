namespace CardBattleEngine;

public class SourceIsTargetCondition : TriggerConditionBase
{
	public override bool Evaluate(ActionContext context)
	{
		return context.Source == context.Target;
	}
}
