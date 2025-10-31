using System.Text.Json;

namespace CardBattleEngine;

public static class ParamHelper
{
	public static T GetValue<T>(object value)
	{
		if (value is JsonElement elem)
		{
			if (typeof(T) == typeof(string))
				return (T)(object)elem.GetString();
			if (typeof(T) == typeof(int))
				return (T)(object)elem.GetInt32();
			if (typeof(T) == typeof(bool))
				return (T)(object)elem.GetBoolean();
			if (typeof(T).IsEnum)
				return (T)Enum.Parse(typeof(T), elem.GetString(), ignoreCase: true);
		}

		return (T)Convert.ChangeType(value, typeof(T));
	}
}