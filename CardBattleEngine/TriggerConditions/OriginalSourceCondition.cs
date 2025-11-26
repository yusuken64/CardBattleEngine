namespace CardBattleEngine;

public class OriginalSourceCondition : TriggerConditionBase
{
	public override bool Evaluate(ActionContext context)
	{
		return context.Source == context.OriginalSource;
	}
}
