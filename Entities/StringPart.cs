namespace Xorog.Logger;

internal class StringPart : IDisposable
{
    internal string String { get; set; }
    internal ConsoleColor? Color { get; set; }

    public void Dispose()
    {
        String = "";
        Color = null;
    }
}
