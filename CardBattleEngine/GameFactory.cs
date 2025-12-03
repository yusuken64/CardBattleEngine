namespace CardBattleEngine;

public static class GameFactory
{
	public static GameState CreateTestGame()
	{
		var p1 = new Player("Alice") { Mana = 1, MaxMana = 1 };
		var p2 = new Player("Bob") { Mana = 1, MaxMana = 1 };

		// create decks of 10 vanilla minions: 1/1, 2/2, ..., 10/10
		for (int i = 1; i <= 10; i++)
		{
			p1.Deck.Add(new MinionCard($"{i + 1}/{i} Soldier2", i, i + 1, i)
			{
				Owner = p1
			});
			p1.Deck.Add(new MinionCard($"{i + 1}/{i} Soldier1", i, i + 1, i)
			{
				Owner = p1
			});
			p1.Deck.Add(new MinionCard($"{i + 1}/{i} Soldier", i, i + 1, i)
			{
				Owner = p1
			});
			p2.Deck.Add(new MinionCard($"{i + 1}/{i} Soldier", i, i + 1, i)
			{
				Owner = p2
			});
			p1.Deck.Add(new MinionCard($"{i}/{i + 1} Soldier", i, i, i + 1)
			{
				Owner = p1
			});
			p2.Deck.Add(new MinionCard($"{i}/{i + 1} Soldier", i, i, i + 1)
			{
				Owner = p2
			});
			p1.Deck.Add(new MinionCard($"{i - 1}/{i} Soldier", i - 1, i, i)
			{
				Owner = p1
			});
			p2.Deck.Add(new MinionCard($"{i - 1}/{i} Soldier", i - 1, i, i)
			{
				Owner = p2
			});
		}

		var state = new GameState(p1, p2, new XorShiftRNG(1), p1.Deck);
		return state;
	}
}