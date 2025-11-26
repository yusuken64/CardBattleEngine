namespace CardBattleEngine;

public class GameState
{
	public Player CurrentPlayer { get; set; }
	public int MaxBoardSize { get; set; } = 7;
	public Player[] Players { get; internal set; }
	public Player? Winner { get; set; }
	public IPendingChoice PendingChoice { get; set; }

	public int maxTurns = 50;
	public int turn = 0;

	public List<HistoryEntry> History { get; set; } = new();

	public GameState(Player p1, Player p2)
	{
		Players = [p1, p2];

		CurrentPlayer = p1;
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

				if (playCardAction.IsValid(this, actionContext))
				{
					actions.Add((playCardAction, actionContext));
				}
			}
		}

		// Attacks (creatures that can attack)
		foreach (var attacker in player.Board)
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
			if (attackHeroAction.IsValid(this, attackHeroActionContext))
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
					attackActionContext
					))
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

				if (playCardAction.IsValid(this, actionContext))
				{
					actions.Add((playCardAction, null));
				}
			}
		}

		// Attacks (creatures that can attack)
		var attackers = new List<IGameEntity>();
		attackers.AddRange([player as IGameEntity]);
		attackers.AddRange(player.Board.Cast<IGameEntity>());

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
		var clone = new GameState(p1, p2)
		{
			MaxBoardSize = this.MaxBoardSize,
			maxTurns = this.maxTurns,
			turn = this.turn,
			Winner = this.Winner == Players[0] ? p1 :
					 this.Winner == Players[1] ? p2 : null
		};

		clone.CurrentPlayer = this.CurrentPlayer == Players[0] ? p1 : p2;

		return clone;
	}

	public IEnumerable<IGameEntity> GetAllEntities()
	{
		var all = new List<IGameEntity>();

		foreach (var player in Players)
		{
			all.Add(player);
			all.AddRange(player.Board);
			//all.AddRange(CurrentPlayer.Hand);
		}

		return all;
	}

	public IEnumerable<ITriggerSource> GetAllTriggerSources()
	{
		var all = new List<ITriggerSource>();

		foreach (var player in Players)
		{
			all.Add(player);
			all.AddRange(player.Secrets);
			all.AddRange(player.Board);
			//all.AddRange(CurrentPlayer.Hand);
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

	public List<ITriggerSource> GetValidTargets(ITriggerSource entity, TargetingType type)
	{
		var targets = new List<ITriggerSource>();
		var opponent = OpponentOf(entity.Entity.Owner);

		switch (type)
		{
			case TargetingType.EnemyHero:
				targets.Add(opponent);
				break;

			case TargetingType.EnemyMinion:
				targets.AddRange(opponent.Board.Where(m => m.IsAlive));
				break;

			case TargetingType.AnyEnemy:
				targets.Add(opponent);
				targets.AddRange(opponent.Board.Where(m => m.IsAlive));
				break;

			case TargetingType.FriendlyHero:
				targets.Add(entity);
				break;

			case TargetingType.FriendlyMinion:
				targets.AddRange(entity.Entity.Owner.Board.Where(m => m.IsAlive));
				break;

			case TargetingType.Self:
				if (entity != null) targets.Add(entity);
				break;

			case TargetingType.Any:
				targets.Add(entity);
				targets.AddRange(entity.Entity.Owner.Board.Where(m => m.IsAlive));
				targets.Add(opponent);
				targets.AddRange(opponent.Board.Where(m => m.IsAlive));
				break;
		}

		return targets;
	}
}
