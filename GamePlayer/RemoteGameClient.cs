using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using CardBattleEngine;
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
		var matchFound = new TaskCompletionSource<string>();

		// Set once CreateOrJoinMatch resolves below - the closures here capture this variable (not
		// its value at registration time), so requests sent from OnStateUpdated before that point are
		// simply skipped by RequestArtForUnseenOpponentCards's empty match ID guard.
		var activeMatchId = string.Empty;
		var requestedCardArtIds = new HashSet<string>();

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
			RequestArtForUnseenOpponentCards(connection, activeMatchId, view, requestedCardArtIds);
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
		connection.On<string>("OnMatchFound", id => matchFound.TrySetResult(id));

		// GamePlayer has no real card art to show - it exercises the same request/relay round trip a
		// visual client would, but responds with a deterministic hash of the cardId instead of image
		// bytes, so the requester can verify the exact bytes it gets back came from this cardId.
		connection.On<string, string>("OnCardArtRequested", (requestMatchId, cardId) =>
			RespondToCardArtRequest(connection, requestMatchId, cardId));
		connection.On<string, byte[]>("OnCardArtReceived", (cardId, imageBytes) =>
			VerifyReceivedCardArt(cardId, imageBytes));

		Console.WriteLine($"Connecting to {hubUrl} ...");
		await connection.StartAsync();

		var matchId = await CreateOrJoinMatch(connection, playerName, matchFound.Task);
		if (matchId == null)
		{
			await connection.StopAsync();
			return;
		}

		activeMatchId = matchId;

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
				if (view.PendingChoice != null)
				{
					// Pending-choice options (mulligan toggles, Discover picks, ...) aren't
					// verified to group safely by (ActionType, Source) - fall back to the flat picker.
					_lastPromptAreaLines = 1 + view.LegalActions.Count;
					chosen = HumanAgent.SelectFromList(
						view.LegalActions,
						"Select an action:",
						a => a.DisplayName ?? a.ActionType);
				}
				else
				{
					chosen = HumanAgent.SelectGrouped(
						view.LegalActions,
						keySelector: static (a) => (a.ActionType, a.SourceEntityId),
						groupLabel: static (key, items) => DescribeActionGroup(key.ActionType, items[0]),
						// Stage-2 text doesn't vary by target for non-Attack actions (DisplayName is
						// built without target info) - a pre-existing ambiguity, not introduced here.
						itemLabel: static (a) => a.DisplayName ?? a.ActionType,
						stage1Prompt: "Select an action:",
						stage2Prompt: "Select a target:",
						rowsDrawn: out _lastPromptAreaLines);
				}
			}

			lastHandledVersion = view.PromptVersion;

			var result = await connection.InvokeAsync<ActionResult>("SubmitAction", matchId, chosen.Index, view.PromptVersion.Value);
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

	private static async Task<string?> CreateOrJoinMatch(HubConnection connection, string playerName, Task<string> matchFound)
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
			Console.Write("Enter match code (e.g. K7MP-4X): ");
			var input = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(input))
			{
				Console.WriteLine("Invalid match id.");
				return null;
			}

			var joinResult = await connection.InvokeAsync<JoinResult>("JoinMatch", input, deck);
			if (!joinResult.Success)
			{
				Console.WriteLine($"Failed to join match: {joinResult.Error}");
				return null;
			}

			return joinResult.MatchId;
		}

		var createdId = await connection.InvokeAsync<string>("CreateMatch", deck);
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
		foreach (var line in BuildHistoryLines(view.NewHistory, view))
		{
			Console.WriteLine(line);
		}
	}

	private static void PrintHistoryLinesAnchored(PlayerGameView view)
	{
		foreach (var line in BuildHistoryLines(view.NewHistory, view))
		{
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

	private static List<string> BuildHistoryLines(IReadOnlyList<HistoryEntryView> entries, PlayerGameView view)
	{
		var lines = new List<string>();

		for (int i = 0; i < entries.Count; i++)
		{
			var entry = entries[i];

			// A basic minion attack resolves as two separate DamageAction entries - the attacker's
			// hit and the defender's retaliation - back to back in the same batch. Collapse that
			// pair into one combat line instead of reporting them as two unrelated damage events.
			if (entry.ActionType == "DamageAction" && i + 1 < entries.Count)
			{
				var next = entries[i + 1];
				if (next.ActionType == "DamageAction" &&
					entry.SourceId != null && entry.TargetId != null &&
					entry.SourceId == next.TargetId && entry.TargetId == next.SourceId)
				{
					lines.Add($"Combat: {entry.SourceName} ({entry.DamageDealt ?? 0}) vs {next.SourceName} ({next.DamageDealt ?? 0})");
					i++;
					continue;
				}
			}

			var line = FormatHistoryLine(entry, view);
			if (line != null)
			{
				lines.Add(line);
			}
		}

		return lines;
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
			// Divine Shield can reduce a real hit to 0 actual damage - skip those, there's nothing to report.
			"DamageAction" when entry.DamageDealt is > 0 && entry.SourceName != null && entry.TargetName != null =>
				$"{entry.SourceName} dealt {entry.DamageDealt} damage to {entry.TargetName}",
			"DeathAction" when entry.TargetName != null => $"{entry.TargetName} died",
			// A draw is recorded as DrawCardFromDeckAction -> GainCardAction sharing one context - key
			// off GainCardAction only (it also covers non-deck "add a card to hand" effects) so a draw
			// doesn't produce two lines. CardGainedName is null for the opponent's draws (redacted
			// server-side), so fall back to generic wording there.
			"GainCardAction" => entry.CardGainedName != null
				? $"{who} drew {entry.CardGainedName}"
				: $"{who} drew a card",
			_ => null,
		};
	}

	// AttackAction's DisplayName is always exactly "Attack: {source} -> {target}" (ActionDisplay's
	// fixed format), so splitting on " -> " and rewording the prefix reliably isolates the
	// attacker's description for every item in the group.
	private static string DescribeActionGroup(string actionType, LegalActionView representative)
	{
		if (actionType == "AttackAction")
		{
			var text = representative.DisplayName ?? "";
			const string prefix = "Attack: ";
			const string separator = " -> ";
			var sepIndex = text.IndexOf(separator, StringComparison.Ordinal);
			if (text.StartsWith(prefix, StringComparison.Ordinal) && sepIndex > prefix.Length)
			{
				return "Attack with " + text.Substring(prefix.Length, sepIndex - prefix.Length);
			}
			return text;
		}

		if (actionType == "EndTurnAction")
		{
			return "End Turn";
		}

		return representative.DisplayName ?? representative.ActionType;
	}

	private static string FormatPlayer(string label, PublicPlayerView player, bool isCurrentTurn)
	{
		var turnMarker = isCurrentTurn ? "*" : " ";
		return $"{turnMarker}{label,-10} HP:{player.Health}/{player.MaxHealth} Armor:{player.Armor} Mana:{player.Mana}/{player.MaxMana} " +
			$"Hand:{player.HandCount} Deck:{player.DeckCount} Secrets:{player.SecretCount} " +
			$"Board:[{string.Join(",", player.Board.Select(m => $"{m.Name} {m.Attack}/{m.Health}"))}]";
	}

	// Card art for anything the opponent has revealed - a minion currently on their board, one that
	// already died, their equipped weapon, or a spell they just cast - counts as "seen" once
	// requested here; opponent hand contents are never visible (PlayerViewBuilder nulls Opponent.Hand),
	// so there is nothing else to request art for.
	private static void RequestArtForUnseenOpponentCards(
		HubConnection connection, string matchId, PlayerGameView view, HashSet<string> requestedCardArtIds)
	{
		if (string.IsNullOrEmpty(matchId))
		{
			return;
		}

		foreach (var minion in view.Opponent.Board)
		{
			RequestArtIfUnseen(connection, matchId, minion.CardId, requestedCardArtIds);
		}

		foreach (var minion in view.Opponent.Graveyard)
		{
			RequestArtIfUnseen(connection, matchId, minion.CardId, requestedCardArtIds);
		}

		RequestArtIfUnseen(connection, matchId, view.Opponent.EquippedWeapon?.CardId, requestedCardArtIds);

		// A cast spell has no persistent view (unlike a minion's Board entry or a weapon's
		// EquippedWeapon) - the history entry recording the opponent's own play is the only place
		// its CardId is ever exposed.
		foreach (var entry in view.NewHistory)
		{
			if (entry.ActionType == "CastSpellAction" && entry.PlayerId != view.ViewerPlayerId)
			{
				RequestArtIfUnseen(connection, matchId, entry.SourceCardId, requestedCardArtIds);
			}
		}
	}

	private static async void RequestArtIfUnseen(
		HubConnection connection, string matchId, string? cardId, HashSet<string> requestedCardArtIds)
	{
		if (string.IsNullOrEmpty(cardId) || !requestedCardArtIds.Add(cardId))
		{
			return;
		}

		try
		{
			await connection.InvokeAsync("RequestCardArt", matchId, cardId);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"RequestCardArt failed for '{cardId}': {ex.Message}");
		}
	}

	// The other client is asking us for art for cardId. GamePlayer has no real art to send, so it
	// replies with a hash of cardId instead - a "verifiable blob" the requester can independently
	// recompute from the cardId it asked for, proving the relay round trip delivered the right bytes
	// for the right card rather than actually rendering anything.
	private static async void RespondToCardArtRequest(HubConnection connection, string matchId, string cardId)
	{
		byte[] blob = ComputeVerifiableArtBlob(cardId);

		try
		{
			await connection.InvokeAsync("SubmitCardArt", matchId, cardId, blob);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SubmitCardArt failed for '{cardId}': {ex.Message}");
		}
	}

	private static void VerifyReceivedCardArt(string cardId, byte[] imageBytes)
	{
		var expected = ComputeVerifiableArtBlob(cardId);
		var verified = imageBytes != null && imageBytes.AsSpan().SequenceEqual(expected);
		Console.WriteLine(verified
			? $"Card art round trip verified for '{cardId}'."
			: $"Card art round trip FAILED for '{cardId}' (received {imageBytes?.Length ?? 0} bytes).");
	}

	private static byte[] ComputeVerifiableArtBlob(string cardId) => SHA256.HashData(Encoding.UTF8.GetBytes(cardId));

	private static DecklistRequest BuildDefaultDeck(string playerName)
	{
		var deck = new DecklistRequest
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

		AddCustomCards(deck);
		return deck;
	}

	// GamePlayer authors a couple of cards that live only in this process, not in the server's
	// on-disk CardDatabase, and sends their definitions alongside the ids that reference them -
	// the same custom-card path a real content-authoring client would use. MatchRegistry resolves
	// them purely from what each client provides here, so nothing needs to exist server-side ahead
	// of time.
	private static void AddCustomCards(DecklistRequest deck)
	{
		var customMinion = new MinionCard("GamePlayerPrototypeGolem", cost: 4, attack: 4, health: 6);
		deck.Minions.Add(new CardCount { CardId = customMinion.Name, Count = 2 });
		deck.CustomMinions.Add(CardDatabase.ToDefinitionJson(
			CardDatabase.ToMinionCardDefinition(customMinion, customMinion.Name)));

		var customSpell = new SpellCard("GamePlayerPrototypeBolt", cost: 1);
		deck.Spells.Add(new CardCount { CardId = customSpell.Name, Count = 2 });
		deck.CustomSpells.Add(CardDatabase.ToDefinitionJson(
			CardDatabase.ToSpellCardDefinition(customSpell, customSpell.Name)));
	}
}
