namespace CardBattleEngine;

// Builds a real (non-test) GameState from actual decklists and a caller-supplied RNG seed - unlike
// GameFactory.CreateTestGame(), which hardcodes both the deck contents and the RNG seed (1) for
// deterministic unit tests, this is what a game server uses to start an actual match. CardDatabase
// has no unified "any card type" lookup (GetMinionCard/GetSpellCard are separate, each throwing
// KeyNotFoundException on a miss), so minion/spell ids are taken as separate lists rather than
// adding a new lookup method to CardDatabase.
public static class MatchFactory
{
	public static GameState CreateMatch(
		CardDatabase cardDb,
		Player player1,
		IEnumerable<(string CardId, int Count)> minionDeck1,
		IEnumerable<(string CardId, int Count)> spellDeck1,
		Player player2,
		IEnumerable<(string CardId, int Count)> minionDeck2,
		IEnumerable<(string CardId, int Count)> spellDeck2,
		ulong rngSeed)
	{
		AddToDeck(cardDb, player1, minionDeck1, spellDeck1);
		AddToDeck(cardDb, player2, minionDeck2, spellDeck2);

		var cardPool = player1.Deck.Concat(player2.Deck);

		return new GameState(player1, player2, new XorShiftRNG(rngSeed), cardPool);
	}

	private static void AddToDeck(
		CardDatabase cardDb,
		Player player,
		IEnumerable<(string CardId, int Count)> minionDeck,
		IEnumerable<(string CardId, int Count)> spellDeck)
	{
		foreach (var (cardId, count) in minionDeck)
		{
			for (int i = 0; i < count; i++)
			{
				player.Deck.Add(cardDb.GetMinionCard(cardId, player));
			}
		}

		foreach (var (cardId, count) in spellDeck)
		{
			for (int i = 0; i < count; i++)
			{
				player.Deck.Add(cardDb.GetSpellCard(cardId, player));
			}
		}
	}
}
