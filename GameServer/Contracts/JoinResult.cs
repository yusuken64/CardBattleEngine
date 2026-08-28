namespace GameServer.Contracts;

public class JoinResult
{
	public bool Success { get; set; }
	public string? Error { get; set; }
}

public class ActionResult
{
	public bool Success { get; set; }
	public string? Error { get; set; }
}
