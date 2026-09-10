namespace CardBattleEngine.View;

// Projects a GameState into a per-player redacted view safe to send over the network.
// Governing rule: anything already in gameState.History is public UNLESS it's still tied to a
// zone that stays hidden after resolution (a still-active Secret, or a card that landed in a
// player's own Hand/Deck rather than a public zone like the board) - see IsHiddenSecretCard/
// CardGained handling below. Anything still only inside GameState.PendingChoice.Options is shown
// only to PendingChoice.SourcePlayer (that's where Discover's real candidate cards live).
public static class PlayerViewBuilder
{
	public static PlayerGameView Build(
		GameState state,
		Player viewer,
		IReadOnlyList<HistoryEntry> newHistory = null,
		IReadOnlyList<(IGameAction Action, ActionContext Context)> canonicalLegalActions = null,
		int? promptVersion = null)
	{
		var opponent = state.OpponentOf(viewer);
		bool isGameOver = state.IsGameOver();

		var view = new PlayerGameView
		{
			ViewerPlayerId = viewer.Id,
			Turn = state.turn,
			CurrentPlayerId = state.CurrentPlayer.Id,
			IsGameOver = isGameOver,
			WinnerPlayerId = state.Winner?.Id,
			Self = BuildSelf(viewer),
			Opponent = BuildOpponent(opponent),
			OpponentIsChoosing = state.PendingChoice != null && state.PendingChoice.SourcePlayer != viewer,
			PromptVersion = promptVersion,
		};

		view.PendingChoice = BuildPendingChoice(state, viewer, canonicalLegalActions);
		view.LegalActions = BuildLegalActions(state, viewer, canonicalLegalActions);

		if (newHistory != null)
		{
			foreach (var entry in newHistory)
			{
				view.NewHistory.Add(BuildHistoryEntry(entry, viewer));
			}
		}

		return view;
	}

	private static PublicPlayerView BuildSelf(Player player)
	{
		var view = BuildBase(player);
		view.Hand = player.Hand.Select(BuildCardView).ToList();
		view.Secrets = player.Secrets.Select((secret, index) => new SecretView
		{
			Index = index,
			Name = secret.SourceCard?.Name,
			CardId = secret.SourceCard?.Id
		}).ToList();
		return view;
	}

	private static PublicPlayerView BuildOpponent(Player player)
	{
		var view = BuildBase(player);
		view.Hand = null;
		view.Secrets = null;
		return view;
	}

	private static PublicPlayerView BuildBase(Player player)
	{
		return new PublicPlayerView
		{
			PlayerId = player.Id,
			Name = player.Name,
			Health = player.Health,
			MaxHealth = player.MaxHealth,
			Armor = player.Armor,
			Mana = player.Mana,
			MaxMana = player.MaxMana,
			Attack = player.Attack,
			IsAlive = player.IsAlive,
			HasAttackedThisTurn = player.HasAttackedThisTurn,
			IsFrozen = player.IsFrozen,
			IsStealth = player.IsStealth,
			HandCount = player.Hand.Count,
			DeckCount = player.Deck.Count,
			Board = player.Board.OfType<Minion>().Select(BuildMinionView).ToList(),
			Graveyard = player.Graveyard.Select(BuildMinionView).ToList(),
			EquippedWeapon = player.EquippedWeapon == null ? null : BuildWeaponView(player.EquippedWeapon),
			HeroPower = player.HeroPower == null ? null : BuildHeroPowerView(player.HeroPower),
			SecretCount = player.Secrets.Count,
		};
	}

	private static CardView BuildCardView(Card card)
	{
		return new CardView
		{
			Id = card.Id,
			Name = card.Name,
			ManaCost = card.ManaCost,
			Type = card.Type,
			Attack = card.Type == CardType.Minion || card.Type == CardType.Weapon ? card.Attack : null,
			Health = card.Type == CardType.Minion ? card.Health : null,
			CardId = card.CardId ?? card.Name,
		};
	}

	private static MinionView BuildMinionView(Minion minion)
	{
		return new MinionView
		{
			Id = minion.Id,
			Name = minion.Name,
			Attack = minion.Attack,
			Health = minion.Health,
			MaxHealth = minion.MaxHealth,
			Taunt = minion.Taunt,
			IsFrozen = minion.IsFrozen,
			IsStealth = minion.IsStealth,
			HasDivineShield = minion.HasDivineShield,
			CanAttack = minion.CanAttack(),
			CardId = minion.OriginalCard?.CardId ?? minion.Name,
			HasPoisonous = minion.HasPoisonous,
			HasWindfury = minion.HasWindfury,
			HasLifeSteal = minion.HasLifeSteal,
			HasReborn = minion.HasReborn,
			HasSummoningSickness = minion.HasSummoningSickness,
		};
	}

	private static WeaponView BuildWeaponView(Weapon weapon)
	{
		return new WeaponView
		{
			Id = weapon.Id,
			Name = weapon.Name,
			Attack = weapon.Attack,
			Durability = weapon.Durability,
			CardId = weapon.OriginalCard?.CardId ?? weapon.Name,
		};
	}

	private static HeroPowerView BuildHeroPowerView(HeroPower heroPower)
	{
		return new HeroPowerView
		{
			Name = heroPower.Name,
			ManaCost = heroPower.ManaCost,
			UsedThisTurn = heroPower.UsedThisTurn,
		};
	}

