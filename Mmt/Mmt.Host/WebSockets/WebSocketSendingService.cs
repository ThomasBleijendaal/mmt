using System.Buffers;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;
using Mmt.Host.Game;
using Mmt.Host.Game.AudioEvents;
using Mmt.Host.Game.VisualEvents;
using Mmt.Host.Networking;

namespace Mmt.Host.WebSockets;

internal class WebSocketSendingService : BackgroundService
{
    private readonly WebSocketHandler _handler;
    private readonly EventCore.ISession _session;
    private readonly ChannelReader<AudioEvent> _audioChannel;
    private readonly ChannelReader<VisualEvent> _visualChannel;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public WebSocketSendingService(
        WebSocketHandler handler,
        EventCore.ISession session,
        ChannelReader<AudioEvent> audioChannel,
        ChannelReader<VisualEvent> visualChannel,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _session = session;
        _audioChannel = audioChannel;
        _visualChannel = visualChannel;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            await Task.Delay(TimeSpan.FromMilliseconds(1000 / 60.0), stoppingToken);

            foreach (var entity in _session.EntityCache.GetActiveEntities<GameEntity>())
            {
                var audioEvents = GetUniqueAudioEvents();
                var visualEvents = _visualChannel.ReadAvailable();

                await Task.WhenAll(
                    _handler.GetAllWebSockets(entity.Id).Select(x =>
                        SendWebSocketAsync(entity, x.playerId, audioEvents, visualEvents, x.ws, stoppingToken)));
            }
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private async Task SendWebSocketAsync(
        GameEntity gameEntity,
        Guid playerId,
        AudioEvent[] audioEvents,
        VisualEvent[] visualEvents,
        WebSocket ws,
        CancellationToken stoppingToken)
    {
        var state = NetworkGameState.Map(gameEntity, playerId, audioEvents, visualEvents);

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

    private AudioEvent[] GetUniqueAudioEvents()
        => _audioChannel.ReadAvailable()
            .GroupBy(x => x.Type)
            .Select(group =>
                group.FirstOrDefault(x => x.PlayerIds == null)
                ?? group.Aggregate(
                    Enumerable.Empty<Guid>(),
                    (playerIds, @event) => playerIds.Union(@event.PlayerIds!),
                    playerIds => new AudioEvent
                    {
                        Type = group.Key,
                        PlayerIds = playerIds.ToArray()
                    }))
            .ToArray();
}
