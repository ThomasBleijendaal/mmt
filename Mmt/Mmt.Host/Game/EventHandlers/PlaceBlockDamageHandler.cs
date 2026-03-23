using EventCore;
using Mmt.Host.Game.Events;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class PlaceBlockDamageHandler : IEventListener<PlaceBlock, GameEntity>
{
    private readonly ISession _session;

    public PlaceBlockDamageHandler(ISession session)
    {
        _session = session;
    }

    public async Task HandleAsync(PlaceBlock @event, GameEntity entity)
    {
        if (@event.Positions.All(x => x.Y <= 3))
        {
            await _session.Events.AppendAsync(new UpdatePlayerHealth(@event.Id, @event.PlayerId, -3));
        }
    }
}
