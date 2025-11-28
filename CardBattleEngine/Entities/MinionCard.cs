namespace CardBattleEngine;

public class MinionCard : Card
{
	public override CardType Type => CardType.Minion;
	public override int Health { get; set; }
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
	public List<TriggeredEffect> MinionTriggeredEffects { get; set; } = new();//triggered effects on summoned minion
	public override IAttackBehavior AttackBehavior => null;


	public MinionCard(string name, int cost, int attack, int health)
	{
		Name = name;
		ManaCost = cost;
		Attack = attack;
		Health = health;
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext context)
	{
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
					SourcePlayer = context.SourcePlayer,
					Target = target
				};

				foreach (var gameAction in effect.GameActions)
					yield return (gameAction, effectContext);
			}
		}

		SummonMinionAction summonMinionAction = new()
		{
			Card = this
		};
		yield return (summonMinionAction, context);
	}

	public override Card Clone()
	{
		return new MinionCard(Name, ManaCost, Attack, Health)
		{
			Owner = Owner,
			IsStealth = IsStealth,
			HasCharge = HasCharge,
			HasDivineShield = HasDivineShield,
			HasPoisonous = HasPoisonous,
			HasTaunt = HasTaunt,
			MinionTribes = MinionTribes.ToList(),
		};
	}
}