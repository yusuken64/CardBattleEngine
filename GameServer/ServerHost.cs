using CardBattleEngine;
using GameServer.Hubs;
using GameServer.Matches;

namespace GameServer;

public static class ServerHost
{
	public static WebApplication Build(WebApplicationBuilder builder)
	{
		builder.Services.AddSignalR(options =>
		{
			options.MaximumReceiveMessageSize = 1 * 1024 * 1024;
		});
		builder.Services.AddSingleton(new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data")));
		builder.Services.AddSingleton<MatchRegistry>();

		var app = builder.Build();

		app.MapHub<MatchHub>("/hubs/match");

		return app;
	}

	public static WebApplication BuildAndListen(string url)
	{
		var builder = WebApplication.CreateBuilder();
		builder.WebHost.UseUrls(url);
		return Build(builder);
	}
}
