namespace CardBattleEngine;

public class TargetOperationSelector : AffectedEntitySelectorBase
{
	public List<ITargetOperation> Operations { get; set; } = new();

	public void ConsumeParams(Dictionary<string, object> paramObj)
	{
		throw new NotImplementedException();
	}

	public Dictionary<string, object> EmitParams()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<IGameEntity> Select(GameState state, ActionContext context)
	{
		IEnumerable<IGameEntity> targets = Enumerable.Empty<IGameEntity>();

		foreach (var op in Operations)
			targets = op.Apply(targets, state, context);

		return targets;
	}
}

public interface ITargetOperation
{
	IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context);
}
