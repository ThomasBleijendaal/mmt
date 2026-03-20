using EventCore;

namespace Mmt.Host.Game.Events;

public record DropPlayer(Guid Id, Guid PlayerId) : IEvent;
