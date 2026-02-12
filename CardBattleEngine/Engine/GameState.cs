namespace CardBattleEngine;

public class GameState
{
	public Player CurrentPlayer { get; set; }
	public int MaxBoardSize { get; set; } = 7;
	public Player[] Players { get; internal set; }
	public Player? Winner { get; set; }

	//TODO replace with a queue or pending choices
	//allow for multiple choices to be queued and resolved one at a time
	//the engine loop should halt if there are and pending choices
	public IPendingChoice PendingChoice { get; set; } 

	public int maxTurns = 50;
	public int turn = 0;

	public List<HistoryEntry> History { get; set; } = new();
	private readonly IRNG rNG;

	public IReadOnlyList<Card> CardDB { get; private set; }
	public bool SkipShuffle; // must be called before start game
	public bool SkipMulligan; // must be called before start game
	public int InitialCards = 3; // must be called before start game

	public GameState(Player p1, Player p2, IRNG rng, IEnumerable<Card> cardDB)
	{
		Players = [p1, p2];
		rNG = rng;

		CurrentPlayer = p1;

		CardDB = cardDB
			.Where(c => c != null && !string.IsNullOrEmpty(c.Name))
			.GroupBy(c => c.Name)
			.Select(g => g.First())
			.ToList()
			.AsReadOnly();
	}

	private Dictionary<Guid, IGameEntity> _entityMap;

	public void RebuildEntityMap()
	{
		_entityMap = new Dictionary<Guid, IGameEntity>();
		foreach (var player in Players)
		{
			_entityMap[player.Id] = player;
			foreach (var minion in player.Board) _entityMap[minion.Id] = minion;
			foreach (var card in player.Hand) _entityMap[card.Id] = card;
		}
	}

	public Player OpponentOf(Player player)
	{
		return player == Players[0] ? Players[1] : Players[0];
	}

	public List<(IGameAction, ActionContext)> GetValidActions(Player player)
	{
		var actions = new List<(IGameAction, ActionContext)>();

		if (PendingChoice != null)
		{
			actions.AddRange(PendingChoice.GetActions(this));
			return actions;
		}

		// Playable cards
		foreach (var card in player.Hand)
		{
			var playCardAction = new PlayCardAction() { Card = card };

			ActionContext actionContext = new()
			{
				SourcePlayer = player,
				Source = player,
				SourceCard = card,
			};

			if (playCardAction.CanCast(this,
					actionContext,
					out _))
			{
				if (playCardAction.Card is WeaponCard weaponCard)
				{
					actionContext = new CardBattleEngine.ActionContext()
					{
						SourcePlayer = actionContext.SourcePlayer,
						Source = player,
						Target = actionContext.SourcePlayer
					};
					actions.Add((playCardAction, actionContext));
					continue;
				}

				var validTargets = playCardAction.Card.ValidTargetSelector?.Select(this, player, playCardAction.Card);
				if (validTargets != null)
				{
					foreach(var target in validTargets)
					{
						ActionContext actionContextTarget = new()
						{
							SourcePlayer = player,
							SourceCard = card,
							Target = target,
						};
						actions.Add((playCardAction, actionContextTarget));
					}
				}
				else
				{
					ActionContext actionContextNoTarget = new()
					{
						SourcePlayer = player,
						SourceCard = card,
					};
					actions.Add((playCardAction, actionContextNoTarget));
				}
			}
		}

		List<IGameEntity> attackers = [player, ..player.Board];

		// Attacks (creatures that can attack)
		foreach (var attacker in attackers)
		{
			if (!attacker.CanAttack())
			{
				continue;
			}

			var attackHeroAction = new AttackAction();
			ActionContext attackHeroActionContext = new()
			{
				Source = attacker,
				SourcePlayer = player,
				Target = OpponentOf(player)
			};
			if (attackHeroAction.IsValid(this, attackHeroActionContext, out string _))
			{
				actions.Add((attackHeroAction, attackHeroActionContext));
			}

			foreach (var defender in OpponentOf(player).Board)
			{
				var attackAction = new AttackAction();
				ActionContext attackActionContext = new()
				{
					Source = attacker,
					SourcePlayer = player,
					Target = defender
				};
				if (attackAction.IsValid(
					this,
					attackActionContext,
					out string _))
				{
					actions.Add((attackAction, attackActionContext));
				}
			}
		}

		// Always can end turn
		actions.Add((
			new EndTurnAction(),
			new ()
			{
				Source = player,
				SourcePlayer = player,
			}));

		return actions;
	}

