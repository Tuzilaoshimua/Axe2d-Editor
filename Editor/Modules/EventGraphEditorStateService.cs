using Axe2DEditor.Core.Graphs;

namespace Axe2DEditor.Editor.Modules;

internal static class EventGraphEditorStateService
{
    public static bool SaveStructuredTriggerState(
        GraphNodeDefinition? selectedTrigger,
        string triggerName,
        Func<GraphNodeDefinition?, string> getTriggerMode,
        Func<GraphNodeDefinition, bool> isTriggerEnabled)
    {
        if (selectedTrigger is null)
        {
            return false;
        }

        selectedTrigger.Title = triggerName.Trim();
        selectedTrigger.Parameters["mode"] = getTriggerMode(selectedTrigger);
        selectedTrigger.Parameters["enabled"] = isTriggerEnabled(selectedTrigger)
            ? "true"
            : GetParameter(selectedTrigger.Parameters, "enabled", "true");
        return true;
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }
}
