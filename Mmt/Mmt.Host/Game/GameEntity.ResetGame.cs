using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<ResetGame, GameEntity>
{
    public static GameEntity Handle(ResetGame command, GameEntity current)
    {
        current.Players.Clear();
        current.Field = CreateField(current.Size);
        current.Status = GameStatus.PreGame;

        return current;
    }
}
