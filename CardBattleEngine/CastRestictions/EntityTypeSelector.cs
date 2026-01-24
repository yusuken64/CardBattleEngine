namespace CardBattleEngine;

public class EntityTypeSelector : IValidTargetSelector
{
	public EntityType EntityTypes { get; set; }  // note plural
	public TeamRelationship TeamRelationship { get; set; }

	public List<IGameEntity> Select(GameState gameState, Player player, Card castingCard)
	{
		return gameState.GetAllEntities()
			.Where(entity => MatchesType(entity) && MatchesTeam(entity, gameState, player))
			.ToList();
	}

	private bool MatchesType(IGameEntity entity)
	{
		return (entity switch
		{
			Player => EntityType.Player,
			Minion => EntityType.Minion,
			Card => EntityType.Card,
			Weapon => EntityType.Weapon,
			_ => EntityType.None
		} & EntityTypes) != 0;
	}

	private bool MatchesTeam(IGameEntity entity, GameState gameState, Player player)
	{
		return TeamRelationship switch
		{
			TeamRelationship.Friendly => entity.Owner == player,
			TeamRelationship.Enemy => entity.Owner == gameState.OpponentOf(player),
			TeamRelationship.Any => true,
			_ => false
		};
	}
}
