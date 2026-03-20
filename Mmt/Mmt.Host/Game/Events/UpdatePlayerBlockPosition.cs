using EventCore;
using Mmt.Host.Models;

namespace Mmt.Host.Game.Events;

public record UpdatePlayerBlockPosition(Guid Id, Guid PlayerId, Position[] Positions, Position Center) : IEvent;
