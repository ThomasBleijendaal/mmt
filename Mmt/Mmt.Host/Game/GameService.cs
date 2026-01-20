using System.Threading.Channels;
using Mmt.Host.Models;

namespace Mmt.Host.Game;

public class GameService : BackgroundService
{
    private readonly GameState _gameState;
    private readonly ChannelReader<PlayerUpdate> _playerChannel;

    public GameService(
        GameState gameState,
        Channel<PlayerUpdate> playerChannel)
    {
        _gameState = gameState;
        _playerChannel = playerChannel.Reader;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            var delay = Task.Delay(TimeSpan.FromMilliseconds(1000 / 60.0), stoppingToken);

            while (await _playerChannel.WaitToReadAsync(stoppingToken))
            {
                if (_playerChannel.TryRead(out var playerUpdate))
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
                    else if (playerUpdate.Update is ReadyUpdate)
                    {
                        _gameState.ReadyPlayer(playerUpdate.Id);
                    }
                }
            }

            await delay;
        }
        while (!stoppingToken.IsCancellationRequested);
    }
}
