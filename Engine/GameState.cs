namespace CardBattleEngine;

public class GameState
{
	public Player CurrentPlayer { get; set; }
	public Player OpponentPlayer { get; set; }
	public int MaxBoardSize { get; set; } = 7;
	public Player[] Players { get; internal set; }
	public Player? Winner { get; set; }

	private Dictionary<Guid, int> _fatigueCounters = new Dictionary<Guid, int>();

	public int maxTurns = 50;
	public int turn = 0;

	public GameState(Player p1, Player p2)
	{
		Players = [p1, p2];

		CurrentPlayer = p1;
		OpponentPlayer = p2;
	}

	internal Player OpponentOf(Player player)
	{
		return player == Players[0] ? Players[1] : Players[0];
	}

	public List<IGameAction> GetValidActions(Player player)
	{
		var actions = new List<IGameAction>();

		// Playable cards
		foreach (var card in player.Hand)
		{
			if (card.ManaCost <= player.Mana)
			{
				var playCardAction = new PlayCardAction(card);
				if (playCardAction.IsValid(this))
				{
					actions.Add(playCardAction);
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

			var attackHeroAction = new AttackAction(attacker, OpponentOf(player));
			actions.Add(attackHeroAction);

			foreach (var defender in OpponentOf(player).Board)
			{
				var attackAction = new AttackAction(attacker, defender);
				if (attackAction.IsValid(this))
				{
					actions.Add(attackAction);
				}
			}
		}

		// Always can end turn
		actions.Add(new EndTurnAction());

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
}
