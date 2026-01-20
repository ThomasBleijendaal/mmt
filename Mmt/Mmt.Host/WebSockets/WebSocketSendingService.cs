using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Mmt.Host.Game;

namespace Mmt.Host.WebSockets;

internal class WebSocketSendingService : BackgroundService
{
    private readonly WebSocketHandler _handler;
    private readonly GameState _gameState;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public WebSocketSendingService(
        WebSocketHandler handler,
        GameState gameState,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _gameState = gameState;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1000 / 60.0), stoppingToken);
            await Task.WhenAll(_handler.GetAllWebSockets().Select(x => SendWebSocketAsync(x.id, x.ws, stoppingToken)));
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private async Task SendWebSocketAsync(Guid playerId, WebSocket ws, CancellationToken stoppingToken)
    {
        // this can wait on some channel and send the message to this web socket
        // if we do that, this class should have the same shape as the reading variant
        var state = _gameState.GetNetworkState(playerId);

        var @string = JsonSerializer.Serialize(state, _jsonSerializerOptions);
        var buffer = Encoding.UTF8.GetBytes(@string);
        var memory = new Memory<byte>(buffer);

        try
        {
            await ws.SendAsync(memory, WebSocketMessageType.Text, true, stoppingToken);
        }
        catch
        {
            await _handler.RemoveWebSocketAsync(ws);
        }
    }
}
