using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game.EventHandlers;

public class GameFinishedHandler :
    IEventListener<DropPlayer, GameEntity>,
    IEventListener<UpdatePlayerHealth, GameEntity>
{
    private readonly ChannelWriter<IEvent> _eventChannel;

    public GameFinishedHandler(ChannelWriter<IEvent> eventChannel)
    {
        _eventChannel = eventChannel;
    }

    public Task HandleAsync(UpdatePlayerHealth @event, GameEntity entity) => CheckDeadPlayersAsync(entity);
    public Task HandleAsync(DropPlayer @event, GameEntity entity) => CheckDeadPlayersAsync(entity);

    private async Task CheckDeadPlayersAsync(GameEntity entity)
    {
        var playerCount = entity.Players.Count;
        var deadCount = entity.Players.Count(p => p.IsDead);

        if (playerCount > 1 && deadCount == playerCount - 1)
        {
            await _eventChannel.WriteAsync(new UpdateGameStatus(entity.Id, GameStatus.Finished));
        }
    }
}
