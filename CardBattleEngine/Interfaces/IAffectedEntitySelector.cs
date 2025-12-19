namespace CardBattleEngine;

public interface IAffectedEntitySelector
{
	TargetResolutionTiming ResolutionTiming { get; set; }
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> Params);
	IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}

public abstract class AffectedEntitySelectorBase : IAffectedEntitySelector
{
	public TargetResolutionTiming ResolutionTiming { get; set; }

	public virtual void ConsumeParams(Dictionary<string, object> Params)
	{
	}

	public virtual Dictionary<string, object> EmitParams()
	{
		return new();
	}

	public abstract IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}
