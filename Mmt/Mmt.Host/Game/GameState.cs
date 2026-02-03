using Mmt.Host.Models;

namespace Mmt.Host.Game;

public class GameState
{
    private readonly int _size;

    public GameState(int size)
    {
        Field = CreateField(size);
        _size = size;
    }

    private int RowsCleared { get; set; } = 0;

    private int TileSize { get; set; } = 4;

    public GameStatus Status { get; private set; } = GameStatus.PreGame;

    private Guid NextGameId { get; init; } = Guid.NewGuid();

    private List<List<Block>> Field { get; set; }

    private List<PlayerState> Players { get; init; } = [];

    public int PlayerCount => Players.Count;

    public Guid? AddPlayer(string name, string color)
    {
        if (Players.Any(p => p.Color == color))
        {
            return null;
        }

        var id = Guid.NewGuid();

        Players.Add(new()
        {
            Color = color,
            Name = name,
            Id = id
        });

        HandleBoardSize();

        return id;
    }

    public void PlaceBlock(Guid playerId, Position[] positions)
    {
        var color = Players.FirstOrDefault(x => x.Id == playerId)?.Color;

        var leftoverPosition = positions.Where(p => p.Y > 3).ToArray();

        if (leftoverPosition.Length > 0)
        {
            if (color != null)
            {
                Field.SetColor(leftoverPosition, color);
            }

            HandleCompleteRows();
        }
        else
        {
            Players.FirstOrDefault(x => x.Id == playerId)?.Health -= 3;
        }
    }

    public void UpdateCurrentBlockOfPlayer(Guid playerId, Position[] positions, Position center)
    {
        var playerState = Players.FirstOrDefault(x => x.Id == playerId);
        playerState?.CurrentBlock = positions;
        playerState?.CenterPosition = center;
    }

    public void RemoveCurrentBlockFromPlayer(Guid playerId)
    {
        var playerState = Players.FirstOrDefault(x => x.Id == playerId);
        playerState?.CurrentBlock = null;
        playerState?.CenterPosition = null;
    }

    public void ReadyPlayer(Guid id)
    {
        Players.FirstOrDefault(x => x.Id == id)?.Ready = true;

        if (Players.All(x => x.Ready) && Players.Count > 1)
        {
            Status = GameStatus.Running;
        }
    }

    public void DropPlayer(Guid playerId)
    {
        Players.RemoveAll(p => p.Id == playerId);
        HandleBoardSize();
    }

    public void Reset()
    {
        Field = CreateField(_size);
        Players.Clear();
        Status = GameStatus.PreGame;
    }

