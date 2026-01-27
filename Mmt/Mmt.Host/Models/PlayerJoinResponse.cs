namespace Mmt.Host.Models;

public record PlayerJoinResponse
{
    public required Guid GameId { get; init; }
    public required Guid PlayerId { get; init; }
}
