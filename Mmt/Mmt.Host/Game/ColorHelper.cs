namespace Mmt.Host.Game;

internal static class ColorHelper
{
    public static string GetColor(int seed)
    {
        var r = (byte)((seed * 23) % 16);
        var g = (byte)((seed * 27) % 16);
        var b = (byte)((seed * 31) % 16);

        return $"#{r:X1}{g:X1}{b:X1}".ToLower();
    }
}
