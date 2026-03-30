namespace Mmt.Host.Game.VisualEvents;

public abstract class VisualEvent
{
    public required Guid[]? PlayerIds { get; init; }
}