    public NetworkGameState GetNetworkState(Guid playerId)
    {
        if (Players.Count > 1 && Players.Count(p => p.IsDead) == Players.Count - 1)
        {
            Status = GameStatus.Finished;
        }

        var result = Field.Select(r => r.ToArray()).ToArray();

        foreach (var player in Players)
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
            BlockState = [.. result.Select(r => r.Select(b => new NetworkGameState.NetworkBlock
            {
                Color = b.Color
            }).ToArray())],
            RowsCleared = RowsCleared,
            Players = [.. Players.Select(p => new NetworkGameState.NetworkPlayer
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

    private void HandleCompleteRows()
    {
        var width = _size / TileSize;
        for (var r = Field.Count - 1; r >= 0; r--)
        {
            if (Field[r].Take(width).All(x => !x.IsEmpty))
            {
                var rowsComplete = 1;
                while (rowsComplete <= r && Field[r - rowsComplete].Take(width).All(b => !b.IsEmpty))
                {
                    rowsComplete++;
                }

                for (var i = 0; i < rowsComplete; i++)
                {
                    RowsCleared++;

                    if (RowsCleared % 10 == 0)
                    {
                        Players.ForEach(x => x.Health = Math.Min(100, x.Health + 20));
                    }
                }

                var filledBlocks = Field.Skip(r - rowsComplete + 1).Take(rowsComplete).SelectMany(x => x).ToArray();
                var totalBlocks = (double)filledBlocks.Length;

                var percentages = filledBlocks
                    .GroupBy(x => x.Color)
                    .Select(g => (color: g.Key, percentage: g.Count() / totalBlocks))
                    .OrderByDescending(d => d.percentage)
                    .ToArray();

                var maxPercentage = percentages.Max(x => x.percentage);
                var updateBoardSize = false;

                var colorsInBlocks = percentages.Select(x => x.color).ToArray();

                foreach (var (color, percentage) in percentages)
                {
                    var damage = percentage switch
                    {
                        _ when percentage < maxPercentage / 4.0 => 3,
                        _ when percentage < maxPercentage / 3.0 => 2,
                        _ when percentage < maxPercentage / 2.0 => 1,
                        _ => 0
                    };

                    Players.FirstOrDefault(x => x.Color == color)?.Health -= damage;
                    updateBoardSize = true;
                }

                var playersNotInBlocks = Players.Except(Players.Where(p => colorsInBlocks.Contains(p.Color)));
                foreach (var player in playersNotInBlocks)
                {
                    player.Health -= 20;
                    updateBoardSize = true;
                }

                for (var nr = r; nr >= rowsComplete; nr--)
                {
                    Field[nr] = Field[nr - rowsComplete].ToList();
                }

                for (var nr = 0; nr <= rowsComplete; nr++)
                {
                    Field[nr] = Enumerable.Repeat(new Block(), Field[nr].Count).ToList();
                }

                if (updateBoardSize)
                {
                    HandleBoardSize();
                }
            }
        }
    }

    private void HandleBoardSize()
    {
        var requiredTileSize = Players.Count(p => p.Health > 0) switch
        {
            var x when x > 8 => 1,
            var x when x > 4 => 2,
            var x when x > 2 => 3,
            _ => 4
        };

        if (requiredTileSize == TileSize)
        {
            return;
        }

        var scale = (double)requiredTileSize / TileSize;
        var blocksInScale = Math.Ceiling((double)requiredTileSize / TileSize);

        try
        {
            if (requiredTileSize < TileSize)
            {
                Field.SetColor(Helper.GetAllPositions(_size), null);

                return;
            }

            var copiedField = Field.Select(r => r.ToList()).ToList();

            foreach (var block in copiedField.Blocks)
            {
                var blocks =
                    GetPositions(block.Position)
                    .Select(p => Field.ElementAtOrDefault(p.Y)?.ElementAtOrDefault(p.X))
                    .OfType<Block>()
                    .ToArray();

                if (blocks.Length > 0)
                {
                    var blockGroups = blocks
                        .GroupBy(x => x.Color)
                        .Select(g => (color: g.Key, count: g.Count()))
                        .OrderByDescending(x => x.count)
                        .ToArray();

                    if (blockGroups[0].color is not null)
                    {
                        Field.SetColor(block.Position, blockGroups[0].color);
                        continue;
                    }
                    else if (blockGroups.Length > 1 && blockGroups[0].count < blockGroups[1].count * 3)
                    {
                        Field.SetColor(block.Position, blockGroups[1].color);
                        continue;
                    }
                }

                Field.SetColor(block.Position, null);
            }
        }
        finally
        {
            TileSize = requiredTileSize;
        }

        IEnumerable<Position> GetPositions(Position position)
        {
            /*
             * scale = 1/2
             * x = 10
             * y = 15
             * 
             * x = 5 -> (10 + 11)
             * y = 7 -> (7 + 8)
             * 
             * scale = 2/3
             * x = 11
             * y = 18
             * 
             * x = 7 -> (10 + 11)
             * y = 12 -> (18)
             */

            var minX = position.X * scale;
            var maxX = (int)Math.Ceiling(minX + blocksInScale);

            var minY = position.Y * scale;
            var maxY = (int)Math.Ceiling(minY + blocksInScale);

            for (var x = (int)minX; x < maxX; x++)
            {
                for (var y = (int)minY; y < maxY; y++)
                {
                    yield return new(x, y);
                }
            }
        }
    }

    private static List<List<Block>> CreateField(int size) => [.. Enumerable.Range(0, size).Select(y => Enumerable.Range(0, size).Select(x => new Block(new(x, y))).ToList())];
}
