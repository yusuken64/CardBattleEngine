namespace CardBattleEngine;

public class TribeOperation : ITargetOperation
{
	public MinionTribe Tribe { get; set; }
	public bool ExcludeSelf { get; set; }

	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		if (input == null)
			yield break;

		foreach (var entity in input)
		{
			if (entity is Minion minion &&
				minion.Tribes.Contains(Tribe) &&
				(!ExcludeSelf || minion != context.Source))
			{
				yield return minion;
			}
		}
	}
}
