namespace Mmt.Host.Models;

public record PlayerJoinResponse
{
    public required Guid GameId { get; init; }
    public required Guid NextGameId { get; init; }
    public required Guid PlayerId { get; init; }
    public required string PlayerColor { get; init; }
}
