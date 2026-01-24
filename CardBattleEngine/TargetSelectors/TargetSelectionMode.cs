using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum TargetSelectionMode
{
	All,
	FirstN,
	RandomN
}