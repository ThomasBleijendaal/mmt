using EventCore;

namespace Mmt.Host.Game.Events;

public record StartGame(Guid Id, int Size) : ICreateEvent
{
    public Guid NextGameId { get; init; } = Guid.NewGuid();
}
