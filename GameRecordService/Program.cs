using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace GameRecorderAPI;

// DTO class for the game record
public class GameRecordDto
{
	public string GameId { get; set; }
	public string CardID { get; set; }
	public bool DidWin { get; set; }
	public byte PlayerOrder { get; set; }
	public bool? WasDrawn { get; set; }
	public bool WasPlayed { get; set; }
	public int CopiesPlayed { get; set; }
	public int? TurnPlayed { get; set; }
	public int TotalTurns { get; set; }
}

public class Program
{
	public static void Main(string[] args)
	{
		Console.WriteLine("Starting GameRecordService");
		CreateHostBuilder(args).Build().Run();
	}

	public static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureWebHostDefaults(webBuilder =>
			{
				webBuilder.Configure(app =>
				{
					var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
					if (string.IsNullOrEmpty(connectionString))
						throw new Exception("ConnectionStrings__DefaultConnection not set");
					//var connectionString = "Server=localhost,1433;Database=master;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;";

					app.UseRouting();

					app.UseEndpoints(endpoints =>
					{
						endpoints.MapGet("/", () =>
						{
							Console.WriteLine("TestEndPoint called");
							return "Hello from GET /";
						});
						endpoints.MapPost("/record", async context =>
						{
							// Deserialize JSON from request body
							var dto = await JsonSerializer.DeserializeAsync<GameRecordDto>(context.Request.Body);

							if (dto == null)
							{
								context.Response.StatusCode = 400;
								await context.Response.WriteAsync("Invalid request");
								return;
							}

							using var conn = new SqlConnection(connectionString);
							await conn.OpenAsync();

							var query = @"
                                    INSERT INTO GameRecord (
                                        GameId, CardID, DidWin, PlayerOrder,
                                        WasDrawn, WasPlayed, CopiesPlayed,
                                        TurnPlayed, TotalTurns
                                    )
                                    VALUES (
                                        @GameId, @CardID, @DidWin, @PlayerOrder,
                                        @WasDrawn, @WasPlayed, @CopiesPlayed,
                                        @TurnPlayed, @TotalTurns
                                    )";

							using var cmd = new SqlCommand(query, conn);
							cmd.Parameters.AddWithValue("@GameId", dto.GameId);
							cmd.Parameters.AddWithValue("@CardID", dto.CardID);
							cmd.Parameters.AddWithValue("@DidWin", dto.DidWin);
							cmd.Parameters.AddWithValue("@PlayerOrder", dto.PlayerOrder);
							cmd.Parameters.AddWithValue("@WasDrawn", dto.WasDrawn ?? (object)DBNull.Value);
							cmd.Parameters.AddWithValue("@WasPlayed", dto.WasPlayed);
							cmd.Parameters.AddWithValue("@CopiesPlayed", dto.CopiesPlayed);
							cmd.Parameters.AddWithValue("@TurnPlayed", dto.TurnPlayed.HasValue ? (object)dto.TurnPlayed.Value : DBNull.Value);
							cmd.Parameters.AddWithValue("@TotalTurns", dto.TotalTurns);

							await cmd.ExecuteNonQueryAsync();

							context.Response.StatusCode = 200;
							await context.Response.WriteAsync("OK");
						});
					});
				});
			});
}