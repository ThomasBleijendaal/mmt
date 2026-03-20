using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game.EventHandlers;

public class PlayerReadyHandler : IEventListener<ReadyPlayer, GameEntity>
{
    private readonly ChannelWriter<IEvent> _eventChannel;

    public PlayerReadyHandler(ChannelWriter<IEvent> eventChannel)
    {
        _eventChannel = eventChannel;
    }

    public async Task HandleAsync(ReadyPlayer @event, GameEntity entity)
    {
        if (entity.Status != GameStatus.PreGame)
        {
            return;
        }

        if (entity.Players.Count > 1 && entity.Players.All(x => x.Ready))
        {
            await _eventChannel.WriteAsync(new UpdateGameStatus(@event.Id, GameStatus.Running));
        }
    }
}
