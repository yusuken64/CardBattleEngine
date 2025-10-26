namespace CardBattleEngine;

internal class StartGameAction : GameActionBase
{
	public Action<IList<Card>> ShuffleFunction;
	public override EffectTrigger EffectTrigger => EffectTrigger.GameStart;

	public override bool IsValid(GameState state, ActionContext actionContext) => true;

	public override IEnumerable<GameActionBase> Resolve(GameState state, ActionContext actionContext)
	{
		ShuffleFunction(actionContext.SourcePlayer.Deck);

		return
		[
			new DrawCardAction(),
			new DrawCardAction(),
			new DrawCardAction(),
		];
	}
}