namespace CardBattleEngine;

public interface IAffectedEntitySelector
{
	TargetResolutionTiming ResolutionTiming { get; set; }
	IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}

public abstract class AffectedEntitySelectorBase : IAffectedEntitySelector
{
	public TargetResolutionTiming ResolutionTiming { get; set; }

	public abstract IEnumerable<IGameEntity> Select(GameState state, ActionContext context);
}
