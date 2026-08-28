using CardBattleEngine;
using GameServer.Hubs;
using GameServer.Matches;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton(new CardDatabase(Path.Combine(AppContext.BaseDirectory, "Data")));
builder.Services.AddSingleton<MatchRegistry>();

var app = builder.Build();

app.MapHub<MatchHub>("/hubs/match");

app.Run();
