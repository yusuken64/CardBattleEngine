using System.Security.Cryptography;
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

		// Compute a short hash (SHA256 → base64 or hex)
		using var sha = SHA256.Create();
		var bytes = Encoding.UTF8.GetBytes(sb.ToString());
		var hashBytes = sha.ComputeHash(bytes);
		return Convert.ToHexString(hashBytes, 0, 8); // short 8-byte hex digest
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