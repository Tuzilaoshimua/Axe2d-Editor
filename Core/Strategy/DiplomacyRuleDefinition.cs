namespace Axe2DEditor.Core.Strategy;

public sealed class DiplomacyRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string FromFactionId { get; set; } = "";

    public string ToFactionId { get; set; } = "";

    public string DiplomaticState { get; set; } = "neutral";

    public int StartingTrust { get; set; }

    public bool AllowsTrade { get; set; }

    public bool AllowsSharedVision { get; set; }

    public bool AllowsPassage { get; set; }

    public string EventGraphId { get; set; } = "";

    public bool BuiltIn { get; set; }
}
