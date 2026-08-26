using CardBattleEngine.View;
using GameServer.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

// Drives a match against a remote GameServer instead of a local GameEngine - the human player only
// ever sees the redacted PlayerGameView the server sends, and only ever submits a
// (matchId, actionIndex, PromptVersion) triple, the same protocol GameServer.TestClient uses.
public static class RemoteGameClient
{
	public static async Task RunAsync(string serverUrl, string playerName)
	{
		var hubUrl = $"{serverUrl}/hubs/match";
		var connection = new HubConnectionBuilder().WithUrl(hubUrl).Build();

		PlayerGameView? latestView = null;
		var viewSignal = new SemaphoreSlim(0);
		var matchEnded = new TaskCompletionSource<Guid?>();
		var matchFound = new TaskCompletionSource<Guid>();

		connection.On<PlayerGameView>("OnStateUpdated", view =>
		{
			latestView = view;
			PrintState(view);
			if (view.IsGameOver)
			{
				matchEnded.TrySetResult(view.WinnerPlayerId);
			}
			viewSignal.Release();
		});
		connection.On<Guid?>("OnMatchEnded", winnerId =>
		{
			matchEnded.TrySetResult(winnerId);
			viewSignal.Release();
		});
		connection.On<string>("OnActionRejected", reason => Console.WriteLine($"Action rejected: {reason}"));
		connection.On<Guid>("OnMatchFound", id => matchFound.TrySetResult(id));

		Console.WriteLine($"Connecting to {hubUrl} ...");
		await connection.StartAsync();

		var matchId = await CreateOrJoinMatch(connection, playerName, matchFound.Task);
		if (matchId == null)
		{
			await connection.StopAsync();
			return;
		}

		while (!matchEnded.Task.IsCompleted)
		{
			await viewSignal.WaitAsync();
			if (matchEnded.Task.IsCompleted) break;

			var view = latestView;
			if (view == null || view.LegalActions.Count == 0 || !view.PromptVersion.HasValue)
			{
				continue;
			}

			var chosen = HumanAgent.SelectFromList(
				view.LegalActions,
				"Select an action:",
				a => a.DisplayName ?? a.ActionType);

			var result = await connection.InvokeAsync<ActionResult>("SubmitAction", matchId.Value, chosen.Index, view.PromptVersion.Value);
			if (!result.Success)
			{
				Console.WriteLine($"Action rejected: {result.Error}");
			}
		}

		var winnerId = await matchEnded.Task;
		Console.WriteLine(winnerId.HasValue ? $"Game over. Winner: {winnerId}" : "Game over. Draw.");

		await connection.StopAsync();
	}

	private static async Task<Guid?> CreateOrJoinMatch(HubConnection connection, string playerName, Task<Guid> matchFound)
	{
		var deck = BuildDefaultDeck(playerName);

		Console.WriteLine("Quick match (Q), create a new match (C), or join an existing one (J)?");
		var key = Console.ReadKey(true).KeyChar;

		if (key is 'q' or 'Q')
		{
			await connection.InvokeAsync("JoinQueue", deck);
			Console.WriteLine("Searching for an opponent...");
			var matchId = await matchFound;
			Console.WriteLine($"Match found: {matchId}");
			return matchId;
		}

		if (key is 'j' or 'J')
		{
			Console.Write("Enter match id: ");
			var input = Console.ReadLine();
			if (!Guid.TryParse(input, out var matchId))
			{
				Console.WriteLine("Invalid match id.");
				return null;
			}

			var joinResult = await connection.InvokeAsync<JoinResult>("JoinMatch", matchId, deck);
			if (!joinResult.Success)
			{
				Console.WriteLine($"Failed to join match: {joinResult.Error}");
				return null;
			}

			return matchId;
		}

		var createdId = await connection.InvokeAsync<Guid>("CreateMatch", deck);
		Console.WriteLine($"Match created: {createdId}");
		Console.WriteLine("Share this id with your opponent and wait for them to join...");
		return createdId;
	}

	private static void PrintState(PlayerGameView view)
	{
		Console.WriteLine();
		Console.WriteLine($"--- Turn {view.Turn} ---");
		PrintPlayer("You", view.Self, view.CurrentPlayerId == view.ViewerPlayerId);
		PrintPlayer("Opponent", view.Opponent, view.CurrentPlayerId != view.ViewerPlayerId);

		if (view.Self.Hand != null)
		{
			Console.WriteLine("Your hand: " + string.Join(", ", view.Self.Hand.Select(c => $"{c.Name}({c.ManaCost})")));
		}

		if (view.OpponentIsChoosing)
		{
			Console.WriteLine("Opponent is making a choice...");
		}
	}

	private static void PrintPlayer(string label, PublicPlayerView player, bool isCurrentTurn)
	{
		var turnMarker = isCurrentTurn ? "*" : " ";
		Console.WriteLine(
			$"{turnMarker}{label,-10} HP:{player.Health}/{player.MaxHealth} Armor:{player.Armor} Mana:{player.Mana}/{player.MaxMana} " +
			$"Hand:{player.HandCount} Deck:{player.DeckCount} Secrets:{player.SecretCount} " +
			$"Board:[{string.Join(",", player.Board.Select(m => $"{m.Name} {m.Attack}/{m.Health}"))}]");
	}

	private static DecklistRequest BuildDefaultDeck(string playerName)
	{
		return new DecklistRequest
		{
			PlayerName = playerName,
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
