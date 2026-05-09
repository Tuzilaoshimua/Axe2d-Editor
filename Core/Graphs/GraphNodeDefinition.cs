namespace Axe2DEditor.Core.Graphs;

public sealed class GraphNodeDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Kind { get; set; } = NodeKinds.Action;

    public string Title { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public Dictionary<string, string> Parameters { get; set; } = [];

    public override string ToString()
    {
        var title = string.IsNullOrWhiteSpace(Title) ? Id : Title;
        return $"{title} [{Kind}]";
    }
}
