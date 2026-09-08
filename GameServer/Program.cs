using GameServer;

var app = ServerHost.Build(WebApplication.CreateBuilder(args));

app.Run();
