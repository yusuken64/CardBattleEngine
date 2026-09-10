namespace CardBattleEngine;

public class RelicCard : Card
{
	public override CardType Type => CardType.Relic;
	public override int Health { get; set; }
	public override int MaxHealth { get; set; }
	public override int Attack { get; set; } = 0;
	public override bool IsAlive { get; set; } = true;
	public override IAttackBehavior AttackBehavior => InertAttackBehavior.Instance;
	public int? Charges { get; set; }
	public RelicAbility Ability { get; set; }
	public List<TriggeredEffect> RelicTriggeredEffects { get; set; } = new();

	private int OriginalManaCost;
	private int OriginalHealth;

	public RelicCard(string name, int cost, int health)
	{
		Name = name;
		ManaCost = cost;
		Health = health;
		MaxHealth = health;

		OriginalManaCost = cost;
		OriginalHealth = health;
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext context)
	{
		SummonRelicAction summonRelicAction = new()
		{
			Card = (RelicCard)this.Clone()
		};

		if (summonRelicAction.IsValid(state, context, out _))
		{
			var relic = new Relic(summonRelicAction.Card, context.SourcePlayer);
			context.SummonedRelic = relic;
		}

		yield return (summonRelicAction, context);

		foreach (var effect in RelicTriggeredEffects)
		{
			if (effect.EffectTrigger != EffectTrigger.Battlecry)
			{
				continue;
			}

			IEnumerable<IGameEntity> targets;
			if (effect.AffectedEntitySelector != null)
			{
				targets = effect.AffectedEntitySelector.Select(state, context);
			}
			else
			{
				targets = context.Targets is { Count: > 0 } ? context.Targets : [null];
			}

			foreach (var target in targets)
			{
				var effectContext = new ActionContext
				{
					SourceCard = null,
					Source = context.SummonedRelic,
					SourcePlayer = context.SourcePlayer,
					Targets = [target],
				};

				foreach (var gameAction in effect.GameActions)
					yield return (gameAction, effectContext);
			}
		}
	}

	public override Card Clone()
	{
		return new RelicCard(Name, ManaCost, Health)
		{
			Id = Id,
			Owner = Owner,
			Charges = Charges,
			Ability = Ability,
			SpriteID = SpriteID,
			Description = Description,
			CastRestriction = CastRestriction,
			ValidTargetSelector = ValidTargetSelector,
			TriggeredEffects = TriggeredEffects.ToList(),
			RelicTriggeredEffects = RelicTriggeredEffects.ToList(),
			VariableSet = new VariableSet(VariableSet),
			NumericId = NumericId,
		};
	}

	internal override void RecalculateStats()
	{
		var maxHealth = OriginalHealth;
		var manaCost = OriginalManaCost;

		foreach (var mod in _modifiers.Concat(_auraModifiers))
		{
			mod.ApplyValue(ref maxHealth, mod.HealthChange);
			mod.ApplyValue(ref manaCost, mod.CostChange);
		}

		MaxHealth = Math.Max(0, maxHealth);
		Health = MaxHealth;
		ManaCost = Math.Max(0, manaCost);
	}
}
