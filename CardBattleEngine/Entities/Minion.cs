using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

public class Minion : IGameEntity, ITriggerSource
{
	public MinionCard OriginalCard { get; private set; }
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; }
	public string TemplateName { get; set; }
	public int Attack { get; set; }
	public int Health {	get; set; }
	public int MaxHealth { get; set; }
	public List<MinionTribe> Tribes { get; set; }
	public Player Owner { get; set; }
	public bool Taunt { get; set; }
	public bool HasSummoningSickness { get; internal set; }
	public IEnumerable<(TriggeredEffect, StatModifier)> ModifierTriggeredEffects
	{
		get
		{
			// Also return a triggered effect for each temporary modifier
			foreach (var mod in _modifiers.Where(m => m.ExpirationTrigger != null))
			{
				yield return (new TriggeredEffect
				{
					EffectTrigger = EffectTrigger.OnTurnEnd,
					EffectTiming = EffectTiming.Pre,
					TargetType = TargetingType.Self,
					GameActions = new List<IGameAction>
					{
						new RemoveModifierAction()
					}
				}, mod);
			}
		}
	}
	public IEnumerable<TriggeredEffect> TriggeredEffects { get; }

	private IAttackBehavior _attackBehavior;
	public IAttackBehavior AttackBehavior
	{
		get
		{
			return _attackBehavior;
		}
	}
	private List<StatModifier> _modifiers = new();
	private List<StatModifier> _auraModifiers = new();

	public bool IsAlive{ get; set; }
	public bool IsFrozen { get; internal set; }
	public bool MissedAttackFromFrozen { get; internal set; }
	public bool IsStealth { get; internal set; }
	public bool HasCharge { get; internal set; }
	public bool HasDivineShield { get; internal set; }
	public bool HasPoisonous { get; internal set; }
	public bool HasRush { get; set; }
	public bool HasWindfury { get; set; }
	public bool HasLifeSteal { get; set; }
	public bool HasReborn { get; set; }
	public int AttacksPerformedThisTurn { get; internal set; }

	public Minion(MinionCard card, Player owner)
	{
		this.OriginalCard = card;
		Owner = owner;

		Name = card.Name;
		Attack = card.Attack;
		MaxHealth = card.Health;
		Health = card.Health;
		Tribes = (card.MinionTribes ?? [MinionTribe.None]).ToList();

		_attackBehavior = new MinionAttackBehavior();
		AttacksPerformedThisTurn = 0;
		HasSummoningSickness = true;
		IsStealth = card.IsStealth;
		HasCharge = card.HasCharge;
		HasDivineShield = card.HasDivineShield;
		HasPoisonous = card.HasPoisonous;
		Taunt = card.HasTaunt;
		HasRush = card.HasRush;
		HasWindfury = card.HasWindfury;
		HasLifeSteal = card.HasLifeSteal;
		HasReborn = card.HasReborn;

		IsAlive = true;
		TriggeredEffects = card.TriggeredEffects.Select(effect =>
		{
			var instance = effect.CloneFor(this);
			return instance;
		});
	}

	public bool CanAttack()
	{
		var maxAttacks = _attackBehavior.MaxAttacks(this);
		return (HasCharge || HasRush || !HasSummoningSickness) && (AttacksPerformedThisTurn < maxAttacks);
	}

	internal Minion Clone()
	{
		return new Minion(this.OriginalCard, Owner)
		{
			Id = this.Id,
			TemplateName = this.TemplateName,
			Attack = this.Attack,
			MaxHealth = this.MaxHealth,
			Health = this.Health,
			Owner = this.Owner,
			Taunt = this.Taunt,
			HasSummoningSickness = this.HasSummoningSickness,
			AttacksPerformedThisTurn = this.AttacksPerformedThisTurn
		};
	}
	internal void AddModifier(StatModifier modifier)
	{
		_modifiers.Add(modifier);
		RecalculateStats();
	}

	internal void AddAuraModifier(StatModifier auraStatModifier)
	{
		_auraModifiers.Add(auraStatModifier);
		RecalculateStats();
	}

	internal void RemoveModifier(StatModifier modifier)
	{
		_modifiers.Remove(modifier);
		RecalculateStats();
	}

	private void RecalculateStats()
	{
		// Before recalculating, compute how much damage the unit has taken.
		// (damageTaken = MaxHealth_before - Health_before)
		int oldDamageTaken = MaxHealth - Health;
		if (oldDamageTaken < 0)
			oldDamageTaken = 0; // safety for weird states

		// --- Rebuild base stats ---
		Attack = OriginalCard.Attack;
		MaxHealth = OriginalCard.Health; // start from base max health

		// Apply modifiers
		foreach (var mod in _modifiers)
		{
			Attack += mod.AttackChange;
			MaxHealth += mod.HealthChange;
		}

		foreach (var mod in _auraModifiers)
		{
			Attack += mod.AttackChange;
			MaxHealth += mod.HealthChange;
		}

		// Clamp final stats
		Attack = Math.Max(0, Attack);
		MaxHealth = Math.Max(0, MaxHealth);

		// --- Reapply damage taken ---
		int newHealth = MaxHealth - oldDamageTaken;

		// Clamp health to valid range
		Health = Utils.Clamp(newHealth, 0, MaxHealth);
	}

	internal bool HasModifier(StatModifier modifier)
	{
		return _modifiers.Contains(modifier);
	}

	internal void ClearAuras()
	{
		_auraModifiers.Clear();
		RecalculateStats();
	}
}

[JsonConverter(typeof(StringEnumConverter))]
public enum MinionTribe
{
	None,
	All,
	Murloc
}