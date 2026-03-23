using EventCore;
using Mmt.Host.Game.Events;
using ISession = EventCore.ISession;

namespace Mmt.Host.Game.EventHandlers;

public class GameFinishedHandler :
    IEventListener<DropPlayer, GameEntity>,
    IEventListener<UpdatePlayerHealth, GameEntity>
{
    private readonly ISession _session;

    public GameFinishedHandler(ISession session)
    {
        _session = session;
    }

    public Task HandleAsync(UpdatePlayerHealth @event, GameEntity entity) => CheckDeadPlayersAsync(entity);
    public Task HandleAsync(DropPlayer @event, GameEntity entity) => CheckDeadPlayersAsync(entity);

    private async Task CheckDeadPlayersAsync(GameEntity entity)
    {
        var playerCount = entity.Players.Count;
        var deadCount = entity.Players.Count(p => p.IsDead);

        if (deadCount >= playerCount - 1)
        {
            await _session.Events.AppendAsync(new UpdateGameStatus(entity.Id, GameStatus.Finished));
        }
    }
}
