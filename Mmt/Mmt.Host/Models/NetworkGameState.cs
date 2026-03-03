namespace Mmt.Host.Models;

public record NetworkGameState
{
    public required Guid NextGameId { get; init; }

    public required NetworkBlock[][] BlockState { get; init; }

    public required NetworkPlayer[] Players { get; init; }

    public required int RowsCleared { get; init; }

    public required int TileSize { get; init; }

    public required string Status { get; init; }

    public record NetworkBlock
    {
        public static readonly NetworkBlock NullBlock = new() { Color = null };

        public required string? Color { get; init; }
    }

    public record NetworkPlayer
    {
        public required Guid Id { get; init; }

        public required string Name { get; init; }

        public required string Color { get; init; }

        public required int Health { get; init; }

        public required bool Ready { get; init; }

        public required bool IsDead { get; init; }

        public required Position? CenterPosition { get; init; }
    }
}
