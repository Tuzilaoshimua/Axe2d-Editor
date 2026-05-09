using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Editor.Modules;

internal static class EventGraphStructuredEditingService
{
    public static void AddOrEditEvent(
        LocalizationService localization,
        GraphNodeDefinition? selectedTrigger,
        bool editExisting,
        Action save,
        Action refreshRightPane,
        Action<string> refreshTriggerList)
    {
        if (selectedTrigger is null)
        {
            return;
        }

        if (editExisting && string.IsNullOrWhiteSpace(GetParameter(selectedTrigger.Parameters, "event", string.Empty)))
        {
            return;
        }

        using var dialog = new EventDefinitionDialog(localization, selectedTrigger.Parameters);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        dialog.ApplyTo(selectedTrigger.Parameters);
        save();
        refreshRightPane();
        refreshTriggerList(selectedTrigger.Id);
    }

    public static void DeleteEvent(
        GraphNodeDefinition? selectedTrigger,
        Action save,
        Action refreshRightPane)
    {
        if (selectedTrigger is null)
        {
            return;
        }

        foreach (var key in new[] { "event", "subject", "areaSource", "shape", "width", "height", "radius", "once", "runOnce" })
        {
            selectedTrigger.Parameters.Remove(key);
        }

        save();
        refreshRightPane();
    }

    public static void AddStructuredNode(
        LocalizationService localization,
        EventGraphDefinition? selectedGraph,
        GraphNodeDefinition? selectedTrigger,
        string kind,
        Func<int> getNextNodeSequence,
        Action<string> showWarning,
        Action save,
        Action refreshRightPane)
    {
        if (selectedGraph is null || selectedTrigger is null)
        {
            return;
        }

        var view = EventGraphAnalysisService.AnalyzeTrigger(selectedGraph, selectedTrigger);
        if (view.IsComplex)
        {
            showWarning(localization.T("graph.error.complexStructuredEdit"));
            return;
        }

        using var dialog = new TemplateNodeDialog(localization, kind);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var node = new GraphNodeDefinition
        {
            Id = $"node.{getNextNodeSequence()}",
            Kind = kind,
            Title = dialog.NodeTitle,
            X = 320 + (view.Conditions.Count + view.Actions.Count) * 220,
            Y = kind == NodeKinds.Condition ? 70 : 170,
            Parameters = dialog.BuildParameters()
        };
        EventGraphAnalysisService.SetNodeOwner(node, selectedTrigger.Id);

        selectedGraph.Nodes.Add(node);
        if (kind == NodeKinds.Condition)
        {
            view.Conditions.Add(node);
        }
        else
        {
            view.Actions.Add(node);
        }

        EventGraphAnalysisService.RebuildStructuredEdges(selectedGraph, selectedTrigger, view.Conditions, view.Actions);
        save();
        refreshRightPane();
    }

    public static void EditStructuredNode(
        LocalizationService localization,
        EventGraphDefinition? selectedGraph,
        GraphNodeDefinition? selectedTrigger,
        GraphNodeDefinition? selectedNode,
        Action save,
        Action refreshRightPane)
    {
        if (selectedGraph is null || selectedTrigger is null || selectedNode is null)
        {
            return;
        }

        using var dialog = new TemplateNodeDialog(localization, selectedNode.Kind, selectedNode);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        selectedNode.Title = dialog.NodeTitle;
        selectedNode.Parameters = dialog.BuildParameters();
        save();
        refreshRightPane();
    }

    public static void DeleteStructuredNode(
        LocalizationService localization,
        EventGraphDefinition? selectedGraph,
        GraphNodeDefinition? selectedTrigger,
        string kind,
        GraphNodeDefinition? selectedNode,
        Action<string> showWarning,
        Action save,
        Action refreshRightPane)
    {
        if (selectedGraph is null || selectedTrigger is null)
        {
            return;
        }

        var view = EventGraphAnalysisService.AnalyzeTrigger(selectedGraph, selectedTrigger);
        if (view.IsComplex)
        {
            showWarning(localization.T("graph.error.complexStructuredEdit"));
            return;
        }

        if (selectedNode is null)
        {
            return;
        }

        selectedGraph.Nodes.RemoveAll(node => node.Id == selectedNode.Id);
        selectedGraph.Edges.RemoveAll(edge => edge.FromNodeId == selectedNode.Id || edge.ToNodeId == selectedNode.Id);
        view.Conditions.RemoveAll(node => node.Id == selectedNode.Id);
        view.Actions.RemoveAll(node => node.Id == selectedNode.Id);
        EventGraphAnalysisService.RebuildStructuredEdges(selectedGraph, selectedTrigger, view.Conditions, view.Actions);
        save();
        refreshRightPane();
    }

    public static string? MoveStructuredNode(
        LocalizationService localization,
        EventGraphDefinition? selectedGraph,
        GraphNodeDefinition? selectedTrigger,
        string kind,
        int offset,
        GraphNodeDefinition? selectedNode,
        Action<string> showWarning,
        Action save,
        Action refreshRightPane)
    {
        if (selectedGraph is null || selectedTrigger is null)
        {
            return null;
        }

        var view = EventGraphAnalysisService.AnalyzeTrigger(selectedGraph, selectedTrigger);
        if (view.IsComplex)
        {
            showWarning(localization.T("graph.error.complexStructuredEdit"));
            return null;
        }

        var list = kind == NodeKinds.Condition ? view.Conditions : view.Actions;
        if (selectedNode is null)
        {
            return null;
        }

        var index = list.FindIndex(node => node.Id == selectedNode.Id);
        if (index < 0)
        {
            return null;
        }

        var nextIndex = index + offset;
        if (nextIndex < 0 || nextIndex >= list.Count)
        {
            return null;
        }

        (list[index], list[nextIndex]) = (list[nextIndex], list[index]);
        EventGraphAnalysisService.RebuildStructuredEdges(selectedGraph, selectedTrigger, view.Conditions, view.Actions);
        save();
        refreshRightPane();
        return selectedNode.Id;
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }
}
