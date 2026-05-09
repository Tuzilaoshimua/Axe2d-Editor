namespace Axe2DEditor.Core.Strategy;

public sealed class TechRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string TechKind { get; set; } = "upgrade";

    public List<string> PrerequisiteTechIds { get; set; } = [];

    public Dictionary<string, double> ResearchCosts { get; set; } = [];

    public int ResearchTurns { get; set; } = 1;

    public List<string> UnlockUnitIds { get; set; } = [];

    public List<string> UnlockSkillIds { get; set; } = [];

    public List<string> UnlockBuildRuleIds { get; set; } = [];

    public List<string> EffectIds { get; set; } = [];

    public string EventGraphId { get; set; } = "";

    public bool BuiltIn { get; set; }
}
