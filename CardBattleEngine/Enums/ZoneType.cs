using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum ZoneType { Deck, Hand, Board, Graveyard }