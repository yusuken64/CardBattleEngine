namespace CardBattleEngine;

public class WeaponCard : Card
{
	public int Attack;
	public int Durability;

	public WeaponCard(string name, int cost, int attack, int durabilty)
	{
		ManaCost = cost;
		Name = name;
		Attack = attack;
		Durability = durabilty;
	}

	public override CardType Type => CardType.Weapon;
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