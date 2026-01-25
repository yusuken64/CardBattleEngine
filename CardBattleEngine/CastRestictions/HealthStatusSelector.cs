namespace CardBattleEngine;

public class HealthStatusSelector : IValidTargetSelector
{
	public HealthStatus HealthStatus { get; set; }
	public List<IGameEntity> Select(GameState gameState, Player player, Card castingCard)
	{
		IEnumerable<IGameEntity> entities = gameState.GetAllEntities();
		switch (HealthStatus)
		{
			case HealthStatus.Full:
				return entities.Where(x => x.Health == x.MaxHealth).ToList();
			case HealthStatus.Damaged:
				return entities.Where(x => x.Health < x.MaxHealth).ToList();
		}

		return [];
	}
}

public enum HealthStatus
{
	Full,
	Damaged,
}