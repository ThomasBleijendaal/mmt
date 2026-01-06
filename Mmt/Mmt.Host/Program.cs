using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Mmt.Host.Game;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5021");

// Add services to the container.

var channel = Channel.CreateUnbounded<PlayerUpdate>();
var gameState = new GameState(30);
var jsonSerializerOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};

builder.Services.AddSingleton(gameState);
builder.Services.AddSingleton(jsonSerializerOptions);
builder.Services.AddSingleton(channel);
builder.Services.AddHostedService<GameService>();
builder.Services.AddSingleton<WebSocketService>();

builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseCors(b => b.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());
app.UseWebSockets(new() { KeepAliveInterval = TimeSpan.FromSeconds(10) });

app.MapPost("/join", ([FromBody] PlayerJoinRequest request) =>
{
    var id = gameState.AddPlayer(request.Name, request.Color);

    if (id == null)
    {
        return Results.BadRequest("Duplicate color");
    }

    return Results.Ok(id);
});

app.MapGet("/ws/{id:guid}", async (HttpContext context, WebSocketService wss, [FromRoute] Guid id) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSockets only");
        return;
    }

    var ws = await context.WebSockets.AcceptWebSocketAsync();
    wss.Add(id, ws);

    try
    {
        var buffer = new byte[1024 * 4];
        var memory = new Memory<byte>(buffer);

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(memory, CancellationToken.None);

            try
            {
                if (result.Count > 0)
                {
                    var data = memory[..result.Count];
                    var @string = Encoding.UTF8.GetString(data.Span);
                    var playerState = JsonSerializer.Deserialize<PlayerStateUpdate>(@string, jsonSerializerOptions);

                    if (playerState != null)
                    {
                        await channel.Writer.WriteAsync(
                            new PlayerUpdate
                            {
                                Id = id,
                                Update = playerState
                            });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client sent garbage: {0}", ex.Message);
            }
        }

        gameState.DropPlayer(id);
        wss.Remove(id);
    }
    catch
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync("Something happened");
    }
});

app.Run();
