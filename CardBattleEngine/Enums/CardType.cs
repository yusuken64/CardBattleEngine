using System.Text.Json.Serialization;

namespace CardBattleEngine;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CardType { Minion, Spell, Weapon, Hero }