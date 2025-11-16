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
					result[kv.Key] = ja.ToObject<List<object>>(); // convert arrays to list
					break;

				default:
					result[kv.Key] = kv.Value;
					break;
			}
		}

		return result;
	}

	public static int GetInt(Dictionary<string, object> dict, string key, int defaultValue = 0)
	{
		if (!dict.TryGetValue(key, out var raw) || raw == null)
			return defaultValue;

		return raw switch
		{
			int i => i,
			long l => (int)l,
			double d => (int)d,
			string s when int.TryParse(s, out var parsed) => parsed,
			_ => defaultValue
		};
	}

	public static bool GetBool(Dictionary<string, object> dict, string key, bool defaultValue = false)
	{
		if (!dict.TryGetValue(key, out var raw) || raw == null)
			return defaultValue;

		return raw switch
		{
			bool b => b,
			string s when bool.TryParse(s, out var parsed) => parsed,
			_ => defaultValue
		};
	}

	public static TEnum GetEnum<TEnum>(Dictionary<string, object> dict, string key, TEnum defaultValue = default) where TEnum : struct
	{
		if (!dict.TryGetValue(key, out var raw) || raw == null)
			return defaultValue;

		return Enum.TryParse<TEnum>(raw.ToString(), true, out var value) ? value : defaultValue;
	}
}
