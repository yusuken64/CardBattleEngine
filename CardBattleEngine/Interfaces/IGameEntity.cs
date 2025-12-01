namespace CardBattleEngine;

public interface IGameEntity
{
	Guid Id { get; set; }
	int Health { get; set; }
	int MaxHealth { get; set; }
	bool IsAlive { get; set; }
	public IAttackBehavior AttackBehavior { get; }
	int Attack { get; set; }
	Player Owner { get; set; }
	public bool CanAttack();
	public void AddModifier(StatModifier statModifier);
	public void AddAuraModifier(StatModifier statModifier);
	public void RemoveModifier(StatModifier modifier);
	public bool HasModifier(StatModifier modifier);
	public void ClearAuras();
}
