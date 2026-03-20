using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<UpdatePlayerBlockPosition, GameEntity>
{
    public static GameEntity Handle(UpdatePlayerBlockPosition command, GameEntity current)
    {
        var player = current.Players.SingleOrDefault(x => x.Id == command.PlayerId);
        player?.CurrentBlock = command.Positions;
        player?.CenterPosition = command.Center;
        return current;
    }
}
