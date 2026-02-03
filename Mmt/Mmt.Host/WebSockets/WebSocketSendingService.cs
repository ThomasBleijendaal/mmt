using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using Mmt.Host.Game;

namespace Mmt.Host.WebSockets;

internal class WebSocketSendingService : BackgroundService
{
    private readonly WebSocketHandler _handler;
    private readonly GameStateRepository _gameStateRepository;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public WebSocketSendingService(
        WebSocketHandler handler,
        GameStateRepository gameStateRepository,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _gameStateRepository = gameStateRepository;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1000 / 60.0), stoppingToken);

            foreach (var (gameId, state) in _gameStateRepository.GetGameIds())
            {
                await Task.WhenAll(_handler.GetAllWebSockets(gameId).Select(x => SendWebSocketAsync(state, x.playerId, x.ws, stoppingToken)));
            }
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private async Task SendWebSocketAsync(GameState gameState, Guid playerId, WebSocket ws, CancellationToken stoppingToken)
    {
        // this can wait on some channel and send the message to this web socket
        // if we do that, this class should have the same shape as the reading variant
        var state = gameState.GetNetworkState(playerId);

        var array = ArrayPool<byte>.Shared.Rent(64 * 1024);

        try
        {
            var memoryStream = new MemoryStream(array);
            var writer = new Utf8JsonWriter(memoryStream);

            JsonSerializer.Serialize(writer, state, _jsonSerializerOptions);

            var memory = new Memory<byte>(array, 0, (int)writer.BytesCommitted);

            try
            {
                await ws.SendAsync(memory, WebSocketMessageType.Text, true, stoppingToken);
            }
            catch
            {
                await _handler.RemoveWebSocketAsync(ws);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}
