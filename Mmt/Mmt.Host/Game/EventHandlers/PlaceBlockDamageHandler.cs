using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game.EventHandlers;

public class PlaceBlockDamageHandler : IEventListener<PlaceBlock, GameEntity>
{
    private readonly ChannelWriter<IEvent> _eventChannel;

    public PlaceBlockDamageHandler(ChannelWriter<IEvent> eventChannel)
    {
        _eventChannel = eventChannel;
    }

    public async Task HandleAsync(PlaceBlock @event, GameEntity entity)
    {
        if (@event.Positions.All(x => x.Y <= 3))
        {
            await _eventChannel.WriteAsync(new UpdatePlayerHealth(@event.Id, @event.PlayerId, -3));
        }
    }
}
