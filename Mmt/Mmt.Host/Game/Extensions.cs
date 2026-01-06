namespace Mmt.Host.Game;

public static class Extensions
{
    extension(int[][] input)
    {
        public Position[] ToPositions() => [.. input.Select(d => new Position(d[0], d[1]))];
    }
}
