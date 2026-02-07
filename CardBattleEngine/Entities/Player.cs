namespace CardBattleEngine;

public class Player : IGameEntity, ITriggerSource
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; }
	public List<Card> Deck { get; } = new List<Card>();
	public List<Card> Hand { get; } = new List<Card>();
	public List<Minion> Board { get; } = new List<Minion>();
	public List<Minion> Graveyard { get; } = new List<Minion>();
	public List<Secret> Secrets { get; set; } = new List<Secret>();//TODO expand to hero auras
	public int MaxMana { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; } = 30;
	public int MaxHealth { get; set; } = 30;
	public int Armor { get; set; }
	public int Mana { get; set; }
	public bool CanAttack()
	{
		return Attack > 0 && !HasAttackedThisTurn;
	}

	private IAttackBehavior _attackBehavior;
	IAttackBehavior IGameEntity.AttackBehavior
	{
		get
		{
			return _attackBehavior;
		}
	}

	public Player Owner { get; set; }

	public bool IsAlive { get; set; }
	public List<TriggeredEffect> TriggeredEffects { get; }
	public bool IsFrozen { get; internal set; }
	public bool HasAttackedThisTurn { get; set; }
	public bool MissedAttackFromFrozen { get; internal set; }
	public bool IsStealth { get; internal set; }
	public Weapon? EquippedWeapon { get; set; }
	public HeroPower HeroPower { get; set; }
	public IGameEntity Entity => this;

	public int Fatigue { get; internal set; } = 0;

	public Player(string name) 
	{
		Name = name;
		IsAlive = true;
		Owner = this;
		TriggeredEffects = new();
		_attackBehavior = new HeroAttackBehavior();
	}

	public Player Clone()
	{
		var clone = new Player(Name)
		{
			Id = Id,
			MaxMana = MaxMana,
			Health = Health,
			MaxHealth = MaxHealth,
			Mana = Mana,
			//EquippedWeapon = EquippedWeapon.Clone,
			HasAttackedThisTurn = HasAttackedThisTurn,
			IsAlive = IsAlive,
			IsFrozen = IsFrozen,
			Fatigue = Fatigue,
		};

		clone.EquippedWeapon = EquippedWeapon?.Clone();

		// Deep copy the collections
		foreach (var card in Deck)
			clone.Deck.Add(card.Clone());

		foreach(var card in clone.Deck)
		{
			card.Owner = clone;
		}

		foreach (var card in Hand)
			clone.Hand.Add(card.Clone());

		foreach (var card in clone.Hand)
		{
			card.Owner = clone;
		}

		foreach (var minion in Board)
			clone.Board.Add(minion.Clone());

		foreach (var minion in clone.Board)
		{
			minion.Owner = clone;
		}

		foreach (var minion in Graveyard)
			clone.Graveyard.Add(minion.Clone());

		foreach (var minion in clone.Graveyard)
		{
			minion.Owner = clone;
		}

		return clone;
	}

	public void EquipWeapon(Weapon weapon)
	{
		weapon.Owner = this;
		this.EquippedWeapon = weapon;
		this.EquippedWeapon.RecalculateStats();
	}

	internal void UnequipWeapon()
	{
		this.EquippedWeapon = null;
		RecalculateStats();
	}

	internal void RecalculateStats()
	{
		var attack = EquippedWeapon?.Attack ?? 0;
		//Health = card.Health;

		foreach (var mod in _modifiers.Concat(_auraModifiers))
		{
			mod.ApplyValue(ref attack, mod.AttackChange);
		}

		Attack = Math.Max(0, attack);
		//Health = Math.Max(0, Health);
	}

	internal List<StatModifier> _modifiers = new();
	internal List<StatModifier> _auraModifiers = new();

	public void AddModifier(StatModifier modifier)
	{
		_modifiers.Add(modifier);
		RecalculateStats();
	}

	public void AddAuraModifier(StatModifier auraStatModifier)
	{
		_auraModifiers.Add(auraStatModifier);
		//RecalculateStats();
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

	void IGameEntity.RecalculateStats()
	{
		RecalculateStats();
	}
}