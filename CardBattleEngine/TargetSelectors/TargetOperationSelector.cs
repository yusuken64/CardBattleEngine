using System.Linq;

namespace CardBattleEngine;
public class TargetOperationSelector : AffectedEntitySelectorBase
{
	public List<ITargetOperation> Operations { get; set; } = new();

	// Serialize to:  { "Operations": [ { Type = "...", Params = {...} }, ... ] }
	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			["Operations"] = Operations
				.Select(op => new SerializedOperation
				{
					Type = op.GetType(),
					Params = op.EmitParams()
				})
				.ToList()
		};
	}

	// Deserialize from dictionary:
	// Params["Operations"] -> List<SerializedOperation>
	public override void ConsumeParams(Dictionary<string, object> Params)
	{
		if (!Params.TryGetValue("Operations", out var opsObj) || opsObj is not List<SerializedOperation> opList)
		{
			Operations = new List<ITargetOperation>();
			return;
		}

		Operations = opList
			.Select(serializedOperation =>
			{
				var op = (ITargetOperation)Activator.CreateInstance(serializedOperation.Type)!;

				op.ConsumeParams(JsonParamHelper.Normalize(serializedOperation.Params));

				return op;
			})
			.ToList();
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
	public Type Type { get; set; } = null!;
	public Dictionary<string, object> Params { get; set; } = null!;
}