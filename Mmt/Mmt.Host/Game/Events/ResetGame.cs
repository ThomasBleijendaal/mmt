using EventCore;

namespace Mmt.Host.Game.Events;

public record ResetGame(Guid Id) : IEvent;
