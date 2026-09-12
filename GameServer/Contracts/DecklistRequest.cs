namespace GameServer.Contracts;

public class DecklistRequest
{
	public string? LeaderDefinition { get; set; }
	public string PlayerName { get; set; } = string.Empty;
	public List<CardCount> Minions { get; set; } = new();
	public List<CardCount> Spells { get; set; } = new();
	public List<CardCount> Weapons { get; set; } = new();
	public List<string> CustomMinions { get; set; } = new();
	public List<string> CustomSpells { get; set; } = new();
	public List<string> CustomWeapons { get; set; } = new();
}

public class CardCount
{
	public string CardId { get; set; } = string.Empty;
	public int Count { get; set; }
}
