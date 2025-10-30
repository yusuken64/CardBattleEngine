using System.Text.Json.Serialization;

namespace CardBattleEngine;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetSelectionMode
{
	All,
	FirstN,
	RandomN
}