using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<UpdateGameStatus, GameEntity>
{
    public static GameEntity Handle(UpdateGameStatus command, GameEntity current)
    {
        current.Status = command.Status;
        return current;
    }
}
