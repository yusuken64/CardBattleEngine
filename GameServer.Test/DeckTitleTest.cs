using CardBattleEngine;
using CardBattleEngine.View;
using GameServer.Contracts;
using GameServer.Matches;
using System.Text.Json;

namespace GameServer.Test;

[TestClass]
public class DeckTitleTest
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BothSeatsReceiveDeckTitlesWithoutRevealingOpponentHand(bool quickMatch)
    {
        var registry = new MatchRegistry(new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data")));
        var first = new DecklistRequest { PlayerName = "Alice", DeckTitle = "Forest Guard", Minions = new() { new() { CardId = "Wisp", Count = 12 } } };
        var second = new DecklistRequest { PlayerName = "Bob", DeckTitle = "Moonlight", Minions = new() { new() { CardId = "Wisp", Count = 12 } } };
        Match? match;
        if (quickMatch)
        {
            Assert.IsNull(registry.TryMatchmake("host", first));
            match = registry.TryMatchmake("joiner", second);
        }
        else
        {
            var id = registry.CreateMatch("host", first);
            Assert.IsTrue(registry.TryJoinMatch(id, "joiner", second, out match, out var error), error);
        }
        Assert.IsNotNull(match);
        foreach (var player in match.GameState.Players)
        {
            Assert.AreEqual(player.DeckTitle, player.Clone().DeckTitle);
            var view = PlayerViewBuilder.Build(match.GameState, player);
            var wire = JsonSerializer.Deserialize<PlayerGameView>(JsonSerializer.Serialize(view))!;
            Assert.AreEqual(player.Name == "Alice" ? "Forest Guard" : "Moonlight", wire.Self.DeckTitle);
            Assert.AreEqual(player.Name == "Alice" ? "Moonlight" : "Forest Guard", wire.Opponent.DeckTitle);
            Assert.IsNull(wire.Opponent.Hand);
            Assert.AreEqual(12, wire.Opponent.DeckCount);
        }
    }

    [TestMethod]
    public void OlderRequestsAndViewsDefaultToEmptyDeckTitle()
    {
        Assert.AreEqual("", JsonSerializer.Deserialize<DecklistRequest>("{\"PlayerName\":\"Old client\"}")!.DeckTitle);
        Assert.AreEqual("", JsonSerializer.Deserialize<PublicPlayerView>("{\"Name\":\"Old server\"}")!.DeckTitle);
    }
}
