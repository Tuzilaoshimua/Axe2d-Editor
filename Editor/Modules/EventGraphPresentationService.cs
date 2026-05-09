using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Editor.Modules;

internal static class EventGraphPresentationService
{
    public static Dictionary<string, string> CreateDefaultTriggerParameters(string mode, string eventType = "", string subject = "")
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["event"] = eventType,
            ["subject"] = subject,
            ["once"] = "false",
            ["runOnce"] = "false",
            ["enabled"] = "true",
            ["mode"] = mode,
            ["hasParallel"] = "false"
        };
    }

    public static string SummarizeTriggerEvent(LocalizationService localization, Dictionary<string, string> parameters)
    {
        var eventType = GetParameter(parameters, "event", string.Empty);
        if (string.IsNullOrWhiteSpace(eventType))
        {
            return localization.T("graph.summary.noEvent");
        }

        var eventText = GraphNodeCatalog.GetFieldDisplayValue(localization, NodeKinds.Trigger, "event", eventType);
        var subjectText = GraphNodeCatalog.GetFieldDisplayValue(localization, NodeKinds.Trigger, "subject", GetParameter(parameters, "subject", string.Empty));
        return localization.Format("graph.summary.eventItem", eventText, subjectText);
    }

    public static string SummarizeStructuredNode(LocalizationService localization, GraphNodeDefinition node)
    {
        var template = GetParameter(node.Parameters, "template", node.Kind);
        var detail = GetParameter(node.Parameters, "detail", string.Empty);
        var templateText = GraphNodeCatalog.GetFieldDisplayValue(localization, node.Kind, "template", template);
        var title = string.IsNullOrWhiteSpace(node.Title) ? templateText : node.Title;
        return string.IsNullOrWhiteSpace(detail) ? $"{title} [{templateText}]" : $"{title} [{templateText}] - {detail}";
    }

    public static string GetNodeKindLabel(LocalizationService localization, string kind)
    {
        return kind switch
        {
            NodeKinds.Trigger => localization.T("graph.node.trigger"),
            NodeKinds.Condition => localization.T("graph.node.condition"),
            NodeKinds.Action => localization.T("graph.node.action"),
            _ => kind
        };
    }

    public static string BuildCanvasNodeCaption(LocalizationService localization, GraphNodeDefinition node)
    {
        var title = string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title;
        return $"{GetNodeKindLabel(localization, node.Kind)}\r\n{title}";
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }
}
