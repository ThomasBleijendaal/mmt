using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Mmt.Host.Game;
using Mmt.Host.Models;
using Mmt.Host.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:8080");
builder.WebHost.UseStaticWebAssets();

// Add services to the container.

var channel = Channel.CreateUnbounded<PlayerUpdate>();

var gameStateRepo = new GameStateRepository(60);

var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};

builder.Services.AddSingleton(gameStateRepo);
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

app.MapPost("/join", ([FromQuery(Name = "gameId")] string? gameIdString, [FromBody] PlayerJoinRequest request) =>
{
    var gameId = Guid.TryParse(gameIdString, out var gameIdGuid) ? gameIdGuid : Guid.NewGuid();

    var gameState = gameStateRepo.GetGame(gameId);

    if (gameState.Status == GameStatus.Finished)
    {
        gameState.Reset();
    }

    if (gameState.Status == GameStatus.Running)
    {
        return Results.BadRequest("Game already started");
    }

    var id = gameState.AddPlayer(request.Name, request.Color);
    if (id == null)
    {
        return Results.BadRequest("Duplicate color");
    }

    return Results.Ok(new PlayerJoinResponse
    {
        GameId = gameId,
        PlayerId = id.Value
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
