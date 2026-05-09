namespace Axe2DEditor.Core.Tactics;

public sealed class TacticalGridRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string GridType { get; set; } = "square";

    public int TileSize { get; set; } = 32;

    public string MovementMetric { get; set; } = "manhattan";

    public bool AllowDiagonalMove { get; set; }

    public bool HeightEnabled { get; set; }

    public bool ZoneOfControlEnabled { get; set; } = true;

    public bool BuiltIn { get; set; }
}
