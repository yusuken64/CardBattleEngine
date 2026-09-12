using CardBattleEngine;
using CardBattleEngine.View;
using GameServer.Contracts;
using GameServer.Matches;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GameServer.Test;

[TestClass]
public class SharedDefaultDeckTest
{
    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task TwoGamePlayersWithDefaultDecks_StartMatch(bool quickMatch)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var app = ServerHost.Build(builder);
        await app.StartAsync();
        try
        {
            var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
            await using var a = new HubConnectionBuilder().WithUrl($"{url}/hubs/match").Build();
            await using var b = new HubConnectionBuilder().WithUrl($"{url}/hubs/match").Build();
            var foundA = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var foundB = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var viewA = new TaskCompletionSource<PlayerGameView>(TaskCreationOptions.RunContinuationsAsynchronously);
            var viewB = new TaskCompletionSource<PlayerGameView>(TaskCreationOptions.RunContinuationsAsynchronously);
            var playable = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            a.On<string>("OnMatchFound", id => foundA.TrySetResult(id));
            b.On<string>("OnMatchFound", id => foundB.TrySetResult(id));
            void OnView(TaskCompletionSource<PlayerGameView> completion, PlayerGameView view)
            {
                completion.TrySetResult(view);
                if (view.LegalActions.Count > 0) playable.TrySetResult(true);
            }
            a.On<PlayerGameView>("OnStateUpdated", view => OnView(viewA, view));
            b.On<PlayerGameView>("OnStateUpdated", view => OnView(viewB, view));
            await a.StartAsync();
            await b.StartAsync();
            // Actual GamePlayer decks, including their shared prototype minion/spell IDs.
            var deckA = RemoteGameClient.BuildDefaultDeck("Player");
            var deckB = RemoteGameClient.BuildDefaultDeck("Player");
            if (quickMatch)
            {
                await a.InvokeAsync("JoinQueue", deckA);
                await b.InvokeAsync("JoinQueue", deckB);
                Assert.AreEqual(await foundA.Task.WaitAsync(TimeSpan.FromSeconds(5)),
                    await foundB.Task.WaitAsync(TimeSpan.FromSeconds(5)));
            }
            else
            {
                var id = await a.InvokeAsync<string>("CreateMatch", deckA);
                var result = await b.InvokeAsync<JoinResult>("JoinMatch", id, deckB);
                Assert.IsTrue(result.Success, result.Error);
            }
            await Task.WhenAll(viewA.Task, viewB.Task, playable.Task).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreNotEqual(viewA.Task.Result.ViewerPlayerId, viewB.Task.Result.ViewerPlayerId);
        }
        finally { await app.StopAsync(); }
    }

    [TestMethod]
    public void EquivalentDefinitions_WithDifferentJsonFormattingAndPropertyOrder_CanMatch()
    {
        var registry = new MatchRegistry(new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data")));
        var a = RemoteGameClient.BuildDefaultDeck("Alice");
        var b = RemoteGameClient.BuildDefaultDeck("Bob");
        string Reorder(string json) => new JObject(JObject.Parse(json).Properties().Reverse()
            .Select(p => new JProperty(p.Name, p.Value.DeepClone()))).ToString();
        b.CustomMinions = b.CustomMinions.Select(Reorder).ToList();
        b.CustomSpells = b.CustomSpells.Select(Reorder).ToList();
        Assert.IsNull(registry.TryMatchmake("a", a));
        Assert.IsNotNull(registry.TryMatchmake("b", b));
    }

    [TestMethod]
    public void ConflictingSpellDefinition_IsRejectedWithoutConsumingHostedMatch()
    {
        var registry = new MatchRegistry(new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data")));
        var a = RemoteGameClient.BuildDefaultDeck("Alice");
        var b = RemoteGameClient.BuildDefaultDeck("Bob");
        var spell = (SpellCardDefinition)CardDatabase.LoadCardFromJson(b.CustomSpells[0])!;
        spell.Cost++;
        b.CustomSpells[0] = CardDatabase.ToDefinitionJson(spell);
        var id = registry.CreateMatch("a", a);
        Assert.IsFalse(registry.TryJoinMatch(id, "b", b, out _, out var error));
        StringAssert.Contains(error!, spell.Id);
        Assert.IsTrue(registry.TryJoinMatch(id, "b", RemoteGameClient.BuildDefaultDeck("Bob"), out _, out error), error);
    }
}
