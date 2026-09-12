using CardBattleEngine.View;
using Newtonsoft.Json;

namespace CardBattleEngine.Test;

[TestClass]
public class PlaybackViewBuilderTests
{
    private static GameState State() => new(new Player("Alice"), new Player("Bob"), new XorShiftRNG(1), new List<Card>());

    [TestMethod]
    public void Capture_PreservesSummonDamageAndDeathStates()
    {
        var state = State();
        var player = state.Players[0];
        var engine = new GameEngine();
        var frames = new List<PlaybackEventView>();
        engine.ActionPlaybackCallback = (s, c) => frames.Add(PlayerViewBuilder.BuildPlayback(s, player, c.action, c.context, frames.Count + 1));
        var context = new ActionContext { SourcePlayer = player };
        engine.Resolve(state, context, new SummonMinionAction { Card = new MinionCard("Token", 1, 1, 2) });
        var minion = context.SummonedMinion;
        engine.Resolve(state, new ActionContext { SourcePlayer = player, Source = player, Targets = [minion] }, new DamageAction { Damage = (Value)3 });
        Assert.AreEqual(2, frames[0].SummonedMinion.Minion.Health);
        Assert.AreEqual(1, frames[0].After.Self.Board.Count);
        var damage = frames.Single(f => f.ActionType == nameof(DamageAction));
        Assert.AreEqual(minion.Id, damage.AffectedEntities[0].Target.Id);
        Assert.AreEqual(3, damage.AffectedEntities[0].Amount);
        Assert.AreEqual(1, damage.After.Self.Board.Count, "Keep the damaged entity until its death event.");
        var death = frames.Single(f => f.ActionType == nameof(DeathAction));
        Assert.AreEqual(minion.Id, death.Target.Id);
        Assert.AreEqual(0, death.After.Self.Board.Count);
        Assert.AreEqual(2, frames[0].After.Self.Board[0].Health, "Later mutations must not change earlier frames.");
    }

    [TestMethod]
    public void MultiTargetDamageAndHealing_CopyAllResolvedAmounts()
    {
        var state = State();
        var owner = state.Players[0];
        var first = new Minion(new MinionCard("First", 1, 1, 8), owner);
        var second = new Minion(new MinionCard("Second", 1, 1, 8), owner);
        owner.Board.AddRange([first, second]);
        var engine = new GameEngine();
        var frames = new List<PlaybackEventView>();
        engine.ActionPlaybackCallback = (s,c) => frames.Add(PlayerViewBuilder.BuildPlayback(s, owner, c.action, c.context, frames.Count + 1));
        engine.Resolve(state, new ActionContext { SourcePlayer = owner, Source = owner, Targets = [first, second] }, new DamageAction { Damage = (Value)3 });
        engine.Resolve(state, new ActionContext { SourcePlayer = owner, Source = owner, Targets = [first, second] }, new HealAction { Amount = (Value)2 });
        Assert.AreEqual(2, frames[0].AffectedEntities.Count);
        Assert.IsTrue(frames[0].AffectedEntities.All(x => x.Amount == 3));
        Assert.AreEqual(2, frames[1].AffectedEntities.Count);
        Assert.IsTrue(frames[1].AffectedEntities.All(x => x.Amount == 2));
        Assert.AreEqual(5, frames[0].After.Self.Board[0].Health);
        Assert.AreEqual(7, frames[1].After.Self.Board[0].Health);
    }

    [TestMethod]
    public void SecretIdentityAndEffectId_AreHiddenBeforeSecretBecomesActive()
    {
        var state = State();
        var owner = state.Players[0];
        var spell = new SpellCard("Private secret", 1) { Owner = owner, CardId = "private-card" };
        spell.SpellCastEffects.Add(new SpellCastEffect { GameActions = [new SecretAction { Secret = new Secret() }] });
        var context = new ActionContext { SourcePlayer = owner, Source = spell, SourceCard = spell, Targets = [owner] };
        var action = new PlayCardAction { Card = spell, PresentationEffectId = "private-effect" };
        var frame = PlayerViewBuilder.BuildPlayback(state, state.Players[1], action, context, 1);
        Assert.AreEqual("SecretPlayed", frame.ActionType);
        Assert.IsTrue(frame.HiddenCardPlayed);
        var json = JsonConvert.SerializeObject(frame);
        Assert.IsFalse(json.Contains("Private secret"));
        Assert.IsFalse(json.Contains("private-card"));
        Assert.IsFalse(json.Contains("private-effect"));
        Assert.IsNull(frame.Source);
        Assert.IsNull(frame.After.Opponent.Hand);
        Assert.IsNull(frame.After.Opponent.Secrets);
    }

