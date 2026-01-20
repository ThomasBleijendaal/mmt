using Mmt.Host.Models;

namespace Mmt.Host.Game;

public static class Helper
{
    public static IEnumerable<Position> GetAllPositions(int size)
    {
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                yield return new Position(x, y);
            }
        }
    }
}
