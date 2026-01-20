using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Mmt.Host.Game;
using Mmt.Host.Models;
using Mmt.Host.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5021");
builder.WebHost.UseStaticWebAssets();

// Add services to the container.

var channel = Channel.CreateUnbounded<PlayerUpdate>();
var gameState = new GameState(48);
var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};

builder.Services.AddSingleton(gameState);
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

app.MapPost("/join", ([FromBody] PlayerJoinRequest request) =>
{
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

    return Results.Ok(id);
});

app.MapGet("/ws/{id:guid}", async (HttpContext context, WebSocketHandler handler, [FromRoute] Guid id) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSockets only");
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();

    await handler.AddWebSocketAsync(id, ws);
});


await app.RunAsync();
