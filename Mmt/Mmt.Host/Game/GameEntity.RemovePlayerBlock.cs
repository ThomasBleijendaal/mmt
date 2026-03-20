using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<RemovePlayerBlock, GameEntity>
{
    public static GameEntity Handle(RemovePlayerBlock command, GameEntity current)
    {
        var player = current.Players.SingleOrDefault(x => x.Id == command.PlayerId);
        player?.CurrentBlock = null;
        player?.CenterPosition = null;
        return current;
    }
}