	public bool IsGameOver()
	{
		foreach (var player in Players)
			if (player.Health <= 0)
			{
				Winner = OpponentOf(player);
				return true;
			}

		if (turn > maxTurns)
		{
			Winner = null; // draw
			return true;
		}

		return false;
	}

	public int GetBoardStrength(Player currentPlayer)
	{
		return currentPlayer.Board.Select(x => x.Health + x.Attack).Sum();
	}

	public GameState Clone()
	{
		// Clone both players first
		var p1 = Players[0].Clone();
		var p2 = Players[1].Clone();

		// Create a new game state using the cloned players
		var clone = new GameState(p1, p2, this.rNG?.Clone(), this.CardDB)
		{
			MaxBoardSize = this.MaxBoardSize,
			maxTurns = this.maxTurns,
			turn = this.turn,
			Winner = this.Winner == Players[0] ? p1 :
					 this.Winner == Players[1] ? p2 : null
		};

		clone.CurrentPlayer = this.CurrentPlayer == Players[0] ? p1 : p2;
		clone.PendingChoice = this.PendingChoice;

		return clone;
	}

	public GameState LightClone()
	{
		// Clone both players first
		var p1 = Players[0].LightClone();
		var p2 = Players[1].LightClone();

		// Create a new game state using the cloned players
		var clone = new GameState(p1, p2, this.rNG?.Clone(), [])
		{
			MaxBoardSize = this.MaxBoardSize,
			maxTurns = this.maxTurns,
			turn = this.turn,
			Winner = this.Winner == Players[0] ? p1 :
					 this.Winner == Players[1] ? p2 : null
		};

		clone.CurrentPlayer = this.CurrentPlayer == Players[0] ? p1 : p2;
		clone.PendingChoice = this.PendingChoice;

		RebuildEntityMap();

		return clone;
	}

	public IEnumerable<IGameEntity> GetAllEntities()
	{
		foreach (var player in Players)
		{
			yield return player;

			foreach (var minion in player.Board)
			{
				yield return minion;
			}

			foreach (var card in player.Hand)
			{
				yield return card;
			}
		}
	}

	public IGameEntity GetEntityById(Guid id)
	{
		if (_entityMap != null &&
			_entityMap.TryGetValue(id, out var entity))
		{
			return entity;
		}
		return GetAllEntities().FirstOrDefault(x => x.Id == id);
	}

	public IEnumerable<ITriggerSource> GetAllTriggerSources()
	{
		foreach (var player in Players)
		{
			yield return player;

			foreach (var secret in player.Secrets)
			{
				yield return secret;
			}

			foreach (var minion in player.Board)
			{
				yield return minion;
			}

			foreach (var card in player.Hand)
			{
				yield return card;
			}
		}
	}

	public IEnumerable<Minion> GetAllMinions()
	{
		var all = new List<Minion>();

		foreach (var player in Players)
		{
			all.AddRange(player.Board);
		}

		return all;
	}

	public T ChooseRandom<T>(IReadOnlyList<T> options)
	{
		if (options.Count == 0) return default!;
		return options[rNG.NextInt(0, options.Count)];
	}

	public IEnumerable<T> ChooseRandom<T>(IReadOnlyList<T> options, int count)
	{
		if (options.Count == 0 || count <= 0)
			return Array.Empty<T>();

		// Work on a copy
		var copy = options.ToList();

		// Shuffle the copy
		Shuffle(copy);

		// Return the first `count` items from the shuffled copy
		return copy.Take(count);
	}

	public void Shuffle<T>(IList<T> list)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rNG.NextInt(0, i + 1); // same RNG as ChooseRandom
			(list[i], list[j]) = (list[j], list[i]);
		}
	}
}
