# Build context must be the repo root (GameServer references sibling projects by relative path):
#   docker build -f GameServer/Dockerfile -t cardbattleengine-gameserver .
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["GameServer/GameServer.csproj", "GameServer/"]
COPY ["GameRunner/GameRunner.csproj", "GameRunner/"]
COPY ["CardBattleEngine/CardBattleEngine.csproj", "CardBattleEngine/"]
RUN dotnet restore "GameServer/GameServer.csproj"

COPY GameServer/ GameServer/
COPY GameRunner/ GameRunner/
COPY CardBattleEngine/ CardBattleEngine/
RUN dotnet build "GameServer/GameServer.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GameServer/GameServer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameServer.dll"]
