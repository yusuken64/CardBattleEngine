namespace CardBattleEngine;

public class HealthStatusSelector : IValidTargetSelector
{
	public HealthStatus HealthStatus { get; set; }
	public IEnumerable<IGameEntity> Select(GameState gameState, Player player, Card castingCard)
	{
		IEnumerable<IGameEntity> entities = gameState.GetAllEntities();
		switch (HealthStatus)
		{
			case HealthStatus.Full:
				return entities.Where(x => x.Health == x.MaxHealth);
			case HealthStatus.Damaged:
				return entities.Where(x => x.Health < x.MaxHealth);
		}

		return [];
	}
}

public enum HealthStatus
{
	Full,
	Damaged,
}