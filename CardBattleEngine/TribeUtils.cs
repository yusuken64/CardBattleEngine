namespace CardBattleEngine;

// Tribes are free-text strings (data-driven, no fixed enum) so custom cards can name any tribe they
// want. Matching normalizes case/whitespace but never fuzzy-matches - a misspelled tribe is simply a
// different tribe. "all" is a reserved wildcard: any entity tagged with it matches every tribe query.
public static class TribeUtils
{
	public const string All = "all";

	public static string Normalize(string tribe) => tribe?.Trim().ToLowerInvariant() ?? string.Empty;

	public static bool Matches(IEnumerable<string> tribes, string query)
	{
		if (tribes == null) return false;

		var normalizedQuery = Normalize(query);
		foreach (var tribe in tribes)
		{
			var normalized = Normalize(tribe);
			if (normalized == All || normalized == normalizedQuery)
				return true;
		}

		return false;
	}
}
