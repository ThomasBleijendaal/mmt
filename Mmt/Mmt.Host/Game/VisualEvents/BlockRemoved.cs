using Mmt.Host.Models;

namespace Mmt.Host.Game.VisualEvents;

public class BlockRemoved : VisualEvent
{
    public required Position[] Positions { get; init; }
}
