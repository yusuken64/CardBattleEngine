namespace CardBattleEngine;

public interface IValidTargetSelector
{
	List<IGameEntity> Select(GameState gameState, Player player, Card castingCard);
}

[Flags]
public enum EntityType
{
	None = 0,
	Player = 1 << 0,
	Minion = 1 << 1,
	Card = 1 << 2,
	Weapon = 1 << 3
}

public enum Comparison
{
	LessThan,
	LessThanOrEqual,
	Equal,
	GreaterThanOrEqual,
	GreaterThan
}