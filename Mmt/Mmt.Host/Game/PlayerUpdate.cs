namespace Mmt.Host.Game;

public record PlayerUpdate
{
    public required Guid Id { get; init; }

    public required Update Update { get; init; }
}
