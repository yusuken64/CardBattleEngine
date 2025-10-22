namespace CardBattleEngine;

// Entities implement this to receive events
public interface IGameEntity
{
	Guid Id { get; }
	int Health { get; set; }
	bool IsAlive { get; set; }
	IAttackBehavior AttackBehavior { get; }
	int Attack { get; }
	Player Owner { get; }
}
