using System.Text.Json.Serialization;

namespace CardBattleEngine;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZoneType { Deck, Hand, Board, Graveyard }