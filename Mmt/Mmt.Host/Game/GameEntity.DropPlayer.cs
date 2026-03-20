using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<DropPlayer, GameEntity>
{
    public static GameEntity Handle(DropPlayer command, GameEntity current)
    {
        current.Players = current.Players.RemoveAll(p => p.Id == command.PlayerId);
        return current;
    }
}
