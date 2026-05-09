namespace Axe2DEditor.Core.Graphs;

public sealed class GraphNodeFieldDefinition
{
    public string Key { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public string LabelKey { get; init; } = string.Empty;

    public string ValueType { get; init; } = NodePortValueTypes.String;

    public bool Editable { get; init; } = true;

    public bool VisibleInScriptModeOnly { get; init; }

    public bool CanConnectInput { get; init; }

    public bool CanConnectOutput { get; init; }

    public string? PortName { get; init; }

    public string? OptionSet { get; init; }
}
