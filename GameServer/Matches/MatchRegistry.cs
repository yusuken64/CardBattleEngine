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
		match = null;

		if (!_pendingMatches.TryRemove(matchId, out var pending))
		{
			error = "No open match with that id.";
			return false;
		}

		var player1 = new Player(pending.HostDeck.PlayerName);
		var player2 = new Player(joinerDeck.PlayerName);

		var rngSeed = (ulong)Random.Shared.NextInt64();

		var gameState = MatchFactory.CreateMatch(
			_cardDb,
			player1,
			pending.HostDeck.Minions.Select(c => (c.CardId, c.Count)),
			pending.HostDeck.Spells.Select(c => (c.CardId, c.Count)),
			player2,
			joinerDeck.Minions.Select(c => (c.CardId, c.Count)),
			joinerDeck.Spells.Select(c => (c.CardId, c.Count)),
			rngSeed);

		var newMatch = new Match(matchId, gameState, new GameEngine())
		{
			ConnectionIdPlayer1 = pending.HostConnectionId,
			ConnectionIdPlayer2 = joinerConnectionId,
		};

		_matches[matchId] = newMatch;
		_connectionToMatch[joinerConnectionId] = matchId;

		match = newMatch;
		error = null;
		return true;
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
		if (_connectionToMatch.TryRemove(connectionId, out var matchId))
		{
			_pendingMatches.TryRemove(matchId, out _);
		}
	}

	private record PendingMatch(string HostConnectionId, DecklistRequest HostDeck);
}
