namespace Axe2DEditor.Core.Tactics;

public sealed class TacticalRangeDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string RangeShape { get; set; } = "diamond";

    public int MinRange { get; set; }

    public int MaxRange { get; set; } = 1;

    public string AreaShape { get; set; } = "single";

    public int AreaRadius { get; set; }

    public bool RequiresLineOfSight { get; set; }

    public bool CanTargetSelf { get; set; }

    public bool CanTargetAlly { get; set; } = true;

    public bool CanTargetEnemy { get; set; } = true;

    public bool TerrainBlocked { get; set; } = true;

    public List<string> RequiredTargetTags { get; set; } = [];

    public List<string> BlockedTargetTags { get; set; } = [];

    public bool BuiltIn { get; set; }
}
