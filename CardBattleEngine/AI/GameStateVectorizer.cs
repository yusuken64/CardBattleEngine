namespace CardBattleEngine.AI;

public static class GameStateVectorizer
{
	public const int MaxMinions = 7;
	public const int MaxHandSize = 10;

	public static float[] ToVector(this GameState state)
	{
		var f = new float[256];
		int i = 0;

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		// --- GLOBAL ---
		f[i++] = current.Health / 30f;
		f[i++] = opponent.Health / 30f;
		f[i++] = current.Mana / 10f;
		f[i++] = current.MaxMana / 10f;

		// --- FRIENDLY BOARD ---
		for (int slot = 0; slot < MaxMinions; slot++)
		{
			if (slot < current.Board.Count)
			{
				var m = current.Board[slot];

				f[i++] = 1f;
				f[i++] = m.Attack / 10f;
				f[i++] = m.Health / 10f;
				f[i++] = m.CanAttack() ? 1f : 0f;
				f[i++] = m.Taunt ? 1f : 0f;
				f[i++] = m.HasDivineShield ? 1f : 0f;
			}
			else
			{
				i += 6; // empty slot
			}
		}

		// --- ENEMY BOARD ---
		for (int slot = 0; slot < MaxMinions; slot++)
		{
			if (slot < opponent.Board.Count)
			{
				var m = opponent.Board[slot];

				f[i++] = 1f;
				f[i++] = m.Attack / 10f;
				f[i++] = m.Health / 10f;
				f[i++] = m.CanAttack() ? 1f : 0f;
				f[i++] = m.Taunt ? 1f : 0f;
				f[i++] = m.HasDivineShield ? 1f : 0f;
			}
			else
			{
				i += 6;
			}
		}

		// --- HAND ---
		for (int slot = 0; slot < MaxHandSize; slot++)
		{
			if (slot < current.Hand.Count)
			{
				var c = current.Hand[slot];

				f[i++] = 1f;
				f[i++] = c.ManaCost / 10f;

				if (c is MinionCard mc)
				{
					f[i++] = mc.Attack / 10f;
					f[i++] = mc.Health / 10f;
				}
				else
				{
					f[i++] = 0f;
					f[i++] = 0f;
				}
			}
			else
			{
				i += 4;
			}
		}

		return f;
	}

	public static float[] ActionsToPolicy(
		GameState state,
		Dictionary<int, float> actionProbs)
	{
		var validActions = state.GetValidActions(state.CurrentPlayer);

		const int PolicySize = 1 << 16; // 65536
		var policy = new float[PolicySize];

		foreach (var action in validActions)
		{
			int idx = EncodeAction(action, state);

			if (actionProbs.TryGetValue(idx, out var prob))
				policy[idx] = prob;
		}

		float sum = policy.Sum();
		if (sum > 0f)
			for (int i = 0; i < policy.Length; i++)
				policy[i] /= sum;
		else
		{
			float uniform = 1f / validActions.Count;

			foreach (var action in validActions)
			{
				int idx = EncodeAction(action, state);
				if (idx >= 0 && idx < policy.Length)
					policy[idx] = uniform;
			}
		}
		return policy;
	}

	const int TARGET_SHIFT = 0;
	const int SOURCE_SHIFT = 4;
	const int HAND_SHIFT = 8;
	const int TYPE_SHIFT = 12;

	const int MASK_4 = 0b1111;

	public static int EncodeAction(
		(IGameAction action, ActionContext ctx) input,
		GameState state)
	{
		var (action, ctx) = input;

		int type = action switch
		{
			EndTurnAction => (int)ActionType.EndTurn,

			AttackAction => (int)ActionType.Attack,

			PlayCardAction => (int)ActionType.PlayCard,

			HeroPowerAction => (int)ActionType.HeroPower,

			_ => throw new Exception("Unknown action")
		};

		int source = GetSourceIndex(ctx.Source, state);
		int target = GetTargetIndex(ctx.Target, state);
		int hand = GetHandIndex(ctx.SourceCard, state);

		return
			((type & MASK_4) << TYPE_SHIFT) |
			((hand & MASK_4) << HAND_SHIFT) |
			((source & MASK_4) << SOURCE_SHIFT) |
			((target & MASK_4) << TARGET_SHIFT);
	}

