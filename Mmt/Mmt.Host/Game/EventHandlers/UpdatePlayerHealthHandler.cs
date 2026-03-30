using System.Threading.Channels;
using EventCore;
using Mmt.Host.Game.Events;
using Mmt.Host.Game.VisualEvents;

namespace Mmt.Host.Game.EventHandlers;

public class UpdatePlayerHealthHandler : IEventListener<UpdatePlayerHealth, GameEntity>
{
    private readonly ChannelWriter<VisualEvent> _visualChannel;

    public UpdatePlayerHealthHandler(
        ChannelWriter<VisualEvent> visualChannel)
    {
        _visualChannel = visualChannel;
    }

    public async Task HandleAsync(UpdatePlayerHealth @event, GameEntity entity)
    {
        VisualEvent ve = @event.Delta >= 0
            ? new HealEvent
            {
                PlayerIds = [@event.PlayerId]
            }
            : new DamageEvent
            {
                PlayerIds = [@event.PlayerId]
            };

        await _visualChannel.WriteAsync(ve);
    }
}
