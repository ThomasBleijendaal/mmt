using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<UpdatePlayerHealth, GameEntity>
{
    public static GameEntity Handle(UpdatePlayerHealth command, GameEntity current)
    {
        var player = current.Players.SingleOrDefault(x => x.Id == command.PlayerId);
        player?.Health = Math.Min(100, player.Health + command.Delta);
        return current;
    }
}
