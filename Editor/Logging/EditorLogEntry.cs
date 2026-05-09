namespace Axe2DEditor.Editor.Logging;

public sealed class EditorLogEntry
{
    public DateTime Time { get; init; } = DateTime.Now;

    public string Level { get; init; } = "Info";

    public string Message { get; init; } = "";

    public override string ToString()
    {
        return $"[{Time:HH:mm:ss}] {Level}: {Message}";
    }
}
