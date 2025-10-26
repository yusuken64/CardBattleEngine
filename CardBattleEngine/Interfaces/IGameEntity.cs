
namespace CardBattleEngine;

// Entities implement this to receive events
public interface IGameEntity
{
	Guid Id { get; set; }
	int Health { get; set; }
	bool IsAlive { get; set; }
	IAttackBehavior AttackBehavior { get; }
	int Attack { get; set; }
	Player Owner { get; set; }
	List<TriggeredEffect> TriggeredEffects { get; }
}
