using EventCore;
using Mmt.Host.Game.Events;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class ClearedRowsHandler : IEventListener<AddClearedRowsCount, GameEntity>
{
    private readonly ISession _session;

    public ClearedRowsHandler(ISession session)
    {
        _session = session;
    }

    public async Task HandleAsync(AddClearedRowsCount @event, GameEntity entity)
    {
        if (entity.RowsCleared % 10 > @event.Count)
        {
            foreach (var player in entity.Players)
            {
                await _session.Events.AppendAsync(new UpdatePlayerHealth(@event.Id, player.Id, 20));
            }
        }
    }
}
