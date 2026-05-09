namespace Axe2DEditor.Core.Graphs;

public sealed class GraphEdgeDefinition
{
    public string FromNodeId { get; set; } = string.Empty;

    public string ToNodeId { get; set; } = string.Empty;

    public string FromPort { get; set; } = "flowOut";

    public string ToPort { get; set; } = "flowIn";

    public string ValueType { get; set; } = NodePortValueTypes.Flow;

    public override string ToString()
    {
        return $"{FromNodeId}:{FromPort} -> {ToNodeId}:{ToPort}";
    }
}
