using EventCore;

namespace Mmt.Host.Game.Events;

public record UpdatePlayerHealth(Guid Id, Guid PlayerId, int Delta) : IEvent;
