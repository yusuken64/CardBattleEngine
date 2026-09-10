namespace CardBattleEngine;

public class Relic : BoardPermanent
{
	public RelicCard OriginalCard { get; private set; }
	public int? Charges { get; set; }
	public RelicAbility Ability { get; set; }

	public override IAttackBehavior AttackBehavior => InertAttackBehavior.Instance;

	public Relic(RelicCard card, Player owner)
	{
		OriginalCard = card;
		Owner = owner;

		Name = card.Name;
		Attack = 0;
		MaxHealth = card.Health;
		Health = card.Health;
		Charges = card.Charges;
		Ability = card.Ability;

		IsAlive = true;
		TriggeredEffects = card.RelicTriggeredEffects.Select(effect =>
		{
			var instance = effect.Clone();
			return instance;
		}).ToList();

		VariableSet = new VariableSet(card.VariableSet);
	}

	public override bool CanAttack()
	{
		return false;
	}

	internal override BoardPermanent Clone()
	{
		var clone = new Relic(this.OriginalCard, Owner)
		{
			Id = this.Id,
			MaxHealth = this.MaxHealth,
			Health = this.Health,
			Owner = this.Owner,
			Charges = this.Charges,
			Ability = this.Ability,
			TriggeredEffects = this.TriggeredEffects.Select(e => e.Clone()).ToList(),
			IsAlive = IsAlive,
		};

		clone._modifiers = _modifiers.Select(x => x.Clone()).ToList();
		clone._auraModifiers = _auraModifiers.Select(x => x.Clone()).ToList();

		return clone;
	}

	public override void RecalculateStats()
	{
		int oldDamageTaken = MaxHealth - Health;
		if (oldDamageTaken < 0)
			oldDamageTaken = 0;

		var maxHealth = OriginalCard.Health;

		foreach (var mod in _modifiers)
		{
			mod.ApplyValue(ref maxHealth, mod.HealthChange);
		}
		foreach (var mod in _auraModifiers)
		{
			mod.ApplyValue(ref maxHealth, mod.HealthChange);
		}

		MaxHealth = Math.Max(0, maxHealth);

		int newHealth = MaxHealth - oldDamageTaken;
		Health = Utils.Clamp(newHealth, 0, MaxHealth);
	}
}
