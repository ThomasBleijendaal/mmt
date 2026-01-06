using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Channels;

namespace Mmt.Host.Game;

public class GameService : BackgroundService
{
    private readonly GameState _gameState;
    private readonly WebSocketService _wss;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly ChannelReader<PlayerUpdate> _playerChannel;

    public GameService(
        GameState gameState,
        Channel<PlayerUpdate> playerChannel,
        WebSocketService wss,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _gameState = gameState;
        _wss = wss;
        _jsonSerializerOptions = jsonSerializerOptions;
        _playerChannel = playerChannel.Reader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var buffer = new byte[1024 * 1024];
        var memory = new Memory<byte>(buffer);
        using var memoryStream = new MemoryStream(buffer);

        do
        {
            var delay = Task.Delay(1000 / 60, stoppingToken);

            while (_playerChannel.TryRead(out var playerUpdate))
            {
                if (playerUpdate.Update is PlayerStateUpdate playerState)
                {
                    if (playerState.BlockPlaced)
                    {
                        _gameState.PlaceBlock(playerUpdate.Id, playerState.CurrentBlock.ToPositions());
                        _gameState.RemoveCurrentBlockFromPlayer(playerUpdate.Id);
                    }
                    else
                    {
                        _gameState.UpdateCurrentBlockOfPlayer(playerUpdate.Id, playerState.CurrentBlock.ToPositions());
                    }
                }
            }

            foreach (var (playerId, ws) in _wss.GetWebSockets())
            {
                var state = _gameState.GetNetworkState(playerId);

                memoryStream.Position = 0;
                JsonSerializer.Serialize(memoryStream, state, _jsonSerializerOptions);

                await ws.SendAsync(memory.Slice(0, (int)memoryStream.Position), WebSocketMessageType.Text, true, stoppingToken);
            }

            await delay;
        }
        while (!stoppingToken.IsCancellationRequested);
    }
}
