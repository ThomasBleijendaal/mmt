using EventCore;

namespace Mmt.Host.Game.Events;

public record ReadyPlayer(Guid Id, Guid PlayerId) : IEvent;
