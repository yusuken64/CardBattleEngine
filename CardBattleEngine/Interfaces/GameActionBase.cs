using System.Collections.Generic;

namespace CardBattleEngine;

// Basic effect/action that can be executed on a GameState
public interface IGameAction
{
	bool Canceled { get; set; }
	EffectTrigger EffectTrigger { get; }
	bool IsValid(GameState gameState, ActionContext context);
	IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context);
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> actionParam);
}

public abstract class GameActionBase : IGameAction
{
	public bool Canceled { get; set; }
	public abstract EffectTrigger EffectTrigger { get; }
	public abstract bool IsValid(GameState gameState, ActionContext context);
	public abstract IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context);
	public virtual void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public virtual Dictionary<string, object> EmitParams()
	{
		return new();
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
}
