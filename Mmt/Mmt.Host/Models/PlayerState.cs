namespace Mmt.Host.Models;

public record PlayerState
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Color { get; init; }

    public Position[]? CurrentBlock { get; set; }

    public int Health { get; set; } = 41;

    public bool Ready { get; set; }

    public bool IsDead => Health <= 0;
}
