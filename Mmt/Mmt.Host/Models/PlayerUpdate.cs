namespace Mmt.Host.Models;

public record PlayerUpdate
{
    public required Guid Id { get; init; }

    public required Update Update { get; init; }
}
