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
		};
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext)
	{
		actionContext.SourceCard = this;
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
		return new Weapon()
		{
			Name = Name,
			Attack = Attack,
			Durability = Durability,
			TriggeredEffects = TriggeredEffects
		};
	}

	internal override void RecalculateStats()
	{
		Attack = OriginalAttack;
		MaxHealth = OriginalDurability;
		ManaCost = OriginalManaCost;

		// Apply modifiers
		foreach (var mod in _modifiers)
		{
			Attack += mod.AttackChange;
			MaxHealth += mod.HealthChange;
			ManaCost += mod.CostChange;
		}

		foreach (var mod in _auraModifiers)
		{
			Attack += mod.AttackChange;
			MaxHealth += mod.HealthChange;
			ManaCost += mod.CostChange;
		}

		Attack = Math.Max(0, Attack);
		MaxHealth = Math.Max(0, MaxHealth);
		Health = MaxHealth;
		ManaCost = Math.Max(0, ManaCost);
	}
}