namespace Mmt.Host.Models;

public record PlayerStateUpdate : Update
{
    public required int[][] CurrentBlock { get; init; }

    public required bool BlockPlaced { get; init; }
}
