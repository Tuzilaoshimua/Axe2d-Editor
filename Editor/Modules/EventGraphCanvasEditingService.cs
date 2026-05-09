using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Editor.Modules;

internal static class EventGraphCanvasEditingService
{
    public static void SaveCanvasNode(
        GraphNodeDefinition? selectedCanvasNode,
        string title,
        int x,
        int y,
        string parametersText,
        Action save,
        Action refreshRightPane,
        Action<string?> refreshTriggerList)
    {
        if (selectedCanvasNode is null)
        {
            return;
        }

        selectedCanvasNode.Title = title;
        selectedCanvasNode.X = x;
        selectedCanvasNode.Y = y;
        selectedCanvasNode.Parameters = ParseParameters(parametersText);
        if (selectedCanvasNode.Kind == NodeKinds.Trigger)
        {
            selectedCanvasNode.Parameters["mode"] = "nodeGraph";
        }

        save();
        refreshRightPane();
        refreshTriggerList(selectedCanvasNode.Id);
    }

    public static void SetCanvasLinkSource(
        GraphNodeDefinition? selectedCanvasNode,
        Action<GraphNodeDefinition?> setLinkSourceNode,
        Action refreshNodeGraphPanel)
    {
        if (selectedCanvasNode is null)
        {
            return;
        }

        setLinkSourceNode(selectedCanvasNode);
        refreshNodeGraphPanel();
    }

    public static void ConnectCanvasNodeFromSource(
        LocalizationService localization,
        EventGraphDefinition? selectedGraph,
        GraphNodeDefinition? linkSourceNode,
        string? canvasContextNodeId,
        Func<GraphNodeDefinition, bool, string> inferPortValueType,
        Action<GraphNodeDefinition?> setSelectedCanvasNode,
        Action<string> showWarning,
        Action save,
        Action refreshRightPane)
    {
        if (selectedGraph is null || linkSourceNode is null || string.IsNullOrWhiteSpace(canvasContextNodeId))
        {
            return;
        }

        var targetNode = selectedGraph.Nodes.FirstOrDefault(node => node.Id == canvasContextNodeId);
        if (targetNode is null)
        {
            return;
        }

        if (linkSourceNode.Id == targetNode.Id)
        {
            showWarning(localization.T("graph.error.sameNode"));
            return;
        }

        var valueType = inferPortValueType(linkSourceNode, true);
        var targetType = inferPortValueType(targetNode, false);
        if (!string.Equals(valueType, targetType, StringComparison.OrdinalIgnoreCase))
        {
            showWarning(localization.Format("graph.error.portTypeMismatch", valueType));
            return;
        }

        if (selectedGraph.Edges.Any(edge => edge.FromNodeId == linkSourceNode.Id && edge.ToNodeId == targetNode.Id && edge.FromPort == "flowOut" && edge.ToPort == "flowIn"))
        {
            return;
        }

        selectedGraph.Edges.Add(new GraphEdgeDefinition
        {
            FromNodeId = linkSourceNode.Id,
            ToNodeId = targetNode.Id,
            FromPort = "flowOut",
            ToPort = "flowIn",
            ValueType = valueType
        });

        setSelectedCanvasNode(targetNode);
        save();
        refreshRightPane();
    }

    public static void DeleteCanvasNode(
        LocalizationService localization,
        EventGraphDefinition? selectedGraph,
        GraphNodeDefinition? selectedTrigger,
        string? canvasContextNodeId,
        ref GraphNodeDefinition? selectedCanvasNode,
        ref GraphNodeDefinition? linkSourceNode,
        Action<string> showWarning,
        Action save,
        Action refreshRightPane)
    {
        if (selectedGraph is null || string.IsNullOrWhiteSpace(canvasContextNodeId))
        {
            return;
        }

        if (string.Equals(canvasContextNodeId, selectedTrigger?.Id, StringComparison.OrdinalIgnoreCase))
        {
            showWarning(localization.T("graph.error.deleteTriggerInNodeMode"));
            return;
        }

        selectedGraph.Nodes.RemoveAll(node => node.Id == canvasContextNodeId);
        selectedGraph.Edges.RemoveAll(edge => edge.FromNodeId == canvasContextNodeId || edge.ToNodeId == canvasContextNodeId);
        if (selectedCanvasNode?.Id == canvasContextNodeId)
        {
            selectedCanvasNode = selectedTrigger;
        }
        if (linkSourceNode?.Id == canvasContextNodeId)
        {
            linkSourceNode = null;
        }
        save();
        refreshRightPane();
    }

    private static Dictionary<string, string> ParseParameters(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var idx = line.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }
}