	private static PendingChoiceView BuildPendingChoice(
		GameState state,
		Player viewer,
		IReadOnlyList<(IGameAction Action, ActionContext Context)> canonicalLegalActions)
	{
		if (state.PendingChoice == null || state.PendingChoice.SourcePlayer != viewer)
		{
			return null;
		}

		return new PendingChoiceView
		{
			SourcePlayerId = viewer.Id,
			ChoiceKind = state.PendingChoice.GetType().Name,
			Options = IndexActions(canonicalLegalActions ?? state.PendingChoice.GetActions(state).ToList()),
		};
	}

	// canonicalLegalActions, when supplied, MUST be the exact list the caller will later resolve a
	// submitted index against (GameEngine.IsAllowedChoice matches by reference equality against the
	// original (IGameAction, ActionContext) instances) - recomputing GetValidActions/GetActions here
	// would silently hand out indices into a different, unresolvable list. Pass null only for
	// read-only/informational views where nothing will ever be submitted back against this snapshot.
	private static List<LegalActionView> BuildLegalActions(
		GameState state,
		Player viewer,
		IReadOnlyList<(IGameAction Action, ActionContext Context)> canonicalLegalActions)
	{
		if (state.PendingChoice != null)
		{
			if (state.PendingChoice.SourcePlayer != viewer) return new List<LegalActionView>();
			return IndexActions(canonicalLegalActions ?? state.PendingChoice.GetActions(state).ToList());
		}

		if (state.CurrentPlayer != viewer) return new List<LegalActionView>();

		return IndexActions(canonicalLegalActions ?? state.GetValidActions(viewer));
	}

	private static List<LegalActionView> IndexActions(IReadOnlyList<(IGameAction Action, ActionContext Context)> options)
	{
		var result = new List<LegalActionView>(options.Count);
		for (int i = 0; i < options.Count; i++)
		{
			var (action, context) = options[i];
			result.Add(new LegalActionView
			{
				Index = i,
				ActionType = action.GetType().Name,
				SourceEntityId = context.Source?.Id,
				TargetEntityId = context.Targets?.FirstOrDefault()?.Id,
				DisplayName = ActionDisplay.Describe(action, context),
			});
		}
		return result;
	}

	private static HistoryEntryView BuildHistoryEntry(HistoryEntry entry, Player viewer)
	{
		var context = entry.Context;
		var view = new HistoryEntryView
		{
			Turn = entry.Turn,
			PlayerId = entry.Player?.Id ?? Guid.Empty,
			ActionType = entry.Action?.GetType().Name,
			SourceId = context?.Source?.Id ?? context?.SourceCard?.Id,
			TargetId = context?.Targets?.FirstOrDefault()?.Id,
			SourceName = EntityName(context?.Source) ?? context?.SourceCard?.Name,
			SourceCardId = EntityCardId(context?.Source) ?? EntityCardId(context?.SourceCard),
			TargetName = EntityName(context?.Targets?.FirstOrDefault()),
			DamageDealt = context?.DamageDealt,
			HealedAmount = context?.HealedAmount,
		};

		// Summoned minions always land on the board, which is always public - safe unconditionally.
		if (context?.SummonedMinion != null)
		{
			view.SummonedMinionId = context.SummonedMinion.Id;
			view.SummonedMinionName = context.SummonedMinion.Name;
		}

		// A gained card lands in Hand, which stays hidden - only the owner gets to see what it was.
		if (context?.CardGained != null && context.CardGained.Owner == viewer)
		{
			view.CardGainedId = context.CardGained.Id;
			view.CardGainedName = context.CardGained.Name;
		}

		if (IsHiddenSecretReveal(entry, viewer))
		{
			view.ActionType = "SecretPlayed";
			view.SourceId = null;
			view.TargetId = null;
			view.SourceName = null;
			view.SourceCardId = null;
			view.TargetName = null;
		}

		return view;
	}

	// Card is included because context.Source is the played card itself for PlayCardAction.
	private static string EntityName(IGameEntity entity)
	{
		return entity switch
		{
			Player player => player.Name,
			Minion minion => minion.Name,
			Card card => card.Name,
			_ => null,
		};
	}

	// Same CardId convention as MinionView/CardView/WeaponView.CardId (the CardDatabase lookup key,
	// falling back to Name for cards not built via CardDatabase) - Player has no CardId, unlike
	// EntityName, so it's omitted here rather than falling through to null.
	private static string EntityCardId(IGameEntity entity)
	{
		return entity switch
		{
			Minion minion => minion.OriginalCard?.CardId ?? minion.Name,
			Card card => card.CardId ?? card.Name,
			_ => null,
		};
	}

	// A secret's identity must stay hidden until it resolves. Secret.cs has no "IsRevealed" flag,
	// so check whether this entry's card still backs an active Secret someone else owns.
	private static bool IsHiddenSecretReveal(HistoryEntry entry, Player viewer)
	{
		var card = entry.Context?.SourceCard as SpellCard;
		if (card == null || card.Owner == null || card.Owner == viewer)
		{
			return false;
		}

		return card.SpellCastEffects.Any(effect => effect.GameActions
			.OfType<SecretAction>()
			.Any(secretAction => secretAction.Secret != null && card.Owner.Secrets.Contains(secretAction.Secret)));
	}
}
