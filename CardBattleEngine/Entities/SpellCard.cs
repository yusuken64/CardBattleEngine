
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
