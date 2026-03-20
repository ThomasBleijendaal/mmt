using EventCore;

namespace Mmt.Host.Game.Events;

public record ResizeBoard(Guid Id, int TileSize) : IEvent;
