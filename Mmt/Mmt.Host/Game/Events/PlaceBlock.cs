using EventCore;
using Mmt.Host.Models;

namespace Mmt.Host.Game.Events;

public record PlaceBlock(Guid Id, Guid PlayerId, Position[] Positions) : IEvent;
