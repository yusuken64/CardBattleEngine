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
		return
		[
			new SummonMinionAction(this, currentPlayer)
		];
	}
}