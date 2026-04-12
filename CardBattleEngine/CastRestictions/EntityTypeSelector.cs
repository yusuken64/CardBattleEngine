namespace CardBattleEngine;

public class EntityTypeSelector : IValidTargetSelector
{
	public EntityType EntityTypes { get; set; }  // note plural
	public TeamRelationship TeamRelationship { get; set; }

	public IEnumerable<IGameEntity> Select(GameState gameState, Player player, Card castingCard)
	{
		return gameState.GetAllEntities()
			.Where(entity =>
			MatchesType(entity, EntityTypes) &&
			MatchesTeam(entity, gameState, player, TeamRelationship));
	}

	public static bool MatchesType(IGameEntity entity, EntityType entityTypes)
	{
		return (entity switch
		{
			Player => EntityType.Player,
			Minion => EntityType.Minion,
			Card => EntityType.Card,
			Weapon => EntityType.Weapon,
			_ => EntityType.None
		} & entityTypes) != 0;
	}

	public static bool MatchesTeam(IGameEntity entity, GameState gameState, Player player, TeamRelationship teamRelationship)
	{
		return teamRelationship switch
		{
			TeamRelationship.Friendly => entity.Owner == player,
			TeamRelationship.Enemy => entity.Owner == gameState.OpponentOf(player),
			TeamRelationship.Any => true,
			_ => false
		};
	}
}
