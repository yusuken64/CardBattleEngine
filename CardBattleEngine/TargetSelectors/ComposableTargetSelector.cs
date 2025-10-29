using System.Text.Json.Serialization;

namespace CardBattleEngine;

public class ComposableTargetSelector : AffectedEntitySelectorBase
{
	public IAffectedEntitySelector BaseSelector { get; set; }         // e.g., EnemyMinionsSelector
	public ITargetFilter Filter { get; set; }                // optional
	public ITargetSorter Sorter { get; set; }                // optional
	public TargetSelectionMode SelectionMode { get; set; } = TargetSelectionMode.All;
	public int Count { get; set; } = -1;                     // -1 = all

	public override IEnumerable<IGameEntity> Select(GameState state, ActionContext context)
	{
		IRNG rng = null;
		var targets = BaseSelector.Select(state, context);

		if (Filter != null)
			targets = targets.Where(t => Filter.IsValid(t, state, context));

		if (Sorter != null)
			targets = Sorter.Sort(targets, state, context);

		switch (SelectionMode)
		{
			case TargetSelectionMode.FirstN:
				targets = targets.Take(Count > 0 ? Count : targets.Count());
				break;

			case TargetSelectionMode.RandomN:
				if (rng != null && Count > 0)
					targets = targets.OrderBy(_ => rng.NextInt(int.MaxValue)).Take(Count);
				break;

			case TargetSelectionMode.All:
			default:
				break;
		}

		return targets;
	}
}

public interface ITargetSorter
{
	IEnumerable<IGameEntity> Sort(IEnumerable<IGameEntity> targets, GameState state, ActionContext context);
}

public interface ITargetFilter
{
	bool IsValid(IGameEntity t, GameState state, ActionContext context);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetSelectionMode
{
	All,
	FirstN,
	RandomN
}