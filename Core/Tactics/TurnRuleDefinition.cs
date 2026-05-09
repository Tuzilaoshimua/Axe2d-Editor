namespace Axe2DEditor.Core.Tactics;

public sealed class TurnRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string TurnMode { get; set; } = "sideTurn";

    public int MaxRounds { get; set; }

    public string ActionRefreshMode { get; set; } = "turnStart";

    public string InitiativeStatKey { get; set; } = "";

    public bool AllowWait { get; set; } = true;

    public bool AllowUndoMove { get; set; } = true;

    public bool BuiltIn { get; set; }
}
