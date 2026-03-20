using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using Mmt.Host.Game;

namespace Mmt.Host.WebSockets;

internal class WebSocketSendingService : BackgroundService
{
    private readonly WebSocketHandler _handler;
    private readonly EventCore.ISession _session;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public WebSocketSendingService(
        WebSocketHandler handler,
        EventCore.ISession session,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _session = session;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1000 / 60.0), stoppingToken);

            foreach (var entity in _session.EntityCache.GetActiveEntities<GameEntity>())
            {
                await Task.WhenAll(_handler.GetAllWebSockets(entity.Id).Select(x => SendWebSocketAsync(entity, x.playerId, x.ws, stoppingToken)));
            }
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private async Task SendWebSocketAsync(GameEntity gameEntity, Guid playerId, WebSocket ws, CancellationToken stoppingToken)
    {
        var state = gameEntity.GetNetworkState(playerId);

        var array = ArrayPool<byte>.Shared.Rent(256 * 1024);

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
