
namespace CardBattleEngine;

public class SecretAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.SecretCasted;

	public Secret Secret { get; set; }

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		//TODO max secret exceeded, no duplicate?
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is Player player)
		{
			player.Secrets.Add(Secret);
			Secret.Owner = player;

			if (Secret.SecretTrigger != null &&
				!Secret.SecretTrigger.GameActions.OfType<SecretResolvedAction>().Any())
			{
				Secret.SecretTrigger.GameActions.Add(
					new SecretResolvedAction { Secret = Secret }
				);
			}
		}

		return [];
		//return [(new SecretResolvedAction() { Secret = Secret}, context)];
	}
}
public class SecretResolvedAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.SecretResolved;

	public Secret Secret { get; set; }

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.SourcePlayer is Player player)
		{
			player.Secrets.Remove(Secret);
		}

		return [];
	}
}