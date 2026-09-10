namespace CardBattleEngine;

public abstract class BoardPermanent : IGameEntity, ITriggerSource
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; }
	public int MaxHealth { get; set; }
	public Player Owner { get; set; }
	public bool IsAlive { get; set; }
	public List<TriggeredEffect> TriggeredEffects { get; internal set; }
	public abstract IAttackBehavior AttackBehavior { get; }
	public IGameEntity Entity => this;
	public VariableSet VariableSet { get; set; } = new();

	protected List<StatModifier> _modifiers = new();
	protected List<StatModifier> _auraModifiers = new();

	public abstract bool CanAttack();
	public abstract void RecalculateStats();
	internal abstract BoardPermanent Clone();

	public void AddModifier(StatModifier modifier)
	{
		_modifiers.Add(modifier);
		RecalculateStats();
	}

	public void AddAuraModifier(StatModifier auraStatModifier)
	{
		_auraModifiers.Add(auraStatModifier);
		RecalculateStats();
	}

	public void RemoveModifier(StatModifier modifier)
	{
		_modifiers.Remove(modifier);
		RecalculateStats();
	}

	public bool HasModifier(StatModifier modifier)
	{
		return _modifiers.Contains(modifier);
	}

	public void ClearAuras(bool skipRecalculate)
	{
		_auraModifiers.Clear();

		if (!skipRecalculate)
		{
			RecalculateStats();
		}
	}
}
