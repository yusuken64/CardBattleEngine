namespace CardBattleEngine;

public class SigilAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.SecretCasted;

	public Sigil Sigil { get; set; }

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is Player player)
		{
			player.Sigils.Add(Sigil);
			Sigil.Owner = player;
		}

		return [];
	}
}
