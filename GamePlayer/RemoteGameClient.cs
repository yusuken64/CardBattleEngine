using CardBattleEngine.View;
using GameServer.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

// Drives a match against a remote GameServer instead of a local GameEngine - the human player only
// ever sees the redacted PlayerGameView the server sends, and only ever submits a
// (matchId, actionIndex, PromptVersion) triple, the same protocol GameServer.TestClient uses.
public static class RemoteGameClient
{
	// Selects PrintState's rendering mode. Live mode (default) redraws the status block in place;
	// list mode restores the old scrolling-append behavior with no dedup, for debugging - it shows
	// literally every broadcast the server sends, redundant or not.
	private static bool _useListView;

	// PrintState runs on the SignalR callback thread (fired from OnStateUpdated) while
	// HumanAgent.SelectFromList's redraw loop runs on the main thread - without this, a push
	// arriving mid-redraw can interleave writes with the menu and corrupt the console. Since nothing
	// else can legitimately change this player's own state while they're mid-prompt (the server
	// blocks on this player's response), contention here should be rare in practice.
	private static readonly object _consoleLock = new();

	// How many rows below the state block the last-drawn prompt ("Select an action:" + its menu
	// options) occupied. When a later push has no prompt of its own (not this player's turn), the
	// prompt/menu from the last turn would otherwise just sit there as a stale leftover forever,
	// since PrintStateInPlace only ever redraws its own fixed 6 rows above it.
	private static int _lastPromptAreaLines;

	// When set, we have no legal actions of our own (their turn, or their pending choice) - records
	// when that started so the idle ticker below can show a live "waiting for opponent (Ns)" count.
	// Null means it's our own turn (or the game is over), so there's nothing to wait on.
	private static DateTime? _waitStartUtc;

