using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum TargetGroup
{
	Minions,
	Hero,
	All
}