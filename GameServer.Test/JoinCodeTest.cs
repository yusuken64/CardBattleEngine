using CardBattleEngine;
using GameServer.Contracts;
using GameServer.Matches;

namespace GameServer.Test;

[TestClass]
public class JoinCodeTest
{
    private static MatchRegistry Registry() => new(new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data")));
    private static DecklistRequest Deck() => new() { PlayerName = "Player", Minions = new() { new() { CardId = "Wisp", Count = 12 } } };

    [TestMethod]
    public void HostedCodesAreReadableAndUnique()
    {
        var registry = Registry();
        var codes = new HashSet<string>();
        Parallel.For(0, 1000, i =>
        {
            var id = registry.CreateMatch($"host-{i}", Deck());
            var code = id.Value;
            StringAssert.Matches(code, new System.Text.RegularExpressions.Regex("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{2}$"));
            lock (codes) Assert.IsTrue(codes.Add(code));
        });
    }

    [TestMethod]
    public void CodeIgnoresCaseSpacesAndHyphenAndIsConsumedOnJoin()
    {
        var registry = Registry();
        var id = registry.CreateMatch("host", Deck());
        var code = id.Value;
        Assert.IsTrue(registry.TryJoinMatch(new MatchId(" " + code.ToLowerInvariant().Replace("-", " ") + " "), "joiner", Deck(), out var match, out var error), error);
        Assert.AreEqual(id, match!.Id);
        Assert.IsFalse(registry.TryJoinMatch(new MatchId(code), "third", Deck(), out _, out _));
    }

    [TestMethod]
    public void StoppingHostExpiresCode()
    {
        var registry = Registry();
        var id = registry.CreateMatch("host", Deck());
        var code = id.Value;
        registry.RemoveConnection("host");
        Assert.IsFalse(registry.TryJoinMatch(new MatchId(code), "joiner", Deck(), out _, out _));
        Assert.IsFalse(registry.TryJoinMatch(id, "joiner", Deck(), out _, out _));
    }

    [TestMethod]
    public void MatchIdIsTheCodeAndStaysReservedUntilBothPlayersLeave()
    {
        var registry = Registry();
        var id = registry.CreateMatch("host", Deck());
        var code = id.Value;
        Assert.IsTrue(registry.TryJoinMatch(id, "joiner", Deck(), out _, out var error), error);
        Assert.IsFalse(registry.TryJoinMatch(new MatchId(code), "third", Deck(), out _, out _));
        Assert.IsTrue(registry.TryGet(new MatchId(code.ToLowerInvariant()), out _));
        registry.RemoveConnection("host");
        Assert.IsTrue(registry.TryGet(id, out _));
        registry.RemoveConnection("joiner");
        Assert.IsFalse(registry.TryGet(id, out _));
    }

    [TestMethod]
    public void QuickMatchUsesSameReadableIdAndDoesNotPairAPlayerWithItself()
    {
        var registry = Registry();
        Assert.IsNull(registry.TryMatchmake("a", Deck()));
        Assert.IsNull(registry.TryMatchmake("a", Deck()));
        var match = registry.TryMatchmake("b", Deck());
        Assert.IsNotNull(match);
        StringAssert.Matches(match.Id.Value, new System.Text.RegularExpressions.Regex("^[A-HJ-NP-Z2-9]{4}-[A-HJ-NP-Z2-9]{2}$"));
        Assert.IsTrue(registry.TryGet(new MatchId(match.Id.Value), out _));
    }

    [TestMethod]
    public void InvalidAndSelfJoinDoNotConsumeHostedMatch()
    {
        var registry = Registry();
        var id = registry.CreateMatch("host", Deck());
        var code = id.Value;
        Assert.IsFalse(registry.TryJoinMatch(new MatchId(null!), "joiner", Deck(), out _, out _));
        Assert.IsFalse(registry.TryJoinMatch(new MatchId("bad"), "joiner", Deck(), out _, out _));
        Assert.IsFalse(registry.TryJoinMatch(new MatchId(code), "host", Deck(), out _, out _));
        Assert.IsTrue(registry.TryJoinMatch(new MatchId(code), "joiner", Deck(), out _, out var error), error);
    }
}
