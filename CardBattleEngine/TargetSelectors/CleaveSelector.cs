namespace CardBattleEngine;

public class CleaveSelector : AffectedEntitySelectorBase
{
	public bool IncludeCenter { get; set; } = true;

	public override IEnumerable<IGameEntity> Select(GameState state, ActionContext context)
	{
		if (context.Target is not Minion target)
			yield break;

		var board = target.Owner.Board;
		int index = board.IndexOf(target);

		if (IncludeCenter)
			yield return target;

		if (index > 0) yield return board[index - 1];
		if (index < board.Count - 1) yield return board[index + 1];
	}
}
