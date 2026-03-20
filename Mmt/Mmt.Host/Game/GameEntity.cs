using System.Collections.Immutable;
using EventCore;
using Mmt.Host.Models;

namespace Mmt.Host.Game;

public partial class GameEntity : IEntity
{
    public Guid Id { get; private set; }

    public int Size { get; private set; }

    public int TileSize { get; private set; }

    public int RowsCleared { get; private set; }

    public GameStatus Status { get; private set; }

    private Guid NextGameId { get; init; } = Guid.NewGuid();

    public ImmutableList<PlayerState> Players { get; private set; } = [];

    public List<List<Block>> Field { get; private set; } = [];

    // TODO: move to separate class
    public NetworkGameState GetNetworkState(Guid playerId)
    {
        var players = Players;

        if (players.Count > 1 && players.Count(p => p.IsDead) == players.Count - 1)
        {
            Status = GameStatus.Finished;
        }

        var result = Field.Select(r => r.ToArray()).ToArray();

        foreach (var player in players)
        {
            if (player.CurrentBlock != null && player.Id != playerId)
            {
                foreach (var (x, y) in player.CurrentBlock)
                {
                    if (x < 0 || x >= Field[0].Count ||
                        y < 0 || y >= Field.Count)
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
            NextGameId = NextGameId,
            BlockState = [.. result.Select(r => r.Select(MapBlock).ToArray())],
            RowsCleared = RowsCleared,
            Players = [.. players.Select(p => new NetworkGameState.NetworkPlayer
            {
                Id = p.Id,
                Color = p.Color,
                Name = p.Name,
                Health = p.Health,
                IsDead = p.IsDead,
                Ready = p.Ready,
                CenterPosition = p.CenterPosition
            })],
            TileSize = TileSize,
            Status = Status.ToString()
        };
    }

    private readonly Dictionary<string, NetworkGameState.NetworkBlock> _blockCache = new();

    private NetworkGameState.NetworkBlock MapBlock(Block block)
    {
        if (block.Color == null)
        {
            return NetworkGameState.NetworkBlock.NullBlock;
        }
        else if (_blockCache.TryGetValue(block.Color, out var value))
        {
            return value;
        }
        else
        {
            return _blockCache[block.Color] = new() { Color = block.Color };
        }
    }
}
