using System.Text.Json.Serialization;

namespace CardBattleEngine;

public class Minion : IGameEntity, ITriggerSource
{
	private MinionCard card;
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; }
	public string TemplateName { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; }
	public List<MinionTribe> Tribes { get; set; }
	public Player Owner { get; set; }
	public bool Taunt { get; set; }
	public bool HasSummoningSickness { get; internal set; }
	public bool HasAttackedThisTurn { get; internal set; }
	public IEnumerable<(TriggeredEffect, StatModifier)> ModifierTriggeredEffects
	{
		get
		{
			// Also return a triggered effect for each temporary modifier
			foreach (var mod in _modifiers.Where(m => m.Duration == EffectDuration.UntilEndOfTurn))
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
	IAttackBehavior IGameEntity.AttackBehavior
	{
		get
		{
			return _attackBehavior;
		}
	}
	private readonly List<StatModifier> _modifiers = new();

	public bool IsAlive{ get; set; }
	public bool IsFrozen { get; internal set; }
	public bool MissedAttackFromFrozen { get; internal set; }
	public bool IsStealth { get; internal set; }
	public bool HasCharge { get; internal set; }
	public bool HasDivineShield { get; internal set; }
	public bool HasPoisonous { get; internal set; }

	public Minion(MinionCard card, Player owner)
	{
		this.card = card;
		Owner = owner;

		Name = card.Name;
		Attack = card.Attack;
		Health = card.Health;
		Tribes = new List<MinionTribe>() { card.MinionTribe };

		_attackBehavior = new MinionAttackBehavior();
		HasAttackedThisTurn = false;
		HasSummoningSickness = true;
		IsStealth = card.IsStealth;
		HasCharge = card.HasCharge;
		HasDivineShield = card.HasDivineShield;
		HasPoisonous = card.HasPoisonous;

		IsAlive = true;
		TriggeredEffects = card.TriggeredEffects.Select(effect =>
		{
			var instance = effect.CloneFor(this);
			return instance;
		});
		
		
	}

	public bool CanAttack()
	{
		return HasCharge || (!HasSummoningSickness && !HasAttackedThisTurn);
	}

	internal Minion Clone()
	{
		return new Minion(this.card, Owner)
		{
			Id = this.Id,
			TemplateName = this.TemplateName,
			Attack = this.Attack,
			Health = this.Health,
			Owner = this.Owner,
			Taunt = this.Taunt,
			HasSummoningSickness = this.HasSummoningSickness,
			HasAttackedThisTurn = this.HasAttackedThisTurn
		};
	}
	internal void AddModifier(StatModifier modifier)
	{
		_modifiers.Add(modifier);
		RecalculateStats();
	}

	internal void RemoveModifier(StatModifier modifier)
	{
		_modifiers.Remove(modifier);
		RecalculateStats();
	}

	private void RecalculateStats()
	{
		Attack = card.Attack;
		Health = card.Health;

		foreach (var mod in _modifiers)
		{
			Attack += mod.AttackChange;
			Health += mod.HealthChange;
		}

		Attack = Math.Max(0, Attack);
		Health = Math.Max(0, Health);
	}

	internal void RemoveExpiredModifiers()
	{
		_modifiers.RemoveAll(m => m.Duration == EffectDuration.UntilEndOfTurn);
		RecalculateStats();
	}

	internal bool HasModifier(StatModifier modifier)
	{
		return _modifiers.Contains(modifier);
	}
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MinionTribe
{
	None,
	All,
	Murloc
}