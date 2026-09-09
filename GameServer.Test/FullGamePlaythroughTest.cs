using CardBattleEngine.View;
using GameServer;
using GameServer.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameServer.Test;

// Networked counterpart to CardBattleEngine.Test.FullGamePlaythroughTest: drives two SignalR
// clients against an in-process GameServer through CreateMatch/JoinMatch/SubmitAction, the same
// protocol RemoteGameClient (the console runner's --remote mode) and GameServer.TestClient use.
// Exercises MatchHub, MatchDriver, RemotePlayerAgent and PlayerViewBuilder end-to-end, including
// the mulligan prompt, to catch regressions where a legal action becomes unsubmittable over the wire.
[TestClass]
public class FullGamePlaythroughTest
{
	[TestMethod]
	public async Task FirstOption_CanCompleteFullGame()
	{
		await RunGame(pickFirst: true);
	}

	[TestMethod]
	public async Task RandomOption_CanCompleteFullGame()
	{
		await RunGame(pickFirst: false);
	}

	private async Task RunGame(bool pickFirst)
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls("http://127.0.0.1:0");
		builder.Logging.ClearProviders();
		var app = ServerHost.Build(builder);
		await app.StartAsync();

		try
		{
			var addressesFeature = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
			var baseUrl = addressesFeature!.Addresses.First();
			var hubUrl = $"{baseUrl}/hubs/match";

			var connectionA = new HubConnectionBuilder().WithUrl(hubUrl).Build();
			var connectionB = new HubConnectionBuilder().WithUrl(hubUrl).Build();

			var actionTypesSeen = new HashSet<string>();
			var playerIdsMulliganed = new HashSet<Guid>();
			var cardIdsSeen = new List<string>();
			var minionCardIdsSeen = new List<string>();
			var redactionFailures = new List<string>();
			var matchEndedA = new TaskCompletionSource<Guid?>();
			var matchEndedB = new TaskCompletionSource<Guid?>();
			var rng = new Random(12345);
			var lastSubmittedVersion = new Dictionary<HubConnection, int?>();
			Guid matchId = Guid.Empty;

			void CheckRedaction(PlayerGameView view, string who)
			{
				if (view.Opponent.Hand != null) redactionFailures.Add($"{who}: opponent hand was exposed.");
				if (view.Opponent.Secrets != null) redactionFailures.Add($"{who}: opponent secrets were exposed.");
			}

			void OnState(HubConnection connection, string who, TaskCompletionSource<Guid?> ended, PlayerGameView view)
			{
				CheckRedaction(view, who);

				foreach (var entry in view.NewHistory)
				{
					actionTypesSeen.Add(entry.ActionType);
					if (entry.ActionType == "SubmitMulliganAction")
					{
						playerIdsMulliganed.Add(entry.PlayerId);
					}
				}

				// Capture CardIds from cards in hand and minions on board
				if (view.Self.Hand != null)
				{
					foreach (var card in view.Self.Hand)
					{
						if (!string.IsNullOrEmpty(card.CardId))
						{
							cardIdsSeen.Add(card.CardId);
						}
					}
				}

				foreach (var minion in view.Self.Board)
				{
					if (!string.IsNullOrEmpty(minion.CardId))
					{
						minionCardIdsSeen.Add(minion.CardId);
					}
				}

				if (view.IsGameOver)
				{
					ended.TrySetResult(view.WinnerPlayerId);
					return;
				}

				if (view.LegalActions.Count == 0 || !view.PromptVersion.HasValue)
				{
					return;
				}

				if (lastSubmittedVersion.TryGetValue(connection, out var last) && last == view.PromptVersion)
				{
					return;
				}
				lastSubmittedVersion[connection] = view.PromptVersion;

				var chosenIndex = pickFirst ? 0 : rng.Next(view.LegalActions.Count);

				_ = connection.InvokeAsync<ActionResult>("SubmitAction", matchId, chosenIndex, view.PromptVersion.Value)
					.ContinueWith(t =>
					{
						if (t.IsCompletedSuccessfully && !t.Result.Success)
						{
							redactionFailures.Add($"{who}: server rejected a legal action - {t.Result.Error}");
						}
					});
			}

			connectionA.On<PlayerGameView>("OnStateUpdated", view => OnState(connectionA, "PlayerA", matchEndedA, view));
			connectionA.On<Guid?>("OnMatchEnded", winnerId => matchEndedA.TrySetResult(winnerId));
			connectionB.On<PlayerGameView>("OnStateUpdated", view => OnState(connectionB, "PlayerB", matchEndedB, view));
			connectionB.On<Guid?>("OnMatchEnded", winnerId => matchEndedB.TrySetResult(winnerId));

			await connectionA.StartAsync();
			await connectionB.StartAsync();

			try
			{
				matchId = await connectionA.InvokeAsync<Guid>("CreateMatch", BuildDeck("Alice"));
				var joinResult = await connectionB.InvokeAsync<JoinResult>("JoinMatch", matchId, BuildDeck("Bob"));
				Assert.IsTrue(joinResult.Success, $"JoinMatch failed: {joinResult.Error}");

				var completed = await Task.WhenAny(
					Task.WhenAll(matchEndedA.Task, matchEndedB.Task),
					Task.Delay(TimeSpan.FromSeconds(30)));

				Assert.IsTrue(matchEndedA.Task.IsCompleted && matchEndedB.Task.IsCompleted,
					"Match did not complete within 30 seconds (likely stuck on an unsubmittable prompt).");

				Assert.IsTrue(redactionFailures.Count == 0, string.Join(" | ", redactionFailures));

				Console.WriteLine("Action types exercised: " + string.Join(", ", actionTypesSeen.OrderBy(x => x)));

				Assert.IsTrue(actionTypesSeen.Contains("SubmitMulliganAction"), "Mulligan was never submitted over the wire.");
				Assert.IsTrue(actionTypesSeen.Contains("EndTurnAction"));
				Assert.IsTrue(actionTypesSeen.Contains("PlayCardAction"));

				// Verify both players submitted mulligans (regression guard for ENG-002)
				Assert.IsTrue(playerIdsMulliganed.Count == 2, $"Both players must submit mulligans, but only {playerIdsMulliganed.Count} player(s) did.");

				// Verify CardId is populated on at least one CardView and one MinionView (regression guard for ENG-001)
				Assert.IsTrue(cardIdsSeen.Count > 0, "No CardView had a non-null CardId during the playthrough.");
				Assert.IsTrue(minionCardIdsSeen.Count > 0, "No MinionView had a non-null CardId during the playthrough.");
			}
			finally
			{
				await connectionA.StopAsync();
				await connectionB.StopAsync();
			}
		}
		finally
		{
			await app.StopAsync();
			await app.DisposeAsync();
		}
	}

	private static DecklistRequest BuildDeck(string name)
	{
		return new DecklistRequest
		{
			PlayerName = name,
			Minions = new List<CardCount>
			{
				new() { CardId = "Wisp", Count = 3 },
				new() { CardId = "RiverCrocolisk", Count = 3 },
				new() { CardId = "ChillwindYeti", Count = 2 },
				new() { CardId = "BoulderfistOgre", Count = 2 },
			},
			Spells = new List<CardCount>
			{
				new() { CardId = "ArcaneIntellect", Count = 2 },
			},
		};
	}
}
