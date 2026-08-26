namespace GameServer.Matches;

public readonly record struct MatchId(Guid Value)
{
	public static MatchId New() => new(Guid.NewGuid());

	public override string ToString() => Value.ToString();
}
