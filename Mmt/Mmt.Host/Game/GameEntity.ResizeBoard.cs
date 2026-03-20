using EventCore;
using Mmt.Host.Game.Events;
using Mmt.Host.Models;

namespace Mmt.Host.Game;

public partial class GameEntity : IHandles<ResizeBoard, GameEntity>
{
    public static GameEntity Handle(ResizeBoard command, GameEntity current)
    {
        if (command.TileSize == current.TileSize)
        {
            return current;
        }

        var scale = (double)command.TileSize / current.TileSize;
        var blocksInScale = Math.Ceiling((double)command.TileSize / current.TileSize);

        try
        {
            if (scale < 1)
            {
                current.Field.SetColor(Helper.GetAllPositions(current.Size), null);

                return current;
            }

            var copiedField = current.Field.Select(r => r.ToList()).ToList();

            foreach (var block in copiedField.Blocks)
            {
                var blocks =
                    GetPositions(block.Position)
                    .Select(p => current.Field.ElementAtOrDefault(p.Y)?.ElementAtOrDefault(p.X))
                    .OfType<Block>()
                    .ToArray();

                if (blocks.Length > 0)
                {
                    var blockGroups = blocks
                        .GroupBy(x => x.Color)
                        .Select(g => (color: g.Key, count: g.Count()))
                        .OrderByDescending(x => x.count)
                        .ToArray();

                    if (blockGroups[0].color is not null)
                    {
                        current.Field.SetColor(block.Position, blockGroups[0].color);
                        continue;
                    }
                    else if (blockGroups.Length > 1 && blockGroups[0].count < blockGroups[1].count * 3)
                    {
                        current.Field.SetColor(block.Position, blockGroups[1].color);
                        continue;
                    }
                }

                current.Field.SetColor(block.Position, null);
            }
        }
        finally
        {
            current.TileSize = command.TileSize;
        }

        return current;

        IEnumerable<Position> GetPositions(Position position)
        {
            /*
             * scale = 1/2
             * x = 10
             * y = 15
             * 
             * x = 5 -> (10 + 11)
             * y = 7 -> (7 + 8)
             * 
             * scale = 2/3
             * x = 11
             * y = 18
             * 
             * x = 7 -> (10 + 11)
             * y = 12 -> (18)
             */

            var minX = position.X * scale;
            var maxX = (int)Math.Ceiling(minX + blocksInScale);

            var minY = position.Y * scale;
            var maxY = (int)Math.Ceiling(minY + blocksInScale);

            for (var x = (int)minX; x < maxX; x++)
            {
                for (var y = (int)minY; y < maxY; y++)
                {
                    yield return new(x, y);
                }
            }
        }
    }
}
