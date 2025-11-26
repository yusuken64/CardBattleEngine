using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum TeamRelationship
{
	Friendly,
	Enemy,
	Any,
}