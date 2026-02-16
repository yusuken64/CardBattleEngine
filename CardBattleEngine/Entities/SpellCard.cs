
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
	public List<SpellCastEffect> SpellCastEffects { get; internal set; } = new();
	public override int Health { get; set; }
	public override int MaxHealth { get; set; }
	public override bool IsAlive { get; set; } = true;
	public override int Attack { get; set; }

	public override IAttackBehavior AttackBehavior => null;
	private int OriginalManaCost;

	public object CustomSFX { get; set; }

	public override Card Clone()
	{
		var spellCard = new SpellCard(Name, ManaCost)
		{
			Id = Id,
			Owner = Owner,
			SpriteID = SpriteID,
			Description = Description,
			CastRestriction = CastRestriction,
			ValidTargetSelector = ValidTargetSelector,
			SpellCastEffects = SpellCastEffects.ToList(),
			TriggeredEffects = TriggeredEffects.ToList(),
			VariableSet = new VariableSet(VariableSet),
			NumericId = NumericId,
		};

		return spellCard;
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
		var manaCost = OriginalManaCost;

		// Apply modifiers
		foreach (var mod in _modifiers.Concat(_auraModifiers))
		{
			mod.ApplyValue(ref manaCost, mod.CostChange);
		}

		ManaCost = Math.Max(0, ManaCost);
	}
}
