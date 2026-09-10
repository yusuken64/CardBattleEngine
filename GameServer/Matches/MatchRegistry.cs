using System.Collections.Concurrent;
using CardBattleEngine;
using GameServer.Contracts;

namespace GameServer.Matches;

// GameState construction is deferred until BOTH decklists are known (see PendingMatch) rather than
// building it at CreateMatch time with a placeholder empty deck for player 2 - GameState's
// constructor captures its CardDB catalog once, from the decks passed in, so building it early would
// permanently miss player 2's cards from anything that draws from the full card pool (e.g. a
// CardDBProvider-backed Discover).
public class MatchRegistry
{
	private readonly CardDatabase _cardDb;
	private readonly ConcurrentDictionary<MatchId, PendingMatch> _pendingMatches = new();
	private readonly ConcurrentDictionary<MatchId, Match> _matches = new();
	private readonly ConcurrentDictionary<string, MatchId> _connectionToMatch = new();

	// Matchmaking queue: at most one waiting entry per connection. Guarded by _matchmakingLock since
	// pairing (peek-then-remove) must be atomic across concurrent JoinQueue calls.
	private readonly Dictionary<string, DecklistRequest> _waiting = new();
	private readonly object _matchmakingLock = new();

	public MatchRegistry(CardDatabase cardDb)
	{
		_cardDb = cardDb;
	}

	public MatchId CreateMatch(string hostConnectionId, DecklistRequest hostDeck)
	{
		var id = MatchId.New();
		_pendingMatches[id] = new PendingMatch(hostConnectionId, hostDeck);
		_connectionToMatch[hostConnectionId] = id;
		return id;
	}

	public bool TryJoinMatch(MatchId matchId, string joinerConnectionId, DecklistRequest joinerDeck, out Match? match, out string? error)
	{
		if (!_pendingMatches.TryRemove(matchId, out var pending))
		{
			match = null;
			error = "No open match with that id.";
			return false;
		}

		return TryCreateMatchFromDecks(matchId, pending.HostConnectionId, pending.HostDeck, joinerConnectionId, joinerDeck, out match, out error);
	}

	// Pairs this connection with whoever is already waiting and returns the created match, or - if no
	// one is waiting - enqueues it and returns null. The caller must notify both sides on a pairing,
	// since the connection that was already waiting learns about it out-of-band (it isn't the one
	// making this call).
	public Match? TryMatchmake(string connectionId, DecklistRequest deck)
	{
		lock (_matchmakingLock)
		{
			var opponent = _waiting.Keys.FirstOrDefault();
			if (opponent == null)
			{
				_waiting[connectionId] = deck;
				return null;
			}

			var opponentDeck = _waiting[opponent];
			_waiting.Remove(opponent);
			return TryCreateMatchFromDecks(MatchId.New(), opponent, opponentDeck, connectionId, deck, out var newMatch, out _) ? newMatch : null;
		}
	}

	public void LeaveQueue(string connectionId)
	{
		lock (_matchmakingLock)
		{
			_waiting.Remove(connectionId);
		}
	}

	public bool TryGet(MatchId matchId, out Match? match) => _matches.TryGetValue(matchId, out match);

	public bool TryGetByConnection(string connectionId, out Match? match)
	{
		if (_connectionToMatch.TryGetValue(connectionId, out var matchId))
		{
			return _matches.TryGetValue(matchId, out match);
		}

		match = null;
		return false;
	}

	public void RemoveConnection(string connectionId)
	{
		LeaveQueue(connectionId);

		if (_connectionToMatch.TryRemove(connectionId, out var matchId))
		{
			_pendingMatches.TryRemove(matchId, out _);
		}
	}

