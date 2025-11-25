namespace CardBattleEngine;

public interface IAffectedEntitySelector
{
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> Params);
	IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}

public abstract class AffectedEntitySelectorBase : IAffectedEntitySelector
{
	public virtual void ConsumeParams(Dictionary<string, object> Params)
	{
	}

	public virtual Dictionary<string, object> EmitParams()
	{
		return new();
	}

	public abstract IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}
