using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum CardType { Minion, Spell, Weapon, Hero }