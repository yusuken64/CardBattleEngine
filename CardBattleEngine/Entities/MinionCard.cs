namespace CardBattleEngine;

public class MinionCard : Card
{
	public int Attack { get; private set; }
	public int Health { get; private set; }

	public MinionCard(string name, int cost, int attack, int health)
	{
		Name = name;
		ManaCost = cost;
		Type = CardType.Minion;
		Attack = attack;
		Health = health;
	}

	internal override IEnumerable<IGameAction> GetPlayEffects(GameState state, Player currentPlayer, Player opponent)
	{
		// Return all effects whose trigger is OnPlay/Battlecry (or whatever you consider)
		foreach (var effect in TriggeredEffect)
		{
			if (effect.EffectTrigger == EffectTrigger.Battlecry)
			{
				foreach (var subAction in effect.GameActions)
					yield return subAction;
			}
		}

		// Summon self is always first
		yield return new SummonMinionAction(this, currentPlayer);
	}

	public override Card Clone()
	{
		return new MinionCard(Name, ManaCost, Attack, Health)
		{
			Owner = Owner
		};
	}
}