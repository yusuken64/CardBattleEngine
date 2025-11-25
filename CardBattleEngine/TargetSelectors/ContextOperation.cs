namespace CardBattleEngine;

public class ContextOperation : ITargetOperation
{
	public bool IncludeTarget { get; set; }
	public bool IncludeSource { get; set; }
	public ContextOperation() { }

	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		if (IncludeTarget)
			yield return context.Target;

		if (IncludeSource)
			yield return context.Source;
	}

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public Dictionary<string, object> EmitParams()
	{
		return [];
	}
}