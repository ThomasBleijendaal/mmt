using EventCore;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;

namespace Mmt.Host.Game;

public partial class GameEntity : IStartsWith<StartGame, GameEntity>
{
    public static GameEntity Create(StartGame command) => new()
    {
        Id = command.Id,
        Size = command.Size,
        TileSize = 4,
        Status = GameStatus.PreGame,
        Field = CreateField(command.Size)
    };

    private static List<List<Block>> CreateField(int size) => [.. Enumerable.Range(0, size).Select(y => Enumerable.Range(0, size).Select(x => new Block(new(x, y))).ToList())];
}