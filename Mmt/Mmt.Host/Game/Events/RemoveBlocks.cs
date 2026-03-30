using EventCore;
using Mmt.Host.Models;

namespace Mmt.Host.Game.Events;

public record RemoveBlocks(Guid Id, Position[] Blocks) : IEvent;
