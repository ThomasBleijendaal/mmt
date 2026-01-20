using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Mmt.Host.Game;
using Mmt.Host.Models;

namespace Mmt.Host.WebSockets;

internal class WebSocketReadingService : BackgroundService
{
    private readonly WebSocketHandler _handler;
    private readonly Channel<PlayerUpdate> _playerChannel;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    private readonly ConcurrentDictionary<WebSocket, Task> _readingTasks = new();

    public WebSocketReadingService(
        WebSocketHandler handler,
        Channel<PlayerUpdate> playerChannel,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _handler = handler;
        _playerChannel = playerChannel;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            await Task.Delay(100, stoppingToken);

            // add any missing web sockets
            foreach (var (id, ws) in _handler.GetAllWebSockets())
            {
                if (!_readingTasks.ContainsKey(ws))
                {
                    _readingTasks.TryAdd(ws, ReadWebSocketAsync(id, ws, stoppingToken));
                }
            }

            // remove any closed web sockets
            foreach (var kv in _readingTasks.Where(x => x.Value.IsCompleted).ToArray())
            {
                _readingTasks.TryRemove(kv);
            }
        }
        while (!stoppingToken.IsCancellationRequested);

        try
        {
            await Task.WhenAll(_readingTasks.Values);
        }
        catch
        {
        }
    }

    private async Task ReadWebSocketAsync(Guid id, WebSocket ws, CancellationToken stoppingToken)
    {
        try
        {
            var buffer = new byte[1024 * 4];
            var memory = new Memory<byte>(buffer);

            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(memory, stoppingToken);

                if (result.Count > 0)
                {
                    var data = memory[..result.Count];
                    var @string = Encoding.UTF8.GetString(data.Span);

                    PlayerStateUpdate? playerState = null;
                    try
                    {
                        playerState = JsonSerializer.Deserialize<PlayerStateUpdate>(@string, _jsonSerializerOptions);
                    }
                    catch { }

                    if (playerState != null)
                    {
                        await _playerChannel.Writer.WriteAsync(
                            new PlayerUpdate
                            {
                                Id = id,
                                Update = playerState
                            });
                    }
                    else
                    {
                        await _playerChannel.Writer.WriteAsync(
                            new PlayerUpdate
                            {
                                Id = id,
                                Update = new ReadyUpdate()
                            });
                    }
                }
            }
        }
        catch { }

        await _handler.RemoveWebSocketAsync(ws);
    }
}
