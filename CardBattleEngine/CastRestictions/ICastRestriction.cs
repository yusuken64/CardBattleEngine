namespace CardBattleEngine;
public interface ICastRestriction
{
	public bool CanPlay(GameState gameState, Player player, Card castingCard, out string reason);
}