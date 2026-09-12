namespace CardBattleEngine.Test;

[TestClass]
public class NetworkBattlecryRoundTripTest
{
    [TestMethod]
    public void ExportedBattlecry_DealsOneDamageAfterServerReconstruction()
    {
        var source = new MinionCard("BattlecryMinion", 1, 1, 1);
        source.MinionTriggeredEffects.Add(new TriggeredEffect
        {
            EffectTrigger = EffectTrigger.Battlecry,
            EffectTiming = EffectTiming.Post,
            GameActions = [new DamageAction { Damage = (Value)1 }]
        });
        var json = CardDatabase.ToDefinitionJson(CardDatabase.ToMinionCardDefinition(source, "battlecry-test"));
        var definition = (MinionCardDefinition)CardDatabase.LoadCardFromJson(json)!;
        Assert.AreEqual(1, definition.MinionTriggeredEffects.Count);
        var state = GameFactory.CreateTestGame();
        var player = state.CurrentPlayer;
        var opponent = state.OpponentOf(player);
        var database = new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data"));
        var card = database.BuildMinionCard(definition, player);
        Assert.AreEqual(1, card.MinionTriggeredEffects.Count);
        Assert.AreNotSame(definition.MinionTriggeredEffects[0], card.MinionTriggeredEffects[0]);
        player.Mana = 1;
        player.Hand.Add(card);
        int initialHealth = opponent.Health;
        new GameEngine().Resolve(state, new ActionContext
        {
            SourcePlayer = player,
            SourceCard = card,
            Targets = [opponent]
        }, new PlayCardAction { Card = card });
        Assert.AreEqual(initialHealth - 1, opponent.Health);
        Assert.AreEqual(1, player.Board.Count);
    }
}
