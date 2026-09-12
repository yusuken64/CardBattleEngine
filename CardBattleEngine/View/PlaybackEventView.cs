namespace CardBattleEngine.View;

// Value-only presentation payload. No executable action, selectors, or unredacted GameState.
public class PlaybackEntityView
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public CardView Card { get; set; }
    public MinionView Minion { get; set; }
}

public class PlaybackAmountView
{
    public PlaybackEntityView Target { get; set; }
    public int Amount { get; set; }
}

public class PlaybackStatusView
{
    public PlaybackEntityView Target { get; set; }
    public StatusType Status { get; set; }
    public bool Gained { get; set; }
}

public class PlaybackEventView
{
    public long Sequence { get; set; }
    public string ActionType { get; set; }
    public Guid PlayerId { get; set; }
    public PlaybackEntityView Source { get; set; }
    public PlaybackEntityView Target { get; set; }
    public PlaybackEntityView SourceCard { get; set; }
    public PlaybackEntityView SummonedMinion { get; set; }
    public PlaybackEntityView CardGained { get; set; }
    public PlaybackEntityView TriggerSource { get; set; }
    public List<PlaybackAmountView> AffectedEntities { get; set; } = new();
    public List<PlaybackStatusView> StatusChanges { get; set; } = new();
    public int DamageDealt { get; set; }
    public int HealedAmount { get; set; }
    public int ArmorGained { get; set; }
    public int ManaSpent { get; set; }
    public int PlayIndex { get; set; }
    public int CardsLeftInDeck { get; set; }
    public bool IsAttack { get; set; }
    public bool HiddenCardPlayed { get; set; }
    public string PresentationEffectId { get; set; }
    public PlayerGameView After { get; set; }
}
