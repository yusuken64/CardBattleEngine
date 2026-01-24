namespace CardBattleEngine;

public class StatSelector : IValidTargetSelector
{
	public Stat Stat { get; set; }
	public Comparison Comparison { get; set; }
	public int Value { get; set; }

	public List<IGameEntity> Select(
		GameState gameState,
		Player player,
		Card castingCard)
	{
		return gameState.GetAllEntities()
			.Where(entity =>
				TryGetEntityValue(entity, Stat, out int entityValue) &&
				Compare(entityValue, Value, Comparison))
			.ToList();
	}

	private bool TryGetEntityValue(IGameEntity entity, Stat stat, out int value)
	{
		value = 0;

		switch (stat)
		{
			case Stat.Attack:
				if (entity is Minion minion)
				{
					value = minion.Attack;
					return true;
				}
				return false;
			case Stat.Health:
				if (entity is Minion minion2)
				{
					value = minion2.Health;
					return true;
				}
				return false;

			case Stat.Cost:
				if (entity is Card card)
				{
					value = card.ManaCost;
					return true;
				}
				return false;

			default:
				return false;
		}
	}

	private bool Compare(int left, int right, Comparison comparison)
	{
		return comparison switch
		{
			Comparison.LessThan => left < right,
			Comparison.LessThanOrEqual => left <= right,
			Comparison.Equal => left == right,
			Comparison.GreaterThanOrEqual => left >= right,
			Comparison.GreaterThan => left > right,
			_ => false
		};
	}
}
