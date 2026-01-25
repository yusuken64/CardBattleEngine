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
			if (card.ManaCost <= player.Mana)
			{
				var playCardAction = new PlayCardAction() { Card = card };

				ActionContext actionContext = new()
				{
					SourcePlayer = player,
					SourceCard = card,
				};

				if (playCardAction.IsValid(this, actionContext, out string _))
				{
					actions.Add((playCardAction, actionContext));
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

	public List<(IGameAction, ActionContext)> GetUntargetedActions(Player player)
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
			if (card.ManaCost <= player.Mana)
			{
				var playCardAction = new PlayCardAction() { Card = card };

				ActionContext actionContext = new()
				{
					SourcePlayer = player,
					SourceCard = card,
				};

				if (playCardAction.IsValid(this, actionContext, out string _))
				{
					actions.Add((playCardAction, null));
				}
			}
		}

		List<IGameEntity> attackers = [player, .. player.Board];

		foreach (var attacker in attackers)
		{
			if (!attacker.CanAttack())
			{
				continue;
			}

			var attackHeroAction = new AttackAction();
			actions.Add((attackHeroAction, new ActionContext()
			{
				Source = attacker
			}));
		}

		// Always can end turn
		actions.Add((
			new EndTurnAction(),
			new()
			{
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
		var clone = new GameState(p1, p2, this.rNG.Clone(), this.CardDB)
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

	public IEnumerable<IGameEntity> GetAllEntities()
	{
		var all = new List<IGameEntity>();

		foreach (var player in Players)
		{
			all.Add(player);
			all.AddRange(player.Board);
			all.AddRange(player.Hand);
		}

		return all;
	}

	public IGameEntity GetEntityById(Guid id)
	{
		return GetAllEntities().FirstOrDefault(x => x.Id == id);
	}

	public IEnumerable<ITriggerSource> GetAllTriggerSources()
	{
		var all = new List<ITriggerSource>();

		foreach (var player in Players)
		{
			all.Add(player);
			all.AddRange(player.Secrets);
			all.AddRange(player.Board);
			all.AddRange(player.Hand);
		}

		return all;
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
