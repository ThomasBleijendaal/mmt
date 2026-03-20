using EventCore;

namespace Mmt.Host.Game.Events;

public record RemovePlayerBlock(Guid Id, Guid PlayerId) : IEvent;
