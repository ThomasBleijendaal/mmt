using EventCore;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<RemoveBlocks, GameEntity>
{
    public static GameEntity Handle(RemoveBlocks command, GameEntity current)
    {
        var size = current.Size / current.TileSize;

        var completeRows = command.Blocks.GroupBy(x => x.Y).Where(r => r.Count() == size).ToArray();
        var itemsInCompleteRows = completeRows.SelectMany(x => x).ToArray();

        // remove separate ones first, before removing entire rows and shifting everything
        current.Field.SetColor(command.Blocks.Except(itemsInCompleteRows), null);

        foreach (var completeRow in completeRows)
        {
            RemoveRow(current, completeRow.Key);
        }

        return current;
    }

    private static void RemoveRow(GameEntity current, int row)
    {
        current.RowsCleared += 1;

        for (var nr = row; nr > 1; nr--)
        {
            current.Field[nr] = current.Field[nr - 1].ToList();
        }

        current.Field[0] = Enumerable.Repeat(new Block(), current.Field[0].Count).ToList();
    }
}
