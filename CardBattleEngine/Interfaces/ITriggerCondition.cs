namespace CardBattleEngine;

public interface ITriggerCondition
{
	bool Evaluate(ActionContext context);
}

public abstract class TriggerConditionBase : ITriggerCondition
{
	public abstract bool Evaluate(ActionContext context);
}