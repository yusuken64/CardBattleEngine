﻿
using CardBattleEngine.Actions;

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
	public int CurrentMana { get; set; }
	public int MaxMana { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; } = 30;
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
	IEnumerable<TriggeredEffect> ITriggerSource.TriggeredEffects => TriggeredEffects;
	public bool IsFrozen { get; internal set; }
	public bool HasAttackedThisTurn { get; set; }
	public bool MissedAttackFromFrozen { get; internal set; }
	public bool IsStealth { get; internal set; }
	public Weapon? EquippedWeapon { get; set; }
	private List<StatModifier> _modifiers = new();

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
			CurrentMana = CurrentMana,
			MaxMana = MaxMana,
			Health = Health,
			Mana = Mana,
			//EquippedWeapon = EquippedWeapon.Clone,
			HasAttackedThisTurn = HasAttackedThisTurn,
			IsAlive = IsAlive
		};

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

	internal void EquipWeapon(Weapon weapon)
	{
		this.EquippedWeapon = weapon;
		RecalculateStats();
	}

	internal void UnequipWeapon()
	{
		this.EquippedWeapon = null;
		RecalculateStats();
	}

	private void RecalculateStats()
	{
		Attack = EquippedWeapon?.Attack ?? 0;
		//Health = card.Health;

		foreach (var mod in _modifiers)
		{
			Attack += mod.AttackChange;
			//Health += mod.HealthChange;
		}

		Attack = Math.Max(0, Attack);
		//Health = Math.Max(0, Health);
	}
}