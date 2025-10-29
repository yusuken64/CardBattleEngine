namespace CardBattleEngine;

public class MinionCard : Card
{
	public override CardType Type => CardType.Minion;
	public int Attack { get; private set; }
	public int Health { get; private set; }
	public MinionTribe MinionTribe { get; set; }
	public bool IsStealth { get; set; }
	public bool HasCharge { get; set; }
	public bool HasDivineShield { get; set; }
	public bool HasPoisonous { get; set; }

	public MinionCard(string name, int cost, int attack, int health)
	{
		Name = name;
		ManaCost = cost;
		Attack = attack;
		Health = health;
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext context)
	{
		foreach (var effect in TriggeredEffects)
		{
			if (effect.EffectTrigger != EffectTrigger.OnPlay &&
				effect.EffectTrigger != EffectTrigger.Battlecry)
				continue;

			IGameEntity? target = null;
			// If this play action provided a selector, ask it for a target
			if (context.AffectedEntitySelector != null)
			{
				IEnumerable<IGameEntity> targets = context.AffectedEntitySelector.Select(state, context);
				target = targets.ToList().FirstOrDefault(); //TODO fix
			}

			var effectContext = new ActionContext
			{
				SourceCard = this,
				SourcePlayer = context.SourcePlayer,
				Target = target
			};

			foreach (var gameAction in effect.GameActions)
				yield return (gameAction, effectContext);
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
			MinionTribe = MinionTribe,
		};
	}
}