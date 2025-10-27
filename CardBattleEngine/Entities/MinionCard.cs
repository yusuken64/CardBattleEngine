using System.Security.Principal;

namespace CardBattleEngine;

public class MinionCard : Card
{
	public int Attack { get; private set; }
	public int Health { get; private set; }
	public bool IsStealth { get; set; }

	public MinionCard(string name, int cost, int attack, int health)
	{
		Name = name;
		ManaCost = cost;
		Type = CardType.Minion;
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
			if (context.TargetSelector != null)
			{
				target = context.TargetSelector(state, context.SourcePlayer, effect.TargetType);
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
			IsStealth = IsStealth
		};
	}
}