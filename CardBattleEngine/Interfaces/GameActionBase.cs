namespace CardBattleEngine;

// Basic effect/action that can be executed on a GameState
public interface IGameAction
{
	bool Canceled { get; set; }
	EffectTrigger EffectTrigger { get; }
	bool IsValid(GameState gameState, ActionContext context, out string reason);
	IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context);
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> actionParam);
	object CustomSFX { get; set; } //this will be used the client to assosiated SFX To Action when animated
	IGameAction Clone();
}

public abstract class GameActionBase : IGameAction
{
	public bool Canceled { get; set; }
	public abstract EffectTrigger EffectTrigger { get; }

	public abstract bool IsValid(
		GameState state,
		ActionContext context,
		out string reason
	);

	public abstract IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context);
	protected IReadOnlyList<IGameEntity> ResolveTargets(
	GameState state,
	ActionContext context)
	{
		IEnumerable<IGameEntity> targets;

		if (context.AffectedEntitySelector != null)
		{
			targets = context.AffectedEntitySelector.Select(state, context);
		}
		else if (context.Target != null)
		{
			targets = [context.Target];
		}
		else if (context.SourcePlayer != null)
		{
			//fallback for effects that imply affects the sourceplayer i.e. draw card
			targets = [context.SourcePlayer];
		}
		else
		{
			targets = Enumerable.Empty<IGameEntity>();
		}

		// Snapshot to avoid mutation during iteration
		return targets
			.Where(t => t != null && t.IsAlive && t.Health > 0)
			.ToList();
	}
	public virtual void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public virtual Dictionary<string, object> EmitParams()
	{
		return new();
	}
	public object CustomSFX { get; set; }

	public virtual IGameAction Clone()
	{
		var clone = (IGameAction)MemberwiseClone();
		clone.Canceled = false;
		return clone;
	}
}

//TODO make different implementations for context for each action??
public class ActionContext
{
	public Player SourcePlayer;
	public Card SourceCard;
	public IGameEntity Source;
	public IGameEntity Target;
	public StatModifier Modifier;

	public IAffectedEntitySelector AffectedEntitySelector;

	// Lazily allocated: most ActionContexts never touch variables, so avoid an
	// unconditional Dictionary allocation on every one of the many contexts created per action/trigger.
	private Dictionary<string, object> _variables;

	public ActionContext() { }

	public ActionContext(ActionContext context)
	{
		this.SourcePlayer = context.SourcePlayer;
		this.SourceCard = context.SourceCard;
		this.Source = context.Source;
		this.Target = context.Target;
		this.Modifier = context.Modifier;
		this._variables = context._variables == null ? null : new(context._variables);
		this._affectedEntities = context._affectedEntities == null ? null : [.. context._affectedEntities];
		this.IsAttack = context.IsAttack;
	}

	public IGameAction OriginalAction { get; set; }
	public bool AuthorizedToEquipWeapon { get; set; }
	public Minion SummonedMinion { get; set; }
	public int PlayIndex { get; set; } = -1;
	public HeroPower SourceHeroPower { get; set; }
	public bool IsReborn { get; set; } = false;
	public bool IsAuraEffect { get; internal set; }
	public IGameEntity OriginalSource { get; set; }
	public int DamageDealt { get; internal set; }
	public int HealedAmount { get; internal set; }
	public int HealthDamageDealt { get; internal set; }
	public int ArmorDamageDealt { get; internal set; }
	public int ArmorGained { get; internal set; }
	public Minion SummonedMinionSnapShot { get; internal set; }

	// Lazily allocated: see _variables above, same rationale.
	private List<StatusDelta> _resolvedStatusChanges;
	public List<StatusDelta> ResolvedStatusChanges
	{
		get => _resolvedStatusChanges ??= new();
		internal set => _resolvedStatusChanges = value;
	}
	public int CardsLeftInDeck { get; internal set; }

	private List<(IGameEntity, int)> _affectedEntities;
	public List<(IGameEntity, int)> AffectedEntities
	{
		get => _affectedEntities ??= new();
		internal set => _affectedEntities = value;
	}
	public bool IsAttack { get; internal set; }
	public Card CardGained { get; internal set; }

	public void SetVar(string variableName, object value)
	{
		if (string.IsNullOrEmpty(variableName))
			throw new ArgumentException(nameof(variableName));

		// For now: numbers only
		if (value is not int)
			throw new InvalidOperationException(
				$"Variable '{variableName}' must be an int");

		(_variables ??= new())[variableName] = value;
	}

	public object GetVar(string variableName)
	{
		if (string.IsNullOrEmpty(variableName))
			throw new ArgumentException(nameof(variableName));

		return _variables != null && _variables.TryGetValue(variableName, out var value)
			? value
			: 0; // Missing vars default to 0
	}

	public ActionContext ShallowCopy()
	{
		var newContext = new ActionContext(this);
		newContext.SummonedMinionSnapShot = this.SummonedMinionSnapShot;
		newContext.ArmorGained = this.ArmorGained;
		newContext.ArmorDamageDealt = this.ArmorDamageDealt;
		newContext.HealthDamageDealt = this.HealthDamageDealt;
		newContext.HealedAmount = this.HealedAmount;
		newContext.DamageDealt = this.DamageDealt;
		newContext.SourceHeroPower = this.SourceHeroPower;
		newContext.IsAuraEffect = this.IsAuraEffect;
		newContext.IsReborn = this.IsReborn;
		newContext._resolvedStatusChanges = this._resolvedStatusChanges == null ? null : new List<StatusDelta>(this._resolvedStatusChanges);
		newContext.PlayIndex = this.PlayIndex;
		newContext.SummonedMinion = this.SummonedMinion;
		newContext.OriginalAction = this.OriginalAction;
		newContext.OriginalSource = this.OriginalSource;
		newContext.CardsLeftInDeck = this.CardsLeftInDeck;
		newContext.IsAttack = this.IsAttack;
		newContext.CardGained = this.CardGained;

		return newContext;
	}
}

public class StatusDelta
{
	public IGameEntity Target { get; }
	public StatusType Status { get; }
	public bool Gained { get; }

	public StatusDelta(IGameEntity target, StatusType status, bool gained)
	{
		Target = target;
		Status = status;
		Gained = gained;
	}
}

public enum StatusType
{
	Freeze,
	Stealth
}