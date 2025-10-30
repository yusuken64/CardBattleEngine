namespace CardBattleEngine;

public interface ITriggerCondition
{
	bool Evaluate(EffectContext context); 
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> actionParam);
}

public abstract class TriggerConditionBase : ITriggerCondition
{
	public abstract bool Evaluate(EffectContext context);
	public virtual void ConsumeParams(Dictionary<string, object> actionParam) { }
	public virtual Dictionary<string, object> EmitParams() { return new(); }
}

public class EffectContext
{
	public IGameEntity EffectOwner { get; internal set; }
	public Minion SummonedUnit { get; set; }
	public IGameEntity SecretOwner { get; internal set; }
	public IGameAction TriggeringAction { get; internal set; }
	public Player OriginalOwner { get; internal set; }
}
