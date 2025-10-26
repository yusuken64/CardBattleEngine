
namespace CardBattleEngine;

// Basic effect/action that can be executed on a GameState
public interface IGameAction
{
	bool Canceled { get; set; }
	EffectTrigger EffectTrigger { get; }

	bool IsValid(GameState gameState);
	IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent); //returns side effects
	Dictionary<string, object> EmitParams();
	void ConsumeParams(Dictionary<string, object> actionParam);
}

public abstract class GameActionBase : IGameAction
{
	public bool Canceled { get; set; }
	public abstract EffectTrigger EffectTrigger { get; }
	public abstract bool IsValid(GameState gameState);
	public abstract IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent);

	public virtual void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public virtual Dictionary<string, object> EmitParams()
	{
		return new();
	}

}