    [TestMethod]
    public void PrivateCardEffects_DoNotRevealOpponentHand_OrDeck()
    {
        var state = State();
        var owner = state.Players[0];
        var card = new SpellCard("Private draw", 1) { Owner = owner, CardId = "private-draw" };
        owner.Hand.Add(card);
        var context = new ActionContext { SourcePlayer = owner, SourceCard = card, Source = card };
        var action = new GainCardAction { PresentationEffectId = "private-effect" };
        var other = PlayerViewBuilder.BuildPlayback(state, state.Players[1], action, context, 1);
        Assert.IsNull(other.SourceCard);
        Assert.IsNull(other.PresentationEffectId);
        Assert.IsNull(other.After.Opponent.Hand);
        var own = PlayerViewBuilder.BuildPlayback(state, owner, action, context, 1);
        Assert.AreEqual(card.Id, own.SourceCard.Id);
        owner.Hand.Clear(); owner.Deck.Add(card);
        own = PlayerViewBuilder.BuildPlayback(state, owner, action, context, 2);
        Assert.IsNull(own.SourceCard, "Deck identity is not presentation data, even for the owner.");
    }

    [TestMethod]
    public void PlayedSpellFollowup_KeepsCustomEffectAndPublicSource()
    {
        var state = State();
        var owner = state.Players[0];
        var spell = new SpellCard("Public spell", 1) { Owner = owner };
        var frame = PlayerViewBuilder.BuildPlayback(state, state.Players[1],
            new DamageAction { PresentationEffectId = "projectile" },
            new ActionContext { SourcePlayer = owner, SourceCard = spell, Targets = [state.Players[1]] }, 1);
        Assert.AreEqual("projectile", frame.PresentationEffectId);
        Assert.AreEqual(spell.Id, frame.SourceCard.Id);
    }

    [TestMethod]
    public void EffectIds_SurviveDefinitionRoundtripCloneAndGeneratedCast()
    {
        var state = State();
        var owner = state.Players[0];
        owner.Mana = 10;
        var spell = new SpellCard("Effect spell", 1) { Owner = owner, PresentationEffectId = "cast-effect" };
        spell.SpellCastEffects.Add(new SpellCastEffect { GameActions = [new DamageAction { Damage = (Value)1, PresentationEffectId = "damage-effect" }] });
        var json = CardDatabase.ToDefinitionJson(CardDatabase.ToSpellCardDefinition(spell, "test-spell"));
        var definition = (SpellCardDefinition)CardDatabase.LoadCardFromJson(json);
        var restored = new CardDatabase("Data").BuildSpellCard(definition, owner);
        Assert.AreEqual("cast-effect", ((SpellCard)restored.Clone()).PresentationEffectId);
        Assert.AreEqual("damage-effect", restored.SpellCastEffects[0].GameActions[0].Clone().PresentationEffectId);
        owner.Hand.Add(restored);
        var frames = new List<PlaybackEventView>();
        var engine = new GameEngine();
        engine.ActionPlaybackCallback = (s,c) => frames.Add(PlayerViewBuilder.BuildPlayback(s, owner, c.action, c.context, frames.Count + 1));
        engine.Resolve(state, new ActionContext { SourcePlayer = owner, SourceCard = restored, Source = restored, Targets = [state.Players[1]] }, new PlayCardAction { Card = restored });
        Assert.AreEqual("cast-effect", frames.Single(x => x.ActionType == nameof(CastSpellAction)).PresentationEffectId);
        Assert.AreEqual("damage-effect", frames.Single(x => x.ActionType == nameof(DamageAction)).PresentationEffectId);
    }
}
