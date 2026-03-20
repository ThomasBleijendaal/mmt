using EventCore;

namespace Mmt.Host.Game.Events;

public record JoinGame(Guid Id, Guid PlayerId, string Name) : IEvent;
