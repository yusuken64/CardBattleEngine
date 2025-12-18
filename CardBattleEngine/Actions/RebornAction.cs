namespace CardBattleEngine;

public class RebornAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.Reborn;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return context.Source is Minion minion &&
			minion.HasReborn;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		yield return (
			new SummonMinionAction()
			{ 
				Card = context.SourceCard as MinionCard
			},
			new ActionContext()
			{
				SourcePlayer = context.SourcePlayer,
				SourceCard = context.SourceCard,
				PlayIndex = context.PlayIndex,
				OriginalAction = this,
				IsReborn = true,
			});
	}
}