	public static async Task RunAsync(string serverUrl, string playerName, bool useListView = false)
	{
		_useListView = useListView;

		var hubUrl = $"{serverUrl}/hubs/match";
		var connection = new HubConnectionBuilder().WithUrl(hubUrl).Build();

		PlayerGameView? latestView = null;
		var viewSignal = new SemaphoreSlim(0);
		var matchEnded = new TaskCompletionSource<Guid?>();
		var matchFound = new TaskCompletionSource<Guid>();

		connection.On<PlayerGameView>("OnStateUpdated", view =>
		{
			latestView = view;
			lock (_consoleLock)
			{
				PrintState(view);
			}
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

		// Redraws just the idle-wait line once a second so it live-counts up between server pushes,
		// rather than only ever updating when a broadcast happens to arrive.
		_ = Task.Run(async () =>
		{
			while (!matchEnded.Task.IsCompleted)
			{
				await Task.Delay(1000);
				RedrawWaitingLine();
			}
		});

		// Every engine Resolve() broadcasts to both players regardless of whether this player's own
		// legal actions changed, so several pushes in a row can carry the same still-unanswered (or
		// already-answered) PromptVersion. Track the last version we've acted on so those redundant
		// wakeups don't re-show/re-submit a prompt that's already been handled.
		int? lastHandledVersion = null;

		while (!matchEnded.Task.IsCompleted)
		{
			await viewSignal.WaitAsync();
			if (matchEnded.Task.IsCompleted) break;

			var view = latestView;
			if (view == null || view.LegalActions.Count == 0 || !view.PromptVersion.HasValue ||
				view.PromptVersion == lastHandledVersion)
			{
				continue;
			}

			LegalActionView chosen;
			lock (_consoleLock)
			{
				_lastPromptAreaLines = 1 + view.LegalActions.Count;
				chosen = HumanAgent.SelectFromList(
					view.LegalActions,
					"Select an action:",
					a => a.DisplayName ?? a.ActionType);
			}

			lastHandledVersion = view.PromptVersion;

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
		if (_useListView)
		{
			PrintStateAsList(view);
		}
		else
		{
			PrintStateInPlace(view);
		}
	}

	// Debug view: append every broadcast unfiltered, so the full sequence the server actually sent
	// (including ones that look redundant) is visible in the scrollback.
	private static void PrintStateAsList(PlayerGameView view)
	{
		Console.WriteLine();
		Console.WriteLine($"--- Turn {view.Turn} ---");
		Console.WriteLine(FormatPlayer("You", view.Self, view.CurrentPlayerId == view.ViewerPlayerId));
		Console.WriteLine(FormatPlayer("Opponent", view.Opponent, view.CurrentPlayerId != view.ViewerPlayerId));

		if (view.Self.Hand != null)
		{
			Console.WriteLine("Your hand: " + string.Join(", ", view.Self.Hand.Select(c => $"{c.Name}({c.ManaCost})")));
		}

		if (view.OpponentIsChoosing)
		{
			Console.WriteLine("Opponent is making a choice...");
		}
	}

	private const int StateBlockLineCount = 6;

	// Live view (default): redraw a fixed-size block in place instead of appending. Anchored to
	// Console.WindowTop (re-read fresh each call, not a cached absolute row) - the same reference
	// point HumanAgent.SelectFromList uses for its menu - so the block stays pinned to the top of
	// whatever's currently visible even as the console auto-scrolls.
	private static void PrintStateInPlace(PlayerGameView view)
	{
		_waitStartUtc = (!view.IsGameOver && view.LegalActions.Count == 0)
			? (_waitStartUtc ?? DateTime.UtcNow)
			: null;

		var lines = new[]
		{
			"",
			$"--- Turn {view.Turn} ---",
			FormatPlayer("You", view.Self, view.CurrentPlayerId == view.ViewerPlayerId),
			FormatPlayer("Opponent", view.Opponent, view.CurrentPlayerId != view.ViewerPlayerId),
			view.Self.Hand != null
				? "Your hand: " + string.Join(", ", view.Self.Hand.Select(c => $"{c.Name}({c.ManaCost})"))
				: "",
			BuildWaitingLine(),
		};

		var top = Console.WindowTop;
		for (int i = 0; i < lines.Length; i++)
		{
			int row = top + i;
			if (row >= Console.BufferHeight) break;

			Console.SetCursorPosition(0, row);
			Console.Write(new string(' ', Console.BufferWidth - 1));
			Console.SetCursorPosition(0, row);
			Console.Write(lines[i]);
		}

		var nextRow = top + StateBlockLineCount;

		if (view.LegalActions.Count == 0 && _lastPromptAreaLines > 0)
		{
			for (int i = 0; i < _lastPromptAreaLines; i++)
			{
				int row = nextRow + i;
				if (row >= Console.BufferHeight) break;
				Console.SetCursorPosition(0, row);
				Console.Write(new string(' ', Console.BufferWidth - 1));
			}
			_lastPromptAreaLines = 0;
		}

		if (nextRow < Console.BufferHeight)
		{
			Console.SetCursorPosition(0, nextRow);
		}
	}

	private static string BuildWaitingLine()
	{
		if (_waitStartUtc == null)
		{
			return "";
		}

		var elapsedSeconds = (int)(DateTime.UtcNow - _waitStartUtc.Value).TotalSeconds;
		return $"Waiting for opponent ({elapsedSeconds}s)...";
	}

	// Called every second from a background ticker so the wait count keeps live-updating between
	// server pushes, not just whenever one happens to arrive.
	private static void RedrawWaitingLine()
	{
		if (_useListView || _waitStartUtc == null)
		{
			return;
		}

		lock (_consoleLock)
		{
			if (_waitStartUtc == null)
			{
				return;
			}

			var top = Console.WindowTop;
			int row = top + StateBlockLineCount - 1;
			if (row >= Console.BufferHeight)
			{
				return;
			}

			Console.SetCursorPosition(0, row);
			Console.Write(new string(' ', Console.BufferWidth - 1));
			Console.SetCursorPosition(0, row);
			Console.Write(BuildWaitingLine());
			Console.SetCursorPosition(0, top + StateBlockLineCount);
		}
	}

	private static string FormatPlayer(string label, PublicPlayerView player, bool isCurrentTurn)
	{
		var turnMarker = isCurrentTurn ? "*" : " ";
		return $"{turnMarker}{label,-10} HP:{player.Health}/{player.MaxHealth} Armor:{player.Armor} Mana:{player.Mana}/{player.MaxMana} " +
			$"Hand:{player.HandCount} Deck:{player.DeckCount} Secrets:{player.SecretCount} " +
			$"Board:[{string.Join(",", player.Board.Select(m => $"{m.Name} {m.Attack}/{m.Health}"))}]";
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
