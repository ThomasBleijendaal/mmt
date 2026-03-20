using EventCore;

namespace Mmt.Host.Game.Events;

public record RemoveRows(Guid Id, int[] Rows) : IEvent;
