namespace Mmt.Host.Game;

public class GameState
{
    public GameState(int size)
    {
        Field = [.. Enumerable.Range(0, size).Select(_ => Enumerable.Repeat(new Block(), size).ToList())];
    }

    private int RowsCleared { get; set; } = 0;

    private List<List<Block>> Field { get; init; }

    private List<PlayerState> Players { get; init; } = [];

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

        return id;
    }

    public void PlaceBlock(Guid playerId, Position[] positions)
    {
        var color = Players.FirstOrDefault(x => x.Id == playerId)?.Color;
        if (color != null)
        {
            foreach (var (x, y) in positions)
            {
                var field = Field[y][x];
                Field[y][x] = field with { Color = color };
            }
        }

        HandleCompleteRows();
    }

    public void UpdateCurrentBlockOfPlayer(Guid playerId, Position[] positions)
    {
        Players.FirstOrDefault(x => x.Id == playerId)?.CurrentBlock = positions;
    }

    public void RemoveCurrentBlockFromPlayer(Guid playerId)
    {
        Players.FirstOrDefault(x => x.Id == playerId)?.CurrentBlock = null;
    }

    public void DropPlayer(Guid id)
    {
        Players.RemoveAll(p => p.Id == id);
    }

    public NetworkGameState GetNetworkState(Guid playerId)
    {
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
            BlockState = result,
            RowsCleared = RowsCleared,
            Players = Players.Select(p => new NetworkGameState.NetworkPlayer
            {
                Color = p.Color,
                Name = p.Name,
                Health = p.Health
            }).ToArray()
        };
    }

    private void HandleCompleteRows()
    {
        for (var r = Field.Count - 1; r >= 0; r--)
        {
            if (Field[r].TrueForAll(x => !x.IsEmpty))
            {
                var rowsComplete = 1;
                while (rowsComplete <= r && Field[r - rowsComplete].TrueForAll(b => !b.IsEmpty))
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

                var colorsInBlocks = percentages.Select(x => x.color).ToArray();

                foreach (var (color, percentage) in percentages)
                {
                    // TODO: this needs some balancing on percentage of percentages
                    var damage = percentage switch
                    {
                        _ when percentage < 0.1 => 15,
                        _ when percentage < 0.25 => 10,
                        _ when percentage < 0.5 => 5,
                        _ => 0
                    };

                    Players.FirstOrDefault(x => x.Color == color)?.Health -= damage;
                }

                var playersNotInBlocks = Players.Except(Players.Where(p => colorsInBlocks.Contains(p.Color)));
                foreach (var player in playersNotInBlocks)
                {
                    player.Health -= 20;
                }

                for (var nr = r; nr >= rowsComplete; nr--)
                {
                    Field[nr] = Field[nr - rowsComplete].ToList();
                }

                for (var nr = 0; nr <= rowsComplete; nr++)
                {
                    Field[nr] = Enumerable.Repeat(new Block(), Field[nr].Count).ToList();
                }
            }
        }
    }
}
