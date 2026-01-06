namespace Mmt.Host.Game;

public record PlayerStateUpdate : Update
{
    public required int[][] CurrentBlock { get; init; }

    public required bool BlockPlaced { get; init; }
}
