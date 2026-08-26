using CardBattleEngine.View;
using GameServer.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

string serverUrl = args.Length > 0 ? args[0] : "http://localhost:5299";
string hubUrl = $"{serverUrl}/hubs/match";

Console.WriteLine($"Connecting two clients to {hubUrl} ...");

var connectionA = new HubConnectionBuilder().WithUrl(hubUrl).Build();
var connectionB = new HubConnectionBuilder().WithUrl(hubUrl).Build();

var matchEndedA = new TaskCompletionSource<Guid?>();
var matchEndedB = new TaskCompletionSource<Guid?>();

bool failed = false;
void Fail(string message)
{
	failed = true;
	Console.WriteLine($"FAIL: {message}");
}

void CheckRedaction(PlayerGameView view, string who)
{
	if (view.Opponent.Hand != null)
		Fail($"{who}: opponent hand contents were exposed!");
	if (view.Opponent.Secrets != null)
		Fail($"{who}: opponent secret contents were exposed!");
}

Guid matchId = Guid.Empty;
bool autoPlayEnabled = false; // held off until the spoofed-action checks below finish, so they race against nothing.

void TrySubmitFirstLegalAction(HubConnection connection, string who, PlayerGameView view)
{
	if (!autoPlayEnabled || view.LegalActions.Count == 0 || !view.PromptVersion.HasValue)
	{
		return;
	}

	_ = connection.InvokeAsync<ActionResult>("SubmitAction", matchId, view.LegalActions[0].Index, view.PromptVersion.Value)
		.ContinueWith(t =>
		{
			if (t.IsCompletedSuccessfully && !t.Result.Success)
			{
				Fail($"{who}: server rejected a legitimate action - {t.Result.Error}");
			}
		});
}

void WireHandlers(HubConnection connection, string who, TaskCompletionSource<Guid?> ended)
{
	connection.On<PlayerGameView>("OnStateUpdated", view =>
	{
		CheckRedaction(view, who);

		if (view.IsGameOver)
		{
			ended.TrySetResult(view.WinnerPlayerId);
			return;
		}

		TrySubmitFirstLegalAction(connection, who, view);
	});

	connection.On<string>("OnActionRejected", reason => Console.WriteLine($"{who}: action rejected - {reason}"));
	connection.On<Guid?>("OnMatchEnded", winnerId => ended.TrySetResult(winnerId));
}

WireHandlers(connectionA, "PlayerA", matchEndedA);
WireHandlers(connectionB, "PlayerB", matchEndedB);

await connectionA.StartAsync();
await connectionB.StartAsync();

var deckA = BuildDeck("Alice");
var deckB = BuildDeck("Bob");

matchId = await connectionA.InvokeAsync<Guid>("CreateMatch", deckA);
Console.WriteLine($"Match created: {matchId}");

var joinResult = await connectionB.InvokeAsync<JoinResult>("JoinMatch", matchId, deckB);
if (!joinResult.Success)
{
	Console.WriteLine($"FAIL: JoinMatch failed - {joinResult.Error}");
	return 1;
}

Console.WriteLine("Match joined - running spoofed-action rejection checks...");

var stateBefore = await connectionA.InvokeAsync<PlayerGameView?>("GetState", matchId);

var staleVersionResult = await connectionB.InvokeAsync<ActionResult>("SubmitAction", matchId, 0, -999);
if (staleVersionResult.Success)
	Fail("Server accepted a SubmitAction with an obviously-wrong version number.");

var outOfRangeResult = await connectionB.InvokeAsync<ActionResult>("SubmitAction", matchId, -1, -999);
if (outOfRangeResult.Success)
	Fail("Server accepted a SubmitAction with a negative action index.");

var stateAfter = await connectionA.InvokeAsync<PlayerGameView?>("GetState", matchId);
if (stateBefore != null && stateAfter != null &&
	(stateBefore.Turn != stateAfter.Turn || stateBefore.CurrentPlayerId != stateAfter.CurrentPlayerId))
{
	Fail("Match state changed as a result of a rejected/spoofed action.");
}

Console.WriteLine(failed ? "Spoofed-action checks FAILED." : "Spoofed-action checks passed.");
Console.WriteLine("Playing the match to completion (auto-picking LegalActions[0] each turn)...");

autoPlayEnabled = true;

// A real prompt may have already arrived (and been ignored) while auto-play was gated off above -
// re-fetch and process each connection's current view once so the game loop isn't stuck waiting
// on a push that already happened.
var catchUpA = await connectionA.InvokeAsync<PlayerGameView?>("GetState", matchId);
if (catchUpA != null) TrySubmitFirstLegalAction(connectionA, "PlayerA", catchUpA);
var catchUpB = await connectionB.InvokeAsync<PlayerGameView?>("GetState", matchId);
if (catchUpB != null) TrySubmitFirstLegalAction(connectionB, "PlayerB", catchUpB);

var timeout = Task.Delay(TimeSpan.FromSeconds(30));
var completed = await Task.WhenAny(Task.WhenAll(matchEndedA.Task, matchEndedB.Task), timeout);

if (completed == timeout)
{
	Fail("Match did not complete within 30 seconds.");
}
else
{
	var winnerId = await matchEndedA.Task;
	Console.WriteLine(winnerId.HasValue ? $"Match complete. Winner: {winnerId}" : "Match complete. Draw.");
}

await connectionA.StopAsync();
await connectionB.StopAsync();

Console.WriteLine(failed ? "RESULT: FAIL" : "RESULT: PASS");
return failed ? 1 : 0;

static DecklistRequest BuildDeck(string name)
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
