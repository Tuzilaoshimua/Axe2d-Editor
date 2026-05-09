namespace Axe2DEditor.Core.Graphs;

public sealed class EventGraphDefinition
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public List<GraphNodeDefinition> Nodes { get; set; } = [];

    public List<GraphEdgeDefinition> Edges { get; set; } = [];

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName;
    }
}
