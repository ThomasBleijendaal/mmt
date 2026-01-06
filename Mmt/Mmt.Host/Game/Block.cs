namespace Mmt.Host.Game;

public record struct Block(string? Color = null)
{
    public bool IsEmpty => string.IsNullOrEmpty(Color);
}
