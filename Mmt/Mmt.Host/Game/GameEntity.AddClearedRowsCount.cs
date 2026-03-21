using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<AddClearedRowsCount, GameEntity>
{
    public static GameEntity Handle(AddClearedRowsCount command, GameEntity current)
    {
        current.RowsCleared += command.Count;
        return current;
    }
}
