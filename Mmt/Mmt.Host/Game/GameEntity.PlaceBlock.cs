using EventCore;
using Mmt.Host.Game.Events;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<PlaceBlock, GameEntity>
{
    public static GameEntity Handle(PlaceBlock command, GameEntity current)
    {
        var color = current.Players.SingleOrDefault(p => p.Id == command.PlayerId)?.Color;
        var leftoverPosition = command.Positions.Where(p => p.Y > 3).ToArray();

        if (leftoverPosition.Length > 0 && color != null)
        {
            current.Field.SetColor(leftoverPosition, color);
        }

        return current;
    }
}
