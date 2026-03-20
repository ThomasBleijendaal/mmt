using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<ReadyPlayer, GameEntity>
{
    public static GameEntity Handle(ReadyPlayer command, GameEntity current)
    {
        current.Players.SingleOrDefault(p => p.Id == command.PlayerId)?.Ready = true;
        return current;
    }
}
