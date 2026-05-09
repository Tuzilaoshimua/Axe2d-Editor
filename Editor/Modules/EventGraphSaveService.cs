using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Core.Projects;

namespace Axe2DEditor.Editor.Modules;

internal static class EventGraphSaveService
{
    public static void SaveProject(
        ProjectContext context,
        ProjectService projectService)
    {
        projectService.SaveProject(context);
    }

    public static string? ValidateGraph(EventGraphDefinition graph, Func<string, string> localize, Func<string, string, string> format)
    {
        foreach (var edge in graph.Edges)
        {
            var fromNode = graph.Nodes.FirstOrDefault(node => node.Id == edge.FromNodeId);
            var toNode = graph.Nodes.FirstOrDefault(node => node.Id == edge.ToNodeId);
            if (fromNode is null || toNode is null)
            {
                return localize("graph.error.missingNode");
            }

            if (!EventGraphAnalysisService.IsEdgePortCompatible(edge))
            {
                return format("graph.error.portTypeMismatch", edge.ValueType);
            }

            if (toNode.Kind == NodeKinds.Trigger)
            {
                return localize("graph.error.triggerIncoming");
            }
        }

        foreach (var node in graph.Nodes.Where(node => node.Kind is NodeKinds.Condition or NodeKinds.Action))
        {
            if (!graph.Edges.Any(edge => edge.ToNodeId == node.Id))
            {
                var nodeName = string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title;
                return format("graph.error.requireIncoming", nodeName);
            }
        }

        return null;
    }
}
