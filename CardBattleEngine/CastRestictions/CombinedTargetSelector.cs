namespace CardBattleEngine;

public class CombinedTargetSelector : IValidTargetSelector
{
	public IValidTargetSelector Left { get; set; }
	public CombinationOperation Operation { get; set; }
	public IValidTargetSelector Right { get; set; }

	public List<IGameEntity> Select(GameState gameState, Player player, Card castingCard)
	{
		var leftResults = Left.Select(gameState, player, castingCard);
		var rightResults = Right.Select(gameState, player, castingCard);

		switch (Operation)
		{
			case CombinationOperation.And:
				return leftResults
					.Intersect(rightResults)
					.ToList();

			case CombinationOperation.Or:
				return leftResults
					.Union(rightResults)
					.ToList();

			case CombinationOperation.Except:
				return leftResults
					.Except(rightResults)
					.ToList();

			default:
				throw new ArgumentOutOfRangeException(nameof(Operation), Operation, null);
		}
	}
}

public enum CombinationOperation
{
	And,
	Or,
	Except,
}