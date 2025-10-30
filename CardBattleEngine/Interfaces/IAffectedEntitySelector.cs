namespace CardBattleEngine;

public interface IAffectedEntitySelector
{
	List<SerializedOperation> EmitParams();
	void ConsumeParams(List<ITargetOperation> paramObj);
	IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}

public abstract class AffectedEntitySelectorBase : IAffectedEntitySelector
{
	public virtual void ConsumeParams(List<ITargetOperation> actionParam)
	{
	}

	public virtual List<SerializedOperation> EmitParams()
	{
		return new();
	}

	public abstract IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}
