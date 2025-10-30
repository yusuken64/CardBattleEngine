namespace CardBattleEngine;

public class CleaveOperation : ITargetOperation
{
	public bool IncludeCenter { get; set; } = true;

	public CleaveOperation() { }
	public CleaveOperation(bool includeCenter)
	{
		IncludeCenter = includeCenter;
	}

	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		if (context.Target is not Minion target)
			yield break;

		var board = target.Owner.Board;
		int index = board.IndexOf(target);

		if (IncludeCenter)
			yield return target;

		if (index > 0)
			yield return board[index - 1];

		if (index < board.Count - 1)
			yield return board[index + 1];
	}

	// Serialization
	public Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(IncludeCenter), IncludeCenter }
		};
	}

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
		if (actionParam.TryGetValue(nameof(IncludeCenter), out var value))
			IncludeCenter = Convert.ToBoolean(value);
	}
}