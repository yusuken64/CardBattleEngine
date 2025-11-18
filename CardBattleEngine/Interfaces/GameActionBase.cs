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

	public IGameAction OriginalAction { get; internal set; }
	public Minion SummonedMinion { get; internal set; }
}
