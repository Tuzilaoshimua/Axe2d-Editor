namespace Axe2DEditor.Core.Tactics;

public sealed class ObjectiveRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string ObjectiveType { get; set; } = "defeatAll";

    public bool IsVictoryCondition { get; set; } = true;

    public List<string> TargetUnitTags { get; set; } = [];

    public List<string> TargetAreaTags { get; set; } = [];

    public int RequiredCount { get; set; } = 1;

    public int RoundLimit { get; set; }

    public string EventGraphId { get; set; } = "";

    public bool BuiltIn { get; set; }
}
