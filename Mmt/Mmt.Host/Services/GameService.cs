using System.Threading.Channels;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;

namespace Mmt.Host.Services;

public class GameService : BackgroundService
{
    private readonly ChannelReader<PlayerUpdate> _playerChannel;
    private readonly EventCore.ISession _session;

    public GameService(
        EventCore.ISession session,
        Channel<PlayerUpdate> playerChannel)
    {
        _playerChannel = playerChannel.Reader;
        _session = session;
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
                            await _session.Events.AppendAsync(new PlaceBlock(
                                playerUpdate.GameId,
                                playerUpdate.PlayerId,
                                playerState.CurrentBlock.ToPositions()));

                            await _session.Events.AppendAsync(new RemovePlayerBlock(
                                playerUpdate.GameId,
                                playerUpdate.PlayerId));
                        }
                        else
                        {
                            await _session.Events.AppendAsync(new UpdatePlayerBlockPosition(
                                playerUpdate.GameId,
                                playerUpdate.PlayerId,
                                playerState.CurrentBlock.ToPositions(),
                                playerState.CenterPosition.ToPosition()));
                        }
                    }
                    else if (playerUpdate.Update is ReadyUpdate)
                    {
                        await _session.Events.AppendAsync(new ReadyPlayer(
                            playerUpdate.GameId,
                            playerUpdate.PlayerId));
                    }
                }
            }

            await delay;
        }
        while (!stoppingToken.IsCancellationRequested);
    }
}
