namespace CardBattleEngine;

public interface ITriggerCondition
{
	bool Evaluate(ActionContext context); 
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> actionParam);
}

public abstract class TriggerConditionBase : ITriggerCondition
{
	public abstract bool Evaluate(ActionContext context);
	public virtual void ConsumeParams(Dictionary<string, object> actionParam) { }
	public virtual Dictionary<string, object> EmitParams() { return new(); }
}