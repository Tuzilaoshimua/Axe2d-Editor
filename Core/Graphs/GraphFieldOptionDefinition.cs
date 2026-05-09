namespace Axe2DEditor.Core.Graphs;

public sealed class GraphFieldOptionDefinition
{
    public string Value { get; init; } = string.Empty;

    public string Text { get; init; } = string.Empty;

    public string TextKey { get; init; } = string.Empty;

    public override string ToString() => Text;
}
