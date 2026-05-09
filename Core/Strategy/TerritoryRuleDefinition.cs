namespace Axe2DEditor.Core.Strategy;

public sealed class TerritoryRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string TerritoryTag { get; set; } = "territory";

    public string ControlMode { get; set; } = "occupyPoint";

    public int ControlRadius { get; set; } = 3;

    public string OwnerFactionId { get; set; } = "";

    public Dictionary<string, double> ResourceYields { get; set; } = [];

    public List<string> RequiredUnitTags { get; set; } = [];

    public List<string> BlockedUnitTags { get; set; } = [];

    public string EventGraphId { get; set; } = "";

    public bool BuiltIn { get; set; }
}
