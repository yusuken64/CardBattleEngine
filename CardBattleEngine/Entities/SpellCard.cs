
namespace CardBattleEngine;

public class SpellCard : Card
{
	public SpellCard(string name, int cost)
	{
		Name = name;
		ManaCost = cost;
		OriginalManaCost = cost;
	}

	public override CardType Type => CardType.Spell;
	public TargetingType TargetingType { get; set; }
	public List<SpellCastEffect> SpellCastEffects { get; } = new();
	public override int Health { get; set; }
	public override int MaxHealth { get; set; }
	public override bool IsAlive { get; set; } = true;
	public override int Attack { get; set; }

	public override IAttackBehavior AttackBehavior => null;
	private int OriginalManaCost;

	public object CustomSFX { get; set; }

	public override Card Clone()
	{
		return new SpellCard(Name, ManaCost)
		{
			Id = Id,
		};
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext)
	{
		actionContext.SourceCard = this;
		return new List<(IGameAction, ActionContext)>()
		{
			(new CastSpellAction()
			{
				CustomSFX = CustomSFX
			}, actionContext)
		};
	}

	internal override void RecalculateStats()
	{
		ManaCost = OriginalManaCost;

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

		ManaCost = Math.Max(0, ManaCost);
	}
}
