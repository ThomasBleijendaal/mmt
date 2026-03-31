using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<CompressField, GameEntity>
{
    public static GameEntity Handle(CompressField command, GameEntity current)
    {
        current.InvalidBlocksPlaced = 0;
        return current;
    }
}
