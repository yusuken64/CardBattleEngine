namespace CardBattleEngine;

public static class Utils
{
	public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
	{
		if (value.CompareTo(min) < 0) return min;
		if (value.CompareTo(max) > 0) return max;
		return value;
	}

	public static TEnum GetEnum<TEnum>(
		Dictionary<string, object> dict,
		string key,
		TEnum defaultValue = default) where TEnum : struct
	{
		if (!dict.TryGetValue(key, out var raw) || raw == null)
			return defaultValue;

		// Unity-safe enum parsing
		if (Enum.TryParse<TEnum>(raw.ToString(), out var result))
			return (TEnum)result;

		return defaultValue;
	}
}