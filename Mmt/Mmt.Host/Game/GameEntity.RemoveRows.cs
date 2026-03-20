using EventCore;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<RemoveRows, GameEntity>
{
    public static GameEntity Handle(RemoveRows command, GameEntity current)
    {
        var delta = command.Rows.Length;

        foreach (var nr in command.Rows)
        {
            current.Field[nr] = current.Field[nr - delta].ToList();
        }

        for (var nr = 0; nr < delta; nr++)
        {
            current.Field[nr] = Enumerable.Repeat(new Block(), current.Field[nr].Count).ToList();
        }

        return current;
    }
}
