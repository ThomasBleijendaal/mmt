namespace Mmt.Host.Game.AudioEvents;

public class AudioEvent
{
    public required Guid[]? PlayerIds { get; init; }

    public required AudioType Type { get; init; }
}
