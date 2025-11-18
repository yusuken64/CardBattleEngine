using CardBattleEngine;
using Newtonsoft.Json.Linq;

public static class JsonParamHelper
{
	public static Dictionary<string, object> Normalize(Dictionary<string, object> raw)
	{
		var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		foreach (var kv in raw)
		{
			if (kv.Value == null)
			{
				result[kv.Key] = null;
				continue;
			}

			switch (kv.Value)
			{
				case JValue jv:
					result[kv.Key] = jv.Value;
					break;

				case JObject jo:
					result[kv.Key] = jo; // keep JObject if needed
					break;

				case JArray ja:
					result[kv.Key] = ja.ToObject<List<object>>();
					break;

				default:
					result[kv.Key] = kv.Value;
					break;
			}
		}

		return result;
	}

	public static T GetValue<T>(Dictionary<string, object> dict, string key, T defaultValue = default) where T : struct
	{
		if (!dict.TryGetValue(key, out var raw) || raw == null)
			return defaultValue;

		// Direct cast works for simple cases
		if (raw is T v)
			return v;

		// Handle enums
		if (typeof(T).IsEnum)
		{
			string str = raw.ToString();
			if (Enum.TryParse(str, out T parsed))
				return (T)parsed;

			return defaultValue;
		}

		// JObject -> T
		if (raw is JObject jo)
		{
			try
			{
				return jo.ToObject<T>();
			}
			catch
			{
				return defaultValue;
			}
		}

		// JValue -> unwrap then convert
		if (raw is JValue jv)
			return ConvertWrappedValue<T>(jv.Value, defaultValue);

		// Good for int, long, float, double, bool, etc.
		if (raw is IConvertible)
			return ConvertWrappedValue<T>(raw, defaultValue);

		// Last resort: parse from string
		return ConvertWrappedValue<T>(raw.ToString(), defaultValue);
	}

	private static T ConvertWrappedValue<T>(object raw, T defaultValue)
	{
		try
		{
			// Special case: bool
			if (typeof(T) == typeof(bool))
			{
				if (bool.TryParse(raw.ToString(), out var b))
					return (T)(object)b;
			}

			// Special case: int
			if (typeof(T) == typeof(int))
			{
				if (int.TryParse(raw.ToString(), out var i))
					return (T)(object)i;
			}

			// Special case: double
			if (typeof(T) == typeof(double))
			{
				if (double.TryParse(raw.ToString(), out var d))
					return (T)(object)d;
			}

			// General-purpose conversion
			return (T)Convert.ChangeType(raw, typeof(T));
		}
		catch
		{
			return defaultValue;
		}
	}

	internal static T GetEnum<T>(Dictionary<string, object> dict, string key, T defaultValue = default) where T : struct
	{
		if (!dict.TryGetValue(key, out var raw) || raw == null)
			return defaultValue;

		var str = raw.ToString();
		if (Enum.TryParse<T>(str, out var parsed))
			return (T)parsed;

		return defaultValue;
	}
}
