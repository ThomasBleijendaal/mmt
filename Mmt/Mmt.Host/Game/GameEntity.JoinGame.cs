using EventCore;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<JoinGame, GameEntity>
{
    public static GameEntity Handle(JoinGame command, GameEntity current)
    {
        current.Players = current.Players.Add(new PlayerState
        {
            Id = command.PlayerId,
            Name = command.Name,
            Color = ColorHelper.GetColor(current.Players.Count + 1)
        });

        return current;
    }
}
