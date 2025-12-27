using System.Collections.Generic;

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
			.Where(t => t != null && t.IsAlive)
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

	public ActionContext() { }

	public ActionContext(ActionContext context)
	{
		this.SourcePlayer = context.SourcePlayer;
		this.SourceCard = context.SourceCard;
		this.Source = context.Source;
		this.Target = context.Target;
		this.Modifier = context.Modifier;
	}

	public IGameAction OriginalAction { get; internal set; }
	public Minion SummonedMinion { get; internal set; }
	public int PlayIndex { get; set; } = -1;
	public HeroPower SourceHeroPower { get; set; }
	public bool IsReborn { get; set; } = false;
	public bool IsAuraEffect { get; internal set; }
	public IGameEntity OriginalSource { get; internal set; }
	public int DamageDealt { get; internal set; }
	public int HealedAmount { get; internal set; }
	public int HealthDamageDealt { get; internal set; }
	public int ArmorDamageDealt { get; internal set; }
	public int ArmorGained { get; internal set; }
	public Minion SummonedMinionSnapShot { get; internal set; }

	public List<StatusDelta> ResolvedStatusChanges { get; } = new();
	public int CardsLeftInDeck { get; internal set; }
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