
namespace CardBattleEngine;

public interface IAffectedEntitySelector
{
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> paramObj);
	IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}
public abstract class AffectedEntitySelectorBase : IAffectedEntitySelector
{
	public virtual void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public virtual Dictionary<string, object> EmitParams()
	{
		return new();
	}

	public abstract IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}
