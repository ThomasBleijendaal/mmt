namespace Mmt.Host.Models;

public record struct Block(Position Position, string? Color = null)
{
    public bool IsEmpty => string.IsNullOrEmpty(Color);
}
