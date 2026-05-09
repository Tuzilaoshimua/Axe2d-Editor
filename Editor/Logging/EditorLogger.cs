namespace Axe2DEditor.Editor.Logging;

public sealed class EditorLogger
{
    private readonly List<EditorLogEntry> _entries = [];

    public event EventHandler<EditorLogEntry>? EntryAdded;

    public IReadOnlyList<EditorLogEntry> Entries => _entries;

    public void Info(string message)
    {
        Add("Info", message);
    }

    public void Warning(string message)
    {
        Add("Warning", message);
    }

    public void Error(string message)
    {
        Add("Error", message);
    }

    private void Add(string level, string message)
    {
        var entry = new EditorLogEntry
        {
            Level = level,
            Message = message
        };

        _entries.Add(entry);
        EntryAdded?.Invoke(this, entry);
    }
}
