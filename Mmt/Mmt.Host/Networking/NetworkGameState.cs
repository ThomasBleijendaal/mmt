using Mmt.Host.Game;
using Mmt.Host.Models;

namespace Mmt.Host.Networking;

public record NetworkGameState
{
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

    public static NetworkGameState Map(GameEntity entity, Guid playerId)
    {
        var players = entity.Players;

        var result = entity.Field.Select(r => r.ToArray()).ToArray();

        foreach (var player in players)
        {
            if (player.CurrentBlock != null && player.Id != playerId)
            {
                foreach (var (x, y) in player.CurrentBlock)
                {
                    if (x < 0 || x >= entity.Field[0].Count ||
                        y < 0 || y >= entity.Field.Count)
                    {
                        continue;
                    }

                    var field = result[y][x];
                    result[y][x] = field with { Color = player.Color };
                }
            }
        }

        return new NetworkGameState
        {
            BlockState = [.. result.Select(r => r.Select(MapBlock).ToArray())],
            RowsCleared = entity.RowsCleared,
            Players = [.. players.Select(p => new NetworkPlayer
            {
                Id = p.Id,
                Color = p.Color,
                Name = p.Name,
                Health = p.Health,
                IsDead = p.IsDead,
                Ready = p.Ready,
                CenterPosition = p.CenterPosition
            })],
            TileSize = entity.TileSize,
            Status = entity.Status.ToString()
        };
    }

    private static readonly Dictionary<string, NetworkBlock> BlockCache = new();

    private static NetworkBlock MapBlock(Block block)
    {
        if (block.Color == null)
        {
            return NetworkBlock.NullBlock;
        }
        else if (BlockCache.TryGetValue(block.Color, out var value))
        {
            return value;
        }
        else
        {
            return BlockCache[block.Color] = new() { Color = block.Color };
        }
    }
}
