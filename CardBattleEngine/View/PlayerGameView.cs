namespace CardBattleEngine.View;

public class PlayerGameView
{
	public long StateRevision { get; set; }
	public long PlaybackSequence { get; set; }
	public List<PlaybackEventView> PlaybackEvents { get; set; } = new();
	public Guid ViewerPlayerId { get; set; }
	public int Turn { get; set; }
	public Guid CurrentPlayerId { get; set; }
	public bool IsGameOver { get; set; }
	public Guid? WinnerPlayerId { get; set; }
	public PublicPlayerView Self { get; set; }
	public PublicPlayerView Opponent { get; set; }

	// Non-null only when this viewer is the one making the choice; the other player only sees OpponentIsChoosing.
	public PendingChoiceView? PendingChoice { get; set; }
	public bool OpponentIsChoosing { get; set; }

	// Populated only when it's this viewer's turn to submit something (their own turn actions, or their own pending choice).
	public List<LegalActionView> LegalActions { get; set; } = new();

	// Echoes the caller-supplied prompt version (e.g. GameServer's PendingActionSet.Version) back to the
	// client so it can be replayed alongside a chosen LegalActionView.Index when submitting an action.
	// Null when LegalActions is empty / there's nothing to submit right now. Opaque to PlayerViewBuilder itself.
	public int? PromptVersion { get; set; }

	public List<HistoryEntryView> NewHistory { get; set; } = new();
}

public class PublicPlayerView
{
	public Guid PlayerId { get; set; }
	public string Name { get; set; }
	public int Health { get; set; }
	public int MaxHealth { get; set; }
	public int Armor { get; set; }
	public int Fatigue { get; set; }
	public int Mana { get; set; }
	public int MaxMana { get; set; }
	public int Attack { get; set; }
	public bool IsAlive { get; set; }
	public bool HasAttackedThisTurn { get; set; }
	public bool IsFrozen { get; set; }
	public bool IsStealth { get; set; }

	public int HandCount { get; set; }
	public List<CardView>? Hand { get; set; } // null unless this is the viewer's own player

	public int DeckCount { get; set; } // count-only for both self and opponent; order/contents never sent

	public List<MinionView> Board { get; set; } = new();
	public List<MinionView> Graveyard { get; set; } = new();

	public WeaponView? EquippedWeapon { get; set; }
	public HeroPowerView? HeroPower { get; set; }

	public int SecretCount { get; set; }
	public List<SecretView>? Secrets { get; set; } // null unless this is the viewer's own player
}

public class CardView
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public int ManaCost { get; set; }
	public CardType Type { get; set; }
	public int? Attack { get; set; }
	public int? Health { get; set; }
	public string? CardId { get; set; }
}

public class MinionView
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; }
	public int MaxHealth { get; set; }
	public bool Taunt { get; set; }
	public bool IsFrozen { get; set; }
	public bool IsStealth { get; set; }
	public bool HasDivineShield { get; set; }
	public bool CanAttack { get; set; }
	public string? CardId { get; set; }
	public bool HasPoisonous { get; set; }
	public bool HasWindfury { get; set; }
	public bool HasLifeSteal { get; set; }
	public bool HasReborn { get; set; }
	public bool HasSummoningSickness { get; set; }
	public bool HasDeathRattle { get; set; }
	public bool HasTrigger { get; set; }
}

public class WeaponView
{
	public Guid Id { get; set; }
	public string Name { get; set; }
	public int Attack { get; set; }
	public int Durability { get; set; }
	public string? CardId { get; set; }
}

public class HeroPowerView
{
	public CardView? LeaderCard { get; set; }
	public string Name { get; set; }
	public int ManaCost { get; set; }
	public bool UsedThisTurn { get; set; }
}

// Only ever populated on a player's own PublicPlayerView.Secrets - a player always knows their own secrets.
// Index is the secret's position in the owner's Secrets list, stable enough for a client to track
// locally against what it played. Name/CardId come from Secret.SourceCard and are only ever populated
// for the owner - PlayerViewBuilder.BuildOpponent nulls the whole Secrets list for the opponent, so
// these two fields can never reach anyone but the secret's own owner.
public class SecretView
{
	public int Index { get; set; }
	public string Name { get; set; }
	public Guid? CardId { get; set; }
}

public class PendingChoiceView
{
	public Guid SourcePlayerId { get; set; }
	public string ChoiceKind { get; set; }
	public List<LegalActionView> Options { get; set; } = new();
}

// Index is the only thing the client echoes back to submit an action - see PlayerViewBuilder.Build's
// canonicalLegalActions parameter for why this must be built from the exact list the server will validate against.
public class LegalActionView
{
	public int Index { get; set; }
	public string ActionType { get; set; }
	public Guid? SourceEntityId { get; set; }
	public Guid? TargetEntityId { get; set; }
	public string DisplayName { get; set; }
}

public class HistoryEntryView
{
	public int Turn { get; set; }
	public Guid PlayerId { get; set; }
	public string ActionType { get; set; }
	public Guid? SourceId { get; set; }
	public Guid? TargetId { get; set; }
	public string SourceName { get; set; }
	// The played/acting card's CardId (same convention as MinionView/CardView.CardId - the card's
	// Name) - lets a client request art for a spell it saw the opponent cast, since spells have no
	// persistent board entity of their own to carry a CardId the way minions/weapons do.
	public string? SourceCardId { get; set; }
	public string TargetName { get; set; }
	public int? DamageDealt { get; set; }
	public int? HealedAmount { get; set; }
	public Guid? SummonedMinionId { get; set; }
	public string? SummonedMinionName { get; set; }
	public Guid? CardGainedId { get; set; }
	public string? CardGainedName { get; set; }
}
