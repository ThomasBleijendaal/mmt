using System.Text.Json;
using System.Threading.Channels;
using EventCore;
using Microsoft.AspNetCore.Mvc;
using Mmt.Host.Game;
using Mmt.Host.Game.EventHandlers;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;
using Mmt.Host.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:8080");
builder.WebHost.UseStaticWebAssets();

// Add services to the container.

var channel = Channel.CreateUnbounded<PlayerUpdate>();

//var gameStateRepo = new GameStateRepository(60);

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};

builder.Services.AddEventCore();
builder.Services.AddInMemoryStorage();
builder.Services.AddEntity<GameEntity>();
builder.Services.AddEventListener<BoardSizeHandler>();
builder.Services.AddEventListener<PlayerReadyHandler>();
builder.Services.AddEventListener<PlaceBlockDamageHandler>();
builder.Services.AddEventListener<ClearRowsHandler>();

//builder.Services.AddSingleton(gameStateRepo);
builder.Services.AddSingleton(jsonSerializerOptions);
builder.Services.AddSingleton(channel);
builder.Services.AddHostedService<GameService>();
builder.Services.AddHostedService<WebSocketReadingService>();
builder.Services.AddHostedService<WebSocketSendingService>();
builder.Services.AddSingleton<WebSocketHandler>();

builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.

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
        // TODO: add finished game state + handling
        return Results.InternalServerError();
        // gameState.Reset();
    }

    if (game?.Status == GameStatus.Running)
    {
        return Results.BadRequest("Game already started");
    }

    if (game == null)
    {
        // TODO: configure that 60
        await session.Events.StartStreamAsync(new StartGame(gameId, 60));
    }

    var playerId = Guid.NewGuid();

    await session.Events.AppendAsync(new JoinGame(gameId, playerId, request.Name));

    // TODO: find solution to return color + next game id here

    return Results.Ok(new PlayerJoinResponse
    {
        GameId = gameId,
        PlayerId = playerId
    });
});

app.MapGet("/ws/{gameId:guid}/{playerId:guid}", async (HttpContext context, WebSocketHandler handler, [FromRoute] Guid gameId, [FromRoute] Guid playerId) =>
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
