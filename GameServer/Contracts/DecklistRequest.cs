namespace GameServer.Contracts;

public class DecklistRequest
{
	public string PlayerName { get; set; } = string.Empty;
	public List<CardCount> Minions { get; set; } = new();
	public List<CardCount> Spells { get; set; } = new();
}

public class CardCount
{
	public string CardId { get; set; } = string.Empty;
	public int Count { get; set; }
}
