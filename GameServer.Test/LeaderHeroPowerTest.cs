using CardBattleEngine;
using CardBattleEngine.View;
using GameServer.Contracts;
using GameServer.Matches;

namespace GameServer.Test;

[TestClass]
public class LeaderHeroPowerTest
{
    private static DecklistRequest Deck(string name, bool targeted = false)
    {
        var leader = new MinionCard(name, 2, 3, 4);
        leader.ValidTargetSelector = targeted ? new EntityTypeSelector { EntityTypes = EntityType.Player, TeamRelationship = TeamRelationship.Enemy } : null;
        leader.MinionTriggeredEffects.Add(new TriggeredEffect
        {
            EffectTrigger = EffectTrigger.Battlecry,
            AffectedEntitySelector = new ContextSelector { IncludeSummonedMinion = !targeted, IncludeTarget = targeted },
            GameActions = targeted ? [new DamageAction { Damage = (Value)3 }] : [new GainArmorAction { Amount = (Value)4 }],
        });
        return new DecklistRequest
        {
            PlayerName = name,
            Minions = [new CardCount { CardId = "Wisp", Count = 12 }],
            // Identical leader ids are valid: leaders are separate from the shared deck catalog.
            LeaderDefinition = CardDatabase.ToDefinitionJson(CardDatabase.ToMinionCardDefinition(leader, "leader")),
        };
    }
    private static Match Create(bool targeted = false, bool queued = false)
    {
        var registry = new MatchRegistry(new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data")));
        if (queued)
        {
            Assert.IsNull(registry.TryMatchmake("a", Deck("Alice", targeted)));
            return registry.TryMatchmake("b", Deck("Bob", targeted))!;
        }
        var id = registry.CreateMatch("a", Deck("Alice", targeted));
        Assert.IsTrue(registry.TryJoinMatch(id, "b", Deck("Bob", targeted), out var match, out var error), error);
        return match!;
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BothSeatsReceiveTheirOwnLeaderWithoutAddingItToDeck(bool queued)
    {
        var match = Create(queued: queued);
        foreach (var player in match.GameState.Players)
        {
            Assert.AreEqual("Invoke " + player.Name, player.HeroPower.Name);
            Assert.AreEqual(2, player.HeroPower.ManaCost);
            Assert.AreEqual(12, player.Deck.Count);
            Assert.IsFalse(player.Deck.Any(c => c.CardId == "leader"));
            var view = PlayerViewBuilder.Build(match.GameState, player);
            Assert.AreEqual(player.Name, view.Self.HeroPower.LeaderCard.Name);
            Assert.AreEqual("leader", view.Opponent.HeroPower.LeaderCard.CardId);
        }
    }

    [TestMethod]
    public void UntargetedPowerCostsManaPlaysBackOnceAndResetsNextTurn()
    {
        var match = Create(); var state = match.GameState; var player = state.Players[0];
        player.Mana = 5; player.MaxMana = 5;
        var before = state.Clone();
        var frames = new List<PlaybackEventView>();
        match.Engine.ActionPlaybackCallback = (s,c) => frames.Add(PlayerViewBuilder.BuildPlayback(s, player, c.action, c.context, frames.Count + 1));
        var option = state.GetValidActions(player).Single(x => x.Item1 is HeroPowerAction);
        Assert.IsTrue(option.Item2.Targets.Count == 0);
        match.Engine.Resolve(state, option.Item2, option.Item1);
        Assert.AreEqual(3, player.Mana);
        Assert.AreEqual(4, player.Armor);
        Assert.IsTrue(player.HeroPower.UsedThisTurn);
        Assert.IsFalse(before.Players[0].HeroPower.UsedThisTurn);
        Assert.AreNotSame(player.HeroPower, before.Players[0].HeroPower);
        Assert.IsFalse(state.GetValidActions(player).Any(x => x.Item1 is HeroPowerAction));
        Assert.IsTrue(frames.Any(f => f.ActionType == nameof(HeroPowerAction) && f.After.Self.HeroPower.UsedThisTurn));
        match.Engine.Resolve(state, new ActionContext { Source = player, SourcePlayer = player }, new StartTurnAction());
        Assert.IsFalse(player.HeroPower.UsedThisTurn);
        Assert.IsTrue(state.GetValidActions(player).Any(x => x.Item1 is HeroPowerAction));
        player.Mana = 1;
        Assert.IsFalse(state.GetValidActions(player).Any(x => x.Item1 is HeroPowerAction));
    }

    [TestMethod]
    public void TargetedPowerExposesOnlyLegalTargetAndResolvesDamage()
    {
        var match = Create(targeted: true); var state = match.GameState;
        var player = state.Players[0]; var opponent = state.Players[1]; player.Mana = 5;
        var option = state.GetValidActions(player).Single(x => x.Item1 is HeroPowerAction);
        Assert.AreSame(opponent, option.Item2.Targets.Single());
        var invalid = new ActionContext { Source = player, SourcePlayer = player, Targets = [player] };
        Assert.IsFalse(option.Item1.IsValid(state, invalid, out _));
        invalid.Targets.Clear();
        Assert.IsFalse(option.Item1.IsValid(state, invalid, out _));
        match.Engine.Resolve(state, option.Item2, option.Item1);
        Assert.AreEqual(27, opponent.Health);
        Assert.AreEqual(3, player.Mana);
    }

    [TestMethod]
    public void MissingOrNonBattlecryLeaderHasNoPowerAndMalformedLeaderIsRejected()
    {
        var registry = new MatchRegistry(new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data")));
        var deck = Deck("Alice"); deck.LeaderDefinition = null;
        var other = Deck("Bob");
        other.LeaderDefinition = CardDatabase.ToDefinitionJson(CardDatabase.ToMinionCardDefinition(new MinionCard("Plain", 1, 1, 1), "plain"));
        var id = registry.CreateMatch("a", deck);
        Assert.IsTrue(registry.TryJoinMatch(id, "b", other, out var match, out var error), error);
        Assert.IsTrue(match!.GameState.Players.All(p => p.HeroPower == null));
        var bad = Deck("Bad"); bad.LeaderDefinition = "invalid json";
        var badId = registry.CreateMatch("c", bad);
        Assert.IsFalse(registry.TryJoinMatch(badId, "d", deck, out _, out error));
        Assert.IsNotNull(error);
    }
}