	// 0 = hero, 1–7 = board order
	static int GetSourceIndex(IGameEntity entity, GameState state)
	{
		var current = state.CurrentPlayer;

		if (entity == current)
		{
			return 0;
		}

		int index = current.Board.IndexOf(entity as Minion);
		if (index >= 0)
		{
			return index + 1;
		}

		return -1;
	}

	// -1 = untargeted
	// 0 = enemy hero
	// 1–7 = enemy board
	// 8 = friendly hero
	// 9–15 = friendly board
	static int GetTargetIndex(IGameEntity entity, GameState state)
	{
		if (entity == null)
			return -1;

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		if (entity == opponent)
		{
			return 0;
		}

		int enemyIndex = opponent.Board.IndexOf(entity as Minion);
		if (enemyIndex >= 0)
		{
			return 1 + enemyIndex;
		}

		if (entity == current)
		{
			return 8;
		}

		int friendlyIndex = current.Board.IndexOf(entity as Minion);
		if (friendlyIndex >= 0)
		{
			return 9 + friendlyIndex;
		}

		return -1;
	}

	static int GetHandIndex(Card card, GameState state)
	{
		if (card == null)
			return -1;

		var current = state.CurrentPlayer;

		int index = current.Hand.IndexOf(card);
		if (index >= 0)
		{
			return index;
		}

		return -1;
	}

	public static (IGameAction, ActionContext) DecodeAction(int idx, GameState state)
	{
		int type = (idx >> TYPE_SHIFT) & MASK_4;
		int hand = (idx >> HAND_SHIFT) & MASK_4;
		int sourceIndex = (idx >> SOURCE_SHIFT) & MASK_4;
		int targetIndex = (idx >> TARGET_SHIFT) & MASK_4;

		var actionType = (ActionType)type;

		var source = ResolveSourceFromIndex(sourceIndex, state);
		var target = ResolveTargetFromIndex(targetIndex, state);

		switch (actionType)
		{
			case ActionType.EndTurn:
				return (
					new EndTurnAction(),
					new ActionContext
					{
						Source = state.CurrentPlayer,
						SourcePlayer = state.CurrentPlayer
					}
				);

			case ActionType.Attack:
				return (
					new AttackAction(),
					new ActionContext
					{
						Source = source,
						Target = target
					}
				);

			case ActionType.PlayCard:
				{
					if (hand >= state.CurrentPlayer.Hand.Count)
						throw new Exception($"Invalid hand index {hand}");

					var card = state.CurrentPlayer.Hand[hand];

					return (
						new PlayCardAction { Card = card },
						new ActionContext
						{
							Source = card,
							Target = target
						}
					);
				}

			case ActionType.HeroPower:
				return (
					new HeroPowerAction(),
					new ActionContext
					{
						Target = target
					}
				);

			default:
				throw new Exception($"Unknown action type {type}");
		}
	}

	public static IGameEntity ResolveSourceFromIndex(int index, GameState state)
	{
		if (index == 0)
			return state.CurrentPlayer;

		int i = index - 1;
		return i >= 0 && i < state.CurrentPlayer.Board.Count
			? state.CurrentPlayer.Board[i]
			: null;
	}

	public static IGameEntity ResolveTargetFromIndex(int index, GameState state)
	{
		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		if (index == -1) return null;

		if (index == 0)
			return opponent;

		if (index >= 1 && index <= 7)
		{
			int i = index - 1;
			return i < opponent.Board.Count ? opponent.Board[i] : null;
		}

		if (index == 8)
			return current;

		if (index >= 9 && index <= 15)
		{
			int i = index - 9;
			return i < current.Board.Count ? current.Board[i] : null;
		}

		return null;
	}
}

public enum ActionType
{
	EndTurn,
	Attack,
	PlayCard,
	HeroPower
}

public struct EncodedAction
{
	public int Type;        // ActionType
	public int SourceIndex; // attacker index or -1
	public int TargetIndex; // target index or -1
	public int HandIndex;   // for PlayCard or -1
}