namespace Mmt.Host.Game;

public record NetworkGameState
{
    public required Block[][] BlockState { get; init; }

    public required NetworkPlayer[] Players { get; init; }

    public required int RowsCleared { get; init; }

    public record NetworkPlayer
    {
        public required string Name { get; init; }

        public required string Color { get; init; }

        public required int Health { get; init; }
    }
}
