
namespace CardBattleEngine;

public class SpellCard : Card
{
	public SpellCard(string name, int cost)
	{
		Name = name;
		ManaCost = cost;
	}

	public override CardType Type => CardType.Spell;
	public TargetingType TargetingType { get; set; }
	public List<SpellCastEffect> SpellCastEffects { get; } = new();
	public override int Health { get; set; }
	public override int MaxHealth { get; set; }
	public override bool IsAlive { get; set; }
	public override int Attack { get; set; }

	public override IAttackBehavior AttackBehavior => null;


	public override Card Clone()
	{
		throw new NotImplementedException();
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext)
	{
		actionContext.SourceCard = this;
		return new List<(IGameAction, ActionContext)>()
		{
			(new CastSpellAction(), actionContext)
		};
	}
}