	private bool TryMergeCustomDefinitions(
		DecklistRequest deck1,
		DecklistRequest deck2,
		out Dictionary<string, MinionCardDefinition> minions,
		out Dictionary<string, SpellCardDefinition> spells,
		out Dictionary<string, WeaponCardDefinition> weapons,
		out string? error)
	{
		minions = new Dictionary<string, MinionCardDefinition>();
		spells = new Dictionary<string, SpellCardDefinition>();
		weapons = new Dictionary<string, WeaponCardDefinition>();
		error = null;

		if (!TryMergeOne(deck1, "1", minions, spells, weapons, out error)) return false;
		if (!TryMergeOne(deck2, "2", minions, spells, weapons, out error)) return false;

		return true;
	}

	private bool TryMergeOne(
		DecklistRequest deck,
		string playerLabel,
		Dictionary<string, MinionCardDefinition> minions,
		Dictionary<string, SpellCardDefinition> spells,
		Dictionary<string, WeaponCardDefinition> weapons,
		out string? error)
	{
		foreach (var json in deck.CustomMinions)
		{
			if (!TryParseCustom<MinionCardDefinition>(json, "minion", playerLabel, minions, out error)) return false;
		}

		foreach (var json in deck.CustomSpells)
		{
			if (!TryParseCustom<SpellCardDefinition>(json, "spell", playerLabel, spells, out error)) return false;
		}

		foreach (var json in deck.CustomWeapons)
		{
			if (!TryParseCustom<WeaponCardDefinition>(json, "weapon", playerLabel, weapons, out error)) return false;
		}

		error = null;
		return true;
	}

	private bool TryParseCustom<TDef>(
		string json,
		string kind,
		string playerLabel,
		Dictionary<string, TDef> target,
		out string? error) where TDef : CardDefinition
	{
		CardDefinition? parsed;
		try
		{
			parsed = CardDatabase.LoadCardFromJson(json);
		}
		catch (Exception ex)
		{
			error = $"Player {playerLabel} sent an invalid custom {kind} definition: {ex.Message}";
			return false;
		}

		if (parsed is not TDef def || string.IsNullOrEmpty(def.Id))
		{
			error = $"Player {playerLabel} sent an invalid custom {kind} definition: could not parse as a {kind} with a non-empty Id.";
			return false;
		}

		if (target.ContainsKey(def.Id))
		{
			error = $"Both players submitted a custom card with id '{def.Id}' - custom card ids must be unique within a match.";
			return false;
		}

		target[def.Id] = def;
		error = null;
		return true;
	}

	private bool TryCreateMatchFromDecks(MatchId id, string connection1, DecklistRequest deck1, string connection2, DecklistRequest deck2, out Match? match, out string? error)
	{
		if (!TryMergeCustomDefinitions(deck1, deck2, out var customMinions, out var customSpells, out var customWeapons, out error))
		{
			match = null;
			return false;
		}

		var player1 = new Player(deck1.PlayerName);
		var player2 = new Player(deck2.PlayerName);

		var rngSeed = (ulong)Random.Shared.NextInt64();

		try
		{
			var gameState = MatchFactory.CreateMatch(
				_cardDb,
				player1,
				deck1.Minions.Select(c => (c.CardId, c.Count)),
				deck1.Spells.Select(c => (c.CardId, c.Count)),
				deck1.Weapons.Select(c => (c.CardId, c.Count)),
				player2,
				deck2.Minions.Select(c => (c.CardId, c.Count)),
				deck2.Spells.Select(c => (c.CardId, c.Count)),
				deck2.Weapons.Select(c => (c.CardId, c.Count)),
				rngSeed,
				customMinions,
				customSpells,
				customWeapons);

			match = new Match(id, gameState, new GameEngine())
			{
				ConnectionIdPlayer1 = connection1,
				ConnectionIdPlayer2 = connection2,
			};

			_matches[id] = match;
			_connectionToMatch[connection1] = id;
			_connectionToMatch[connection2] = id;

			error = null;
			return true;
		}
		catch (Exception ex)
		{
			match = null;
			error = $"Failed to build match from decklists: {ex.Message}";
			return false;
		}
	}

	private record PendingMatch(string HostConnectionId, DecklistRequest HostDeck);
}
