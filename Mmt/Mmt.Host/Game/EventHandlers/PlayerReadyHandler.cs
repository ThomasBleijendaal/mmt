using EventCore;
using Mmt.Host.Game.Events;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class PlayerReadyHandler : IEventListener<ReadyPlayer, GameEntity>
{
    private readonly ISession _session;

    public PlayerReadyHandler(ISession session)
    {
        _session = session;
    }

    public async Task HandleAsync(ReadyPlayer @event, GameEntity entity)
    {
        if (entity.Status != GameStatus.PreGame)
        {
            return;
        }

        if (entity.Players.Count > 1 && entity.Players.All(x => x.Ready))
        {
            await _session.Events.AppendAsync(new UpdateGameStatus(@event.Id, GameStatus.Running));
        }
    }
}
