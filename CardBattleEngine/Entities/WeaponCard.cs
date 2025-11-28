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
	}

	public override CardType Type => CardType.Weapon;

	public override int Attack { get; set; }
	public override int Health { get =>  Durability; set { Durability = value; } }
	public override int MaxHealth { get => Durability; set { Durability = value; } }
	public override bool IsAlive { get; set; }
	public override IAttackBehavior AttackBehavior => throw new NotImplementedException();


	public override Card Clone()
	{
		throw new NotImplementedException();
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext)
	{
		actionContext.SourceCard = this;
		return new List<(IGameAction, ActionContext)>()
		{
			(new AcquireWeaponAction()
			{
				Weapon = new Weapon()
				{
					Name = Name,
					Attack = Attack,
					Durability = Durability,
					TriggeredEffects = TriggeredEffects
				}
			}, actionContext)
		};
	}
}