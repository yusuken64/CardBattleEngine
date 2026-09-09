using System.Runtime.InteropServices;
using System.Runtime.Versioning;
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

	// Rows the last prompt+menu occupied below the block, so they can be cleared once this
	// player has no prompt of their own - otherwise they'd sit there as a stale leftover.
	private static int _lastPromptAreaLines;

	// When set, we have no legal actions of our own (their turn, or their pending choice) - records
	// when that started so the idle ticker below can show a live "waiting for opponent (Ns)" count.
	// Null means it's our own turn (or the game is over), so there's nothing to wait on.
	private static DateTime? _waitStartUtc;

	// Row where the fixed-size history log starts in live-view mode. Set once, never moved.
	private static int? _logRow;

	// True when the last view we saw handed us legal actions - tracked separately from
	// _waitStartUtc (list mode doesn't set that) so both view modes can detect the same
	// "it just became my turn" edge and steal focus for this window.
	private static bool _hadLegalActions;

	private const int HistoryLogLines = 8;

	// Oldest first, capped at HistoryLogLines.
	private static readonly List<string> _historyLog = new();

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
			lock (_consoleLock)
			{
				ClearPromptArea();
			}
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
		var resultMessage = winnerId == null
			? "Game over. Draw."
			: winnerId == latestView?.ViewerPlayerId
				? "Game over. You win!"
				: "Game over. You lose.";
		Console.WriteLine(resultMessage);

		await connection.StopAsync();

		Console.WriteLine("Press any key to close this window...");
		Console.ReadKey(true);
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
		var hasLegalActions = !view.IsGameOver && view.LegalActions.Count > 0;
		if (hasLegalActions && !_hadLegalActions)
		{
			FocusConsoleWindow();
		}
		_hadLegalActions = hasLegalActions;

		if (_useListView)
		{
			PrintStateAsList(view);
		}
		else
		{
			PrintStateInPlace(view);
		}
	}

	// Steals focus for this process's console window so whichever player's turn it is - the one
	// with something to act on - is the window in front, instead of leaving that to alt-tab. Windows
	// blocks background processes from calling SetForegroundWindow directly; attaching to the current
	// foreground window's input queue first is the standard way around that restriction.
	private static void FocusConsoleWindow()
	{
		if (!OperatingSystem.IsWindows())
		{
			return;
		}

		var hWnd = NativeMethods.GetConsoleWindow();
		var foregroundWindow = NativeMethods.GetForegroundWindow();
		if (hWnd == IntPtr.Zero || hWnd == foregroundWindow)
		{
			return;
		}

		var foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
		var currentThreadId = NativeMethods.GetCurrentThreadId();

		NativeMethods.ShowWindow(hWnd, NativeMethods.SW_RESTORE);

		if (foregroundThreadId != 0 && foregroundThreadId != currentThreadId)
		{
			NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
			NativeMethods.SetForegroundWindow(hWnd);
			NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
		}
		else
		{
			NativeMethods.SetForegroundWindow(hWnd);
		}
	}

	[SupportedOSPlatform("windows")]
	private static class NativeMethods
	{
		public const int SW_RESTORE = 9;

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetConsoleWindow();

		[DllImport("kernel32.dll")]
		public static extern uint GetCurrentThreadId();

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
	}

	// Debug view: append every broadcast unfiltered, so the full sequence the server actually sent
	// (including ones that look redundant) is visible in the scrollback.
	private static void PrintStateAsList(PlayerGameView view)
	{
		PrintHistoryLines(view);

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

	// Live view (default): redraw a fixed-size block in place instead of appending.
	private static void PrintStateInPlace(PlayerGameView view)
	{
		_waitStartUtc = (!view.IsGameOver && view.LegalActions.Count == 0)
			? (_waitStartUtc ?? DateTime.UtcNow)
			: null;

		_logRow ??= Console.CursorTop;

		PrintHistoryLinesAnchored(view);

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

		var top = _logRow.Value + HistoryLogLines;
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

		if ((view.LegalActions.Count == 0 || view.IsGameOver) && _lastPromptAreaLines > 0)
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

	private static void ClearPromptArea()
	{
		if (_useListView || _logRow == null || _lastPromptAreaLines == 0)
		{
			return;
		}

		var nextRow = _logRow.Value + HistoryLogLines + StateBlockLineCount;
		for (int i = 0; i < _lastPromptAreaLines; i++)
		{
			int row = nextRow + i;
			if (row >= Console.BufferHeight) break;
			Console.SetCursorPosition(0, row);
			Console.Write(new string(' ', Console.BufferWidth - 1));
		}
		_lastPromptAreaLines = 0;
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
			if (_waitStartUtc == null || _logRow == null)
			{
				return;
			}

			var top = _logRow.Value + HistoryLogLines;
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

	private static void PrintHistoryLines(PlayerGameView view)
	{
		foreach (var entry in view.NewHistory)
		{
			var line = FormatHistoryLine(entry, view);
			if (line != null)
			{
				Console.WriteLine(line);
			}
		}
	}

	private static void PrintHistoryLinesAnchored(PlayerGameView view)
	{
		foreach (var entry in view.NewHistory)
		{
			var line = FormatHistoryLine(entry, view);
			if (line == null)
			{
				continue;
			}

			_historyLog.Add(line);
			if (_historyLog.Count > HistoryLogLines)
			{
				_historyLog.RemoveAt(0);
			}
		}

		for (int i = 0; i < HistoryLogLines; i++)
		{
			int row = _logRow!.Value + i;
			if (row >= Console.BufferHeight) break;

			Console.SetCursorPosition(0, row);
			Console.Write(new string(' ', Console.BufferWidth - 1));
			Console.SetCursorPosition(0, row);
			if (i < _historyLog.Count)
			{
				Console.Write(_historyLog[i]);
			}
		}
	}

	private static string FormatHistoryLine(HistoryEntryView entry, PlayerGameView view)
	{
		var who = entry.PlayerId == view.ViewerPlayerId ? "You" : view.Opponent.Name;

		return entry.ActionType switch
		{
			"PlayCardAction" => entry.TargetName != null
				? $"{who} played {entry.SourceName} on {entry.TargetName}"
				: $"{who} played {entry.SourceName}",
			"AttackAction" => $"{who} attacked {entry.TargetName} with {entry.SourceName}",
			"HeroPowerAction" => $"{who} used their Hero Power",
			"EndTurnAction" => $"{who} ended their turn",
			"SubmitMulliganAction" => $"{who} finished mulligan",
			"SecretPlayed" => $"{who} played a Secret",
			_ => null,
		};
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
