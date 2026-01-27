using System.Threading.Channels;
using Mmt.Host.Models;

namespace Mmt.Host.Game;

public class GameService : BackgroundService
{
    private readonly GameStateRepository _gameStateRepository;
    private readonly ChannelReader<PlayerUpdate> _playerChannel;

    public GameService(
        GameStateRepository gameStateRepository,
        Channel<PlayerUpdate> playerChannel)
    {
        _gameStateRepository = gameStateRepository;
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
                    var gameState = _gameStateRepository.GetGame(playerUpdate.GameId);

                    if (playerUpdate.Update is PlayerStateUpdate playerState)
                    {
                        if (playerState.BlockPlaced)
                        {
                            gameState.PlaceBlock(playerUpdate.PlayerId, playerState.CurrentBlock.ToPositions());
                            gameState.RemoveCurrentBlockFromPlayer(playerUpdate.PlayerId);
                        }
                        else
                        {
                            gameState.UpdateCurrentBlockOfPlayer(playerUpdate.PlayerId, playerState.CurrentBlock.ToPositions());
                        }
                    }
                    else if (playerUpdate.Update is ReadyUpdate)
                    {
                        gameState.ReadyPlayer(playerUpdate.PlayerId);
                    }
                }
            }

            await delay;
        }
        while (!stoppingToken.IsCancellationRequested);
    }
}
