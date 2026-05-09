namespace Axe2DEditor.Core.Graphs;

public sealed class GraphNodeKindDefinition
{
    public string Kind { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string DisplayNameKey { get; init; } = string.Empty;

    public string AccentColor { get; init; } = "5e6cff";

    public IReadOnlyList<NodePortDefinition> Inputs { get; init; } = [];

    public IReadOnlyList<NodePortDefinition> Outputs { get; init; } = [];

    public IReadOnlyList<GraphNodeFieldDefinition> Fields { get; init; } = [];
}
