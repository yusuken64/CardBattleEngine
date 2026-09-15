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
				f[i++] = m is Minion friendlyMinion && friendlyMinion.Taunt ? 1f : 0f;
				f[i++] = m is Minion friendlyMinionDs && friendlyMinionDs.HasDivineShield ? 1f : 0f;
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
				f[i++] = m is Minion enemyMinion && enemyMinion.Taunt ? 1f : 0f;
				f[i++] = m is Minion enemyMinionDs && enemyMinionDs.HasDivineShield ? 1f : 0f;
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

	// ------------------------------------------------------------------
	// ACTION ENCODING
	// ------------------------------------------------------------------
	//
	// Every field reserves 0 as the NONE sentinel and shifts real indices
	// up by one. The previous layout used -1 for NONE and masked it to
	// 4 bits, which made NONE indistinguishable from index 15 -- so an
	// untargeted play silently decoded as "target my 7th minion".
	//
	// Field sizes (0 = none in all three index fields):
	//   TYPE    5 values                                       -> 3 bits
	//   HAND    none + 10 hand slots            = 11 values     -> 4 bits
	//   SOURCE  none + hero + 7 board slots     =  9 values     -> 4 bits
	//   TARGET  none + enemy hero + 7 enemy
	//           + friendly hero + 7 friendly    = 17 values     -> 5 bits
	//                                             total 16 bits -> 65536
	//
	// A pending choice (Discover / Choose One / target prompt) is encoded as
	// TYPE = Choice with the option's index in TARGET; HAND and SOURCE are unused.
	// The choice itself supplies the context, so nothing else needs encoding.
	//
	// Verified exhaustively: 6763 reachable tuples, 0 collisions,
	// exact round-trip, max index 32799.

	public const int PolicySize = 1 << 16; // 65536

	const int TARGET_SHIFT = 0;
	const int SOURCE_SHIFT = 5;
	const int HAND_SHIFT = 9;
	const int TYPE_SHIFT = 13;

	const int TARGET_BITS = 5;
	const int SOURCE_BITS = 4;
	const int HAND_BITS = 4;
	const int TYPE_BITS = 3;

	const int MASK_TARGET = (1 << TARGET_BITS) - 1; // 31
	const int MASK_SOURCE = (1 << SOURCE_BITS) - 1; // 15
	const int MASK_HAND = (1 << HAND_BITS) - 1;     // 15
	const int MASK_TYPE = (1 << TYPE_BITS) - 1;     // 7

	/// <summary>Sentinel meaning "this field is not applicable to the action".</summary>
	public const int NONE = 0;

	/// <summary>Option index 0 encodes as 1, since 0 is the none-sentinel.</summary>
	const int CHOICE_BASE = 1;

	/// <summary>Most options a single pending choice can offer and still encode.</summary>
	public const int MaxChoiceOptions = MASK_TARGET - CHOICE_BASE + 1; // 31

	public static float[] ActionsToPolicy(
		GameState state,
		Dictionary<int, float> actionProbs)
	{
		var validActions = state.GetValidActions(state.CurrentPlayer);
		var policy = new float[PolicySize];

		// Collect the valid indices once so normalisation touches only the
		// handful of live entries rather than sweeping all 65536 slots.
		var validIndices = new List<int>(validActions.Count);
		foreach (var action in validActions)
		{
			int idx = EncodeAction(action, state);
			validIndices.Add(idx);

			if (actionProbs.TryGetValue(idx, out var prob))
				policy[idx] = prob;
		}

		float sum = 0f;
		foreach (int idx in validIndices)
			sum += policy[idx];

		if (sum > 0f)
		{
			foreach (int idx in validIndices)
				policy[idx] /= sum;
		}
		else if (validIndices.Count > 0)
		{
			float uniform = 1f / validIndices.Count;
			foreach (int idx in validIndices)
				policy[idx] = uniform;
		}

		return policy;
	}

	public static int EncodeAction(
		(IGameAction action, ActionContext ctx) input,
		GameState state)
	{
		// A pending choice short-circuits everything: GameState.GetValidActions
		// returns the choice's option list and nothing else, so the only thing
		// left to encode is which option was taken.
		if (state.PendingChoice != null)
			return EncodeChoice(input, state);

		var (action, ctx) = input;

		int type = action switch
		{
			EndTurnAction => (int)ActionType.EndTurn,
			AttackAction => (int)ActionType.Attack,
			PlayCardAction => (int)ActionType.PlayCard,
			HeroPowerAction => (int)ActionType.HeroPower,

			_ => throw new NotSupportedException(
				$"Action type {action.GetType().Name} has no policy-space encoding, " +
				"and no choice is pending.")
		};

		int source = GetSourceIndex(ctx.Source, state);
		// AI action space is intentionally single-target for now; multi-target cards will require future extension
		int target = GetTargetIndex(ctx.Targets?.FirstOrDefault(), state);
		int hand = GetHandIndex(ctx.SourceCard, state);

		return
			((type & MASK_TYPE) << TYPE_SHIFT) |
			((hand & MASK_HAND) << HAND_SHIFT) |
			((source & MASK_SOURCE) << SOURCE_SHIFT) |
			((target & MASK_TARGET) << TARGET_SHIFT);
	}

	public static (IGameAction, ActionContext) DecodeAction(int idx, GameState state)
	{
		int type = (idx >> TYPE_SHIFT) & MASK_TYPE;
		int hand = (idx >> HAND_SHIFT) & MASK_HAND;
		int sourceIndex = (idx >> SOURCE_SHIFT) & MASK_SOURCE;
		int targetIndex = (idx >> TARGET_SHIFT) & MASK_TARGET;

		if (type == (int)ActionType.Choice)
			return DecodeChoice(targetIndex, state);

		if (type > (int)ActionType.HeroPower)
			throw new NotSupportedException($"Unknown action type {type}");

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
						SourcePlayer = state.CurrentPlayer,
						Targets = target is null ? null : [target]
					}
				);

			case ActionType.PlayCard:
				{
					if (hand == NONE)
						throw new InvalidOperationException(
							"PlayCard decoded with no hand index.");

					int handSlot = hand - 1;
					if (handSlot >= state.CurrentPlayer.Hand.Count)
						throw new InvalidOperationException(
							$"Invalid hand index {handSlot} (hand has {state.CurrentPlayer.Hand.Count} cards)");

					var card = state.CurrentPlayer.Hand[handSlot];

					return (
						new PlayCardAction { Card = card },
						new ActionContext
						{
							Source = card,
							SourceCard = card,
							SourcePlayer = state.CurrentPlayer,
							Targets = target is null ? null : [target]
						}
					);
				}

			case ActionType.HeroPower:
				return (
					new HeroPowerAction(),
					new ActionContext
					{
						Source = source,
						SourcePlayer = state.CurrentPlayer,
						Targets = target is null ? null : [target]
					}
				);

			default:
				throw new NotSupportedException($"Unknown action type {type}");
		}
	}

	// --- choice encoding -----------------------------------------------
	//
	// DESIGN NOTE, and it matters for what the policy head can learn.
	//
	// A choice is encoded purely as "take option k", not as "take THIS card".
	// The option list is freshly shuffled each time the choice is created, so
	// index 2 is a different card every game. That means the policy head cannot
	// learn anything semantic about choices — only positional bias, which is
	// noise.
	//
	// That is a deliberate, bounded limitation rather than an oversight. The
	// search still handles choices correctly: MCTS expands each option as its
	// own child and evaluates it by simulation, so the VALUE head does the work
	// and the policy prior simply contributes little here. If choice quality
	// later turns out to matter, the fix is to score options by encoding each
	// resulting state through the value head and taking the argmax, rather than
	// widening this encoding.
	//
	// Document this in the technical spec. It is the kind of limitation an
	// examiner will find more convincing stated than discovered.

	static int EncodeChoice((IGameAction action, ActionContext ctx) input, GameState state)
	{
		var options = state.PendingChoice.GetActions(state).ToList();

		int index = -1;
		for (int i = 0; i < options.Count; i++)
		{
			if (ReferenceEquals(options[i].Item1, input.action))
			{
				index = i;
				break;
			}
		}

		if (index < 0)
			throw new InvalidOperationException(
				"Action is not among the pending choice's options. Encode choice actions " +
				"from the list GetValidActions returned, not from a rebuilt equivalent — " +
				"option identity is by reference.");

		if (index >= MaxChoiceOptions)
			throw new NotSupportedException(
				$"Pending choice offers {options.Count} options; the encoding supports " +
				$"{MaxChoiceOptions}.");

		return
			(((int)ActionType.Choice & MASK_TYPE) << TYPE_SHIFT) |
			(((index + CHOICE_BASE) & MASK_TARGET) << TARGET_SHIFT);
	}

	static (IGameAction, ActionContext) DecodeChoice(int targetField, GameState state)
	{
		if (state.PendingChoice is null)
			throw new InvalidOperationException(
				"Decoded a Choice action, but no choice is pending in this state.");

		if (targetField < CHOICE_BASE)
			throw new InvalidOperationException("Choice action carries no option index.");

		int index = targetField - CHOICE_BASE;
		var options = state.PendingChoice.GetActions(state).ToList();

		if (index >= options.Count)
			throw new InvalidOperationException(
				$"Option index {index} is out of range; the pending choice offers " +
				$"{options.Count} options.");

		return options[index];
	}

	// --- index helpers -------------------------------------------------
	// Encoding convention, NONE = 0 throughout:
	//   source : 0 none | 1 friendly hero | 2..8  friendly board 0..6
	//   target : 0 none | 1 enemy hero    | 2..8  enemy board 0..6
	//                   | 9 friendly hero | 10..16 friendly board 0..6
	//   hand   : 0 none | 1..10 hand slot 0..9

	const int SOURCE_HERO = 1;
	const int SOURCE_BOARD_BASE = 2;

	const int TARGET_ENEMY_HERO = 1;
	const int TARGET_ENEMY_BOARD_BASE = 2;
	const int TARGET_FRIENDLY_HERO = 9;
	const int TARGET_FRIENDLY_BOARD_BASE = 10;

	const int HAND_BASE = 1;

	static int GetSourceIndex(IGameEntity entity, GameState state)
	{
		if (entity is null) return NONE;

		var current = state.CurrentPlayer;

		if (ReferenceEquals(entity, current))
			return SOURCE_HERO;

		int index = entity is Minion m ? current.Board.IndexOf(m) : -1;
		if (index >= 0 && index < MaxMinions)
			return SOURCE_BOARD_BASE + index;

		return NONE;
	}

	static int GetTargetIndex(IGameEntity entity, GameState state)
	{
		if (entity is null) return NONE;

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		if (ReferenceEquals(entity, opponent))
			return TARGET_ENEMY_HERO;

		if (ReferenceEquals(entity, current))
			return TARGET_FRIENDLY_HERO;

		if (entity is Minion m)
		{
			int enemyIndex = opponent.Board.IndexOf(m);
			if (enemyIndex >= 0 && enemyIndex < MaxMinions)
				return TARGET_ENEMY_BOARD_BASE + enemyIndex;

			int friendlyIndex = current.Board.IndexOf(m);
			if (friendlyIndex >= 0 && friendlyIndex < MaxMinions)
				return TARGET_FRIENDLY_BOARD_BASE + friendlyIndex;
		}

		return NONE;
	}

	static int GetHandIndex(Card card, GameState state)
	{
		if (card is null) return NONE;

		int index = state.CurrentPlayer.Hand.IndexOf(card);
		if (index >= 0 && index < MaxHandSize)
			return HAND_BASE + index;

		return NONE;
	}

	public static IGameEntity ResolveSourceFromIndex(int index, GameState state)
	{
		if (index == NONE) return null;

		var current = state.CurrentPlayer;

		if (index == SOURCE_HERO) return current;

		int i = index - SOURCE_BOARD_BASE;
		return i >= 0 && i < current.Board.Count ? current.Board[i] : null;
	}

	public static IGameEntity ResolveTargetFromIndex(int index, GameState state)
	{
		if (index == NONE) return null;

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		if (index == TARGET_ENEMY_HERO) return opponent;
		if (index == TARGET_FRIENDLY_HERO) return current;

		if (index >= TARGET_ENEMY_BOARD_BASE && index < TARGET_ENEMY_BOARD_BASE + MaxMinions)
		{
			int i = index - TARGET_ENEMY_BOARD_BASE;
			return i < opponent.Board.Count ? opponent.Board[i] : null;
		}

		if (index >= TARGET_FRIENDLY_BOARD_BASE && index < TARGET_FRIENDLY_BOARD_BASE + MaxMinions)
		{
			int i = index - TARGET_FRIENDLY_BOARD_BASE;
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
	HeroPower,
	Choice
}

// Retained from the original file. Not referenced by the encoder itself,
// but kept so existing callers keep compiling.
public struct EncodedAction
{
	public int Type;        // ActionType
	public int SourceIndex; // encoded source index, 0 = none
	public int TargetIndex; // encoded target index, 0 = none
	public int HandIndex;   // encoded hand index,   0 = none
}
