using System.Text.Json;
using System.Threading.Channels;
using EventCore;
using Microsoft.AspNetCore.Mvc;
using Mmt.Host.Game;
using Mmt.Host.Game.AudioEvents;
using Mmt.Host.Game.EventHandlers;
using Mmt.Host.Game.Events;
using Mmt.Host.Game.VisualEvents;
using Mmt.Host.Models;
using Mmt.Host.Services;
using Mmt.Host.WebSockets;

var gameSize = 60;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:8080");
builder.WebHost.UseStaticWebAssets();

var playerUpdateChannel = Channel.CreateUnbounded<PlayerUpdate>();
var audioChannel = Channel.CreateUnbounded<AudioEvent>();
var visualChannel = Channel.CreateUnbounded<VisualEvent>();

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};

builder.Services.AddEventCore();
builder.Services.AddInMemoryStorage();
builder.Services.AddEntity<GameEntity>();
builder.Services.AddEventListener<BoardSizeHandler>();
builder.Services.AddEventListener<ClearRowsHandler>();
builder.Services.AddEventListener<CompressFieldHandler>();
builder.Services.AddEventListener<GameFinishedHandler>();
builder.Services.AddEventListener<PlaceBlockDamageHandler>();
builder.Services.AddEventListener<PlayerReadyHandler>();
builder.Services.AddEventListener<RemoveBlocksHandler>();
builder.Services.AddEventListener<UpdatePlayerHealthHandler>();

builder.Services.AddSingleton(jsonSerializerOptions);
builder.Services.AddSingleton(playerUpdateChannel);
builder.Services.AddSingleton(audioChannel.Reader);
builder.Services.AddSingleton(audioChannel.Writer);
builder.Services.AddSingleton(visualChannel.Reader);
builder.Services.AddSingleton(visualChannel.Writer);
builder.Services.AddHostedService<GameService>();
builder.Services.AddHostedService<WebSocketReadingService>();
builder.Services.AddHostedService<WebSocketSendingService>();
builder.Services.AddSingleton<WebSocketHandler>();

builder.Services.AddCors();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(b => b.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
app.UseWebSockets(new() { KeepAliveInterval = TimeSpan.FromSeconds(10) });

app.MapPost("/join",
    async ([FromQuery(Name = "gameId")] string? gameIdString,
    [FromBody] PlayerJoinRequest request,
    [FromServices] EventCore.ISession session) =>
{
    var gameId = Guid.TryParse(gameIdString, out var gameIdGuid) ? gameIdGuid : Guid.NewGuid();

    var game = await session.Events.AggregateStreamAsync<GameEntity>(gameId);

    if (game?.Status == GameStatus.Finished)
    {
        await session.Events.AppendAsync(new ResetGame(gameId));
    }

    if (game?.Status == GameStatus.Running)
    {
        return Results.BadRequest("Game already started");
    }

    if (game == null)
    {
        await session.Events.StartStreamAsync(new StartGame(gameId, gameSize));
    }

    var playerId = Guid.NewGuid();

    await session.Events.AppendAsync(new JoinGame(gameId, playerId, request.Name));

    game = await session.Events.AggregateStreamAsync<GameEntity>(gameId);
    if (game == null)
    {
        return Results.BadRequest("Failed to start game");
    }

    return Results.Ok(new PlayerJoinResponse
    {
        GameId = gameId,
        NextGameId = game.NextGameId,
        PlayerId = playerId,
        PlayerColor = game.Players.Single(x => x.Id == playerId).Color
    });
});

app.MapGet("/ws/{gameId:guid}/{playerId:guid}",
    async (HttpContext context,
    WebSocketHandler handler,
    [FromRoute] Guid gameId,
    [FromRoute] Guid playerId) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSockets only");
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();

    await handler.AddWebSocketAsync(gameId, playerId, ws);
});


await app.RunAsync();
