namespace Mmt.Host.Models;

public record NetworkGameState
{
    public required Block[][] BlockState { get; init; }

    public required NetworkPlayer[] Players { get; init; }

    public required int RowsCleared { get; init; }

    public required int TileSize { get; init; }

    public required string Status { get; init; }

    public record NetworkPlayer
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required string Color { get; init; }

        public required int Health { get; init; }

        public required bool Ready { get; init; }

        public required bool IsDead { get; init; }
    }
}
