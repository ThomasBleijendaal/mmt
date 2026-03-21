using EventCore;

namespace Mmt.Host.Game.Events;

public record AddClearedRowsCount(Guid Id, int Count) : IEvent;
