namespace CardBattleEngine;

public class WeaponCard : Card
{
	public int Durability;

	public WeaponCard(string name, int cost, int attack, int durabilty)
	{
		ManaCost = cost;
		Name = name;
		Attack = attack;
		Durability = durabilty;

		OriginalManaCost = cost;
		OriginalAttack = attack;
		OriginalDurability = durabilty;
	}
	public override CardType Type => CardType.Weapon;

	public override int Attack { get; set; }
	public override int Health { get =>  Durability; set { Durability = value; } }
	public override int MaxHealth { get => Durability; set { Durability = value; } }
	public override bool IsAlive { get; set; }
	public override IAttackBehavior AttackBehavior => throw new NotImplementedException();

	private int OriginalManaCost;
	private int OriginalAttack;
	private int OriginalDurability;

	public override Card Clone()
	{
		return new WeaponCard(Name, ManaCost, Attack, Durability)
		{
			Id = Id,
			Owner = Owner,
			SpriteID = SpriteID,
			Description = Description,
			CastRestriction = CastRestriction,
			ValidTargetSelector = ValidTargetSelector,
			TriggeredEffects = TriggeredEffects.ToList(),
			VariableSet = new VariableSet(VariableSet),
		};
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext)
	{
		actionContext.SourceCard = this;
		actionContext.Target = actionContext.SourcePlayer;
		return new List<(IGameAction, ActionContext)>()
		{
			(new AcquireWeaponAction()
			{
				Weapon = CreateWeapon()
			}, actionContext)
		};
	}

	public Weapon CreateWeapon()
	{
		return new Weapon(Name, Attack, Durability)
		{
			Name = Name,
			TriggeredEffects = TriggeredEffects,
			OriginalCard = this
		};
	}

	internal override void RecalculateStats()
	{
		var attack = OriginalAttack;
		var maxHealth = OriginalDurability;
		var manaCost = OriginalManaCost;

		// Apply modifiers
		foreach (var mod in _modifiers.Concat(_auraModifiers))
		{
			mod.ApplyValue(ref attack, mod.AttackChange);
			mod.ApplyValue(ref maxHealth, mod.HealthChange);
			mod.ApplyValue(ref manaCost, mod.CostChange);
		}

		Attack = Math.Max(0, attack);
		MaxHealth = Math.Max(0, maxHealth);
		Health = MaxHealth;
		ManaCost = Math.Max(0, manaCost);
	}
}