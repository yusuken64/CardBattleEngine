namespace CardBattleEngine;

public class MinionCard : Card
{
	public override CardType Type => CardType.Minion;
	public override int Health { get; set; }

	private int OriginalManaCost;
	private int OriginalAttack;
	private int OriginalHealth;

	public override int MaxHealth { get; set; }
	public override int Attack { get; set; }
	public override bool IsAlive { get; set; } = true;
	public List<MinionTribe> MinionTribes { get; set; } = new();
	public bool IsStealth { get; set; }
	public bool HasCharge { get; set; }
	public bool HasDivineShield { get; set; }
	public bool HasPoisonous { get; set; }
	public bool HasTaunt { get; set; }
	public bool HasRush { get; set; }
	public bool HasWindfury { get; set; }
	public bool HasLifeSteal { get; set; }
	public bool HasReborn{ get; set; }
	public bool CannotAttack { get; set; }
	public List<TriggeredEffect> MinionTriggeredEffects { get; set; } = new();//triggered effects on summoned minion
	public override IAttackBehavior AttackBehavior => null;


	public MinionCard(string name, int cost, int attack, int health)
	{
		Name = name;
		ManaCost = cost;
		Attack = attack;
		Health = health;
		MaxHealth = health;

		OriginalManaCost = cost;
		OriginalAttack = attack;
		OriginalHealth = health;
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext context)
	{
		SummonMinionAction summonMinionAction = new()
		{
			Card = this
		};

		if (summonMinionAction.IsValid(state, context, out _))
		{
			var minion = new Minion(summonMinionAction.Card, context.SourcePlayer);
			context.SummonedMinion = minion;
		}

		yield return (summonMinionAction, context);

		int originalIndex = context.PlayIndex;
		foreach (var effect in MinionTriggeredEffects)
		{
			if (effect.EffectTrigger != EffectTrigger.OnPlay &&
				effect.EffectTrigger != EffectTrigger.Battlecry)
				continue;

			IEnumerable<IGameEntity> targets;
			// If this play action provided a selector, ask it for a target
			if (effect.AffectedEntitySelector != null)
			{
				targets = effect.AffectedEntitySelector.Select(state, context);
			}
			else
			{
				targets = [context.Target];
			}

			foreach (var target in targets)
			{
				var effectContext = new ActionContext
				{
					SourceCard = this,
					Source = context.SummonedMinion,
					SourcePlayer = context.SourcePlayer,
					Target = target,
					PlayIndex = originalIndex
				};

				foreach (var gameAction in effect.GameActions)
					yield return (gameAction, effectContext);
			}
		}
	}

	public override Card Clone()
	{
		return new MinionCard(Name, ManaCost, Attack, Health)
		{
			Id = Id,
			Owner = Owner,
			IsStealth = IsStealth,
			HasCharge = HasCharge,
			HasDivineShield = HasDivineShield,
			HasPoisonous = HasPoisonous,
			HasTaunt = HasTaunt,
			MinionTribes = MinionTribes.ToList(),
			MaxHealth = Health,
		};
	}

	internal override void RecalculateStats()
	{
		Attack = OriginalAttack;
		MaxHealth = OriginalHealth;
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