namespace CardBattleEngine;

public class Weapon : ITriggerSource, IGameEntity
{
	public string Name { get; set; }
	public int Attack { get; set; }
	public int Durability { get; set; }
	public Player Owner { get; set; }
	public IGameEntity Entity => Owner;
	public List<TriggeredEffect> TriggeredEffects { get; set; }
	public Guid Id { get; set; } = Guid.NewGuid();
	public int Health { get { return Durability; } set { Durability = value; } }
	public int MaxHealth { get; set; }
	public bool IsAlive { get; set; }
	public int OriginalAttack { get; }
	public int OriginalDurability { get; }

	public IAttackBehavior AttackBehavior => (Owner as IGameEntity).AttackBehavior;

	public WeaponCard OriginalCard { get; internal set; }

	private List<StatModifier> _modifiers = new();
	private List<StatModifier> _auraModifiers = new();

	public Weapon(string name, int attack, int durability)
	{
		Name = name;
		Attack = attack;
		Durability = durability;
		OriginalAttack = attack;
		OriginalDurability = durability;
	}

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

	public bool CanAttack()
	{
		return Owner.CanAttack();
	}

	public bool HasModifier(StatModifier modifier)
	{
		return _modifiers.Contains(modifier);
	}

	public void ClearAuras()
	{
		_auraModifiers.Clear();
		RecalculateStats();
	}

	public void RecalculateStats()
	{
		// Before recalculating, compute how much damage the unit has taken.
		// (damageTaken = MaxHealth_before - Health_before)
		int oldDamageTaken = MaxHealth - Health;
		if (oldDamageTaken < 0)
			oldDamageTaken = 0; // safety for weird states

		// --- Rebuild base stats ---
		var attack = OriginalAttack;
		var maxHealth = OriginalDurability; // start from base max health

		foreach (var mod in _modifiers.Concat(_auraModifiers))
		{
			mod.ApplyValue(ref attack, mod.AttackChange);
			mod.ApplyValue(ref maxHealth, mod.HealthChange);
		}

		// Clamp final stats
		Attack = Math.Max(0, attack);
		MaxHealth = Math.Max(0, maxHealth);

		// --- Reapply damage taken ---
		int newHealth = MaxHealth - oldDamageTaken;

		// Clamp health to valid range
		Health = Utils.Clamp(newHealth, 0, MaxHealth);
		IsAlive = Durability > 0;

		Owner.RecalculateStats();
	}

	internal Weapon Clone()
	{
		return new Weapon(Name, OriginalAttack, OriginalDurability)
		{
			Attack = Attack,
			Durability = Durability,
			Owner = Owner,
			TriggeredEffects = TriggeredEffects,
			OriginalCard = OriginalCard,
		};
	}
}
