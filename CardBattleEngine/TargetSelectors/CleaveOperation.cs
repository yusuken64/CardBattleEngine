using System.Collections.Generic;

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

public class AdjacentOperation : ITargetOperation
{
	public bool UsePlayIndexInstead { get; set; } = true;
	public bool IncludeCenter { get; set; } = true;

	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		if (input.ToList()[0] is not Minion target)
		{
			yield break;
		}

		var board = target.Owner.Board;


		if (IncludeCenter)
		{
			yield return target;
		}

		int index;
		if (UsePlayIndexInstead)
		{
			//at this point the summoned minion is not added to the board yet
			int requested = context.PlayIndex;
			int clampedIndex =
				(requested == -1 || requested < 0 || requested > board.Count)
				? board.Count : requested;

			if (clampedIndex > 0)
				yield return board[clampedIndex - 1];

			if (clampedIndex < board.Count)
				yield return board[clampedIndex];
		}
		else
		{
			index = board.IndexOf(target);
			if (index > 0)
				yield return board[index - 1];

			if (index < board.Count - 1)
				yield return board[index + 1];
		}


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
