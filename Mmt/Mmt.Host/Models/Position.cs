namespace Mmt.Host.Models;

public record struct Position(int X, int Y)
{
    public override string ToString() => $"{X}x{Y}";
}
