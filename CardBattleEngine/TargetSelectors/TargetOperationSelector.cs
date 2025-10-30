namespace CardBattleEngine;

public class TargetOperationSelector : AffectedEntitySelectorBase
{
	public List<ITargetOperation> Operations { get; set; } = new();

	public override void ConsumeParams(List<ITargetOperation> paramObj)
	{
		Operations = paramObj;
	}

	public override List<SerializedOperation> EmitParams()
	{
		return Operations.Select(op => new SerializedOperation
		{
			Type = op.GetType().Name,
			Params = (Dictionary<string, object>)op.EmitParams()
		}).ToList();
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
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> actionParam);
}
public class SerializedOperation
{
	public string Type { get; set; } = null!;
	public Dictionary<string, object> Params { get; set; } = null!;
}