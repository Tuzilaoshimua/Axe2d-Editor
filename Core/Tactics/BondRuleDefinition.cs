namespace Axe2DEditor.Core.Tactics;

public sealed class BondRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string TriggerTiming { get; set; } = "whileAdjacent";

    public int Range { get; set; } = 1;

    public int MinParticipants { get; set; } = 2;

    public int MaxParticipants { get; set; }

    public bool RequireSameFaction { get; set; } = true;

    public bool RequireLineOfSight { get; set; }

    public List<string> RequiredUnitTags { get; set; } = [];

    public List<string> ExcludedUnitTags { get; set; } = [];

    public List<string> EffectIds { get; set; } = [];

    public string DurationMode { get; set; } = "whileConditionMet";

    public string StackingMode { get; set; } = "refresh";

    public bool BuiltIn { get; set; }
}
