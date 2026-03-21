using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game.EventHandlers;

public class ClearedRowsHandler : IEventListener<AddClearedRowsCount, GameEntity>
{
    private readonly ChannelWriter<IEvent> _eventChannel;

    public ClearedRowsHandler(ChannelWriter<IEvent> eventChannel)
    {
        _eventChannel = eventChannel;
    }

    public async Task HandleAsync(AddClearedRowsCount @event, GameEntity entity)
    {
        if (entity.RowsCleared % 10 > @event.Count)
        {
            foreach (var player in entity.Players)
            {
                await _eventChannel.WriteAsync(new UpdatePlayerHealth(@event.Id, player.Id, 20));
            }
        }
    }
}
