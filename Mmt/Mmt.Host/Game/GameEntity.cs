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

    public Guid NextGameId { get; private set; } = Guid.NewGuid();

    public ImmutableList<PlayerState> Players { get; private set; } = [];

    public List<List<Block>> Field { get; private set; } = [];
}
