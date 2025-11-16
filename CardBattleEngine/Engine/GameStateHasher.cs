using System.Text;

namespace CardBattleEngine;

public static class GameStateHasher
{
	public static string Hash(GameState state)
	{
		var sb = new StringBuilder();

		AppendPlayer(sb, state.Players[0]);
		sb.Append('|');
		AppendPlayer(sb, state.Players[1]);
		sb.Append('|');
		sb.Append(state.CurrentPlayer == state.Players[0] ? "P1" : "P2");
		sb.Append('|');
		sb.Append(state.turn);

		// Compute non-cryptographic FNV-1a hash
		uint hash = FNV1a32(sb.ToString());
		return hash.ToString("X8"); // 8 hex chars
	}

	private static uint FNV1a32(string input)
	{
		const uint fnvOffset = 2166136261;
		const uint fnvPrime = 16777619;

		uint hash = fnvOffset;

		foreach (char c in input)
		{
			hash ^= (byte)c;      // mix lower byte
			hash *= fnvPrime;

			hash ^= (byte)(c >> 8); // mix upper byte (handles Unicode)
			hash *= fnvPrime;
		}

		return hash;
	}

	private static void AppendPlayer(StringBuilder sb, Player p)
	{
		sb.Append($"HP{p.Health}M{p.Mana}/{p.MaxMana}H{p.Hand.Count}");

		// Hand summary by card cost
		var handSummary = string.Join(',', p.Hand.Select(c => c.ManaCost));
		sb.Append($"[{handSummary}]");

		// Board summary by attack/health
		var boardSummary = string.Join(',', p.Board.Select(m => $"{m.Attack}/{m.Health}"));
		sb.Append($"B[{boardSummary}]");
	}
}
