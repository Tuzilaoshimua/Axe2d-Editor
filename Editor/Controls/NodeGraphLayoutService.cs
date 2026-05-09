using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Editor.Localization;
using System.Windows.Forms;

namespace Axe2DEditor.Editor.Controls;

internal static class NodeGraphLayoutService
{
    public static NodeLayoutInfo BuildLayout(LocalizationService localization, Font font, GraphNodeDefinition node)
    {
        var definition = GraphNodeCatalog.GetDefinition(node.Kind);
        var fields = GraphNodeCatalog.GetVisibleFields(node).ToList();
        var flowInputs = definition.Inputs.Where(port => string.Equals(port.ValueType, NodePortValueTypes.Flow, StringComparison.OrdinalIgnoreCase)).ToList();
        var flowOutputs = definition.Outputs.Where(port => string.Equals(port.ValueType, NodePortValueTypes.Flow, StringComparison.OrdinalIgnoreCase)).ToList();

        var rowCount = Math.Max(1, fields.Count);
        var maxTextWidth = MeasureNodeTextWidth(localization, font, node, definition, fields);
        var width = Math.Clamp(maxTextWidth + 72, NodeGraphCanvasStyle.NodeMinWidth, NodeGraphCanvasStyle.NodeMaxWidth);
        var bodyHeight = NodeGraphCanvasStyle.NodeBodyPadding * 2 + rowCount * NodeGraphCanvasStyle.NodeRowHeight;
        var height = NodeGraphCanvasStyle.NodeHeaderHeight + bodyHeight;

        var bounds = new RectangleF(node.X, node.Y, width, height);
        var flowInputLayouts = new List<NodeSocketHit>();
        var flowOutputLayouts = new List<NodeSocketHit>();
        var fieldLayouts = new List<NodeFieldHit>();

        var flowY = node.Y + (NodeGraphCanvasStyle.NodeHeaderHeight / 2f) - NodeGraphCanvasStyle.SocketRadius;
        if (flowInputs.Count > 0)
        {
            flowInputLayouts.Add(new NodeSocketHit(node.Id, flowInputs[0].Name, flowInputs[0].ValueType, true, false, new RectangleF(node.X + 8, flowY, NodeGraphCanvasStyle.SocketRadius * 2, NodeGraphCanvasStyle.SocketRadius * 2)));
        }

        if (flowOutputs.Count > 0)
        {
            flowOutputLayouts.Add(new NodeSocketHit(node.Id, flowOutputs[0].Name, flowOutputs[0].ValueType, false, true, new RectangleF(node.X + width - 18, flowY, NodeGraphCanvasStyle.SocketRadius * 2, NodeGraphCanvasStyle.SocketRadius * 2)));
        }

        for (var i = 0; i < rowCount; i++)
        {
            var rowTop = NodeGraphCanvasStyle.NodeHeaderHeight + NodeGraphCanvasStyle.NodeBodyPadding + i * NodeGraphCanvasStyle.NodeRowHeight;
            var field = fields[i];
            var rawValue = GetParameter(node.Parameters, field.Key, string.Empty);
            var displayValue = GraphNodeCatalog.GetFieldDisplayValue(localization, node.Kind, field.Key, rawValue);
            var hasLeftSocket = field.CanConnectInput;
            var hasRightSocket = field.CanConnectOutput;
            var fieldRect = new RectangleF(node.X + NodeGraphCanvasStyle.NodeBodyPadding + 2, node.Y + rowTop + 1, width - NodeGraphCanvasStyle.NodeBodyPadding * 2 - 4, NodeGraphCanvasStyle.NodeRowHeight - 2);
            fieldLayouts.Add(new NodeFieldHit(node.Id, field.PortName ?? field.Key, field.ValueType, field.Key, field.Label, displayValue, field.Editable, hasLeftSocket, hasRightSocket, fieldRect));
        }

        return new NodeLayoutInfo(node, definition.DisplayName, definition.AccentColor, bounds, flowInputLayouts, flowOutputLayouts, fieldLayouts);
    }

    private static int MeasureNodeTextWidth(LocalizationService localization, Font font, GraphNodeDefinition node, GraphNodeKindDefinition definition, IReadOnlyList<GraphNodeFieldDefinition> fields)
    {
        var maxWidth = TextRenderer.MeasureText(definition.DisplayName, font, Size.Empty, TextFormatFlags.NoPadding).Width;
        var title = string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title;
        maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(title, font, Size.Empty, TextFormatFlags.NoPadding).Width + 24);

        foreach (var field in fields)
        {
            var displayValue = GraphNodeCatalog.GetFieldDisplayValue(localization, node.Kind, field.Key, GetParameter(node.Parameters, field.Key, string.Empty));
            var text = string.IsNullOrWhiteSpace(displayValue) ? $"{field.Label}" : $"{field.Label} {displayValue}";
            maxWidth = Math.Max(maxWidth, TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding).Width + 40);
        }

        return Math.Min(NodeGraphCanvasStyle.NodeMaxWidth, Math.Max(NodeGraphCanvasStyle.NodeMinWidth, maxWidth));
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }
}

internal sealed record NodeLayoutInfo(
    GraphNodeDefinition Node,
    string DisplayName,
    string AccentColor,
    RectangleF Bounds,
    IReadOnlyList<NodeSocketHit> Inputs,
    IReadOnlyList<NodeSocketHit> Outputs,
    IReadOnlyList<NodeFieldHit> Fields)
{
    public string NodeId => Node.Id;
}

internal sealed record NodeSocketHit(
    string NodeId,
    string PortName,
    string ValueType,
    bool IsInput,
    bool IsOutput,
    RectangleF Bounds);

internal sealed record NodeFieldHit(
    string NodeId,
    string PortName,
    string ValueType,
    string Key,
    string Label,
    string Value,
    bool Editable,
    bool CanConnectInput,
    bool CanConnectOutput,
    RectangleF Bounds)
{
    public RectangleF LeftSocketBounds => new RectangleF(Bounds.Left + 6, Bounds.Top + (Bounds.Height - NodeGraphCanvasStyle.SocketRadius * 2) / 2f, NodeGraphCanvasStyle.SocketRadius * 2, NodeGraphCanvasStyle.SocketRadius * 2);

    public RectangleF RightSocketBounds => new RectangleF(Bounds.Right - 6 - NodeGraphCanvasStyle.SocketRadius * 2, Bounds.Top + (Bounds.Height - NodeGraphCanvasStyle.SocketRadius * 2) / 2f, NodeGraphCanvasStyle.SocketRadius * 2, NodeGraphCanvasStyle.SocketRadius * 2);
}
