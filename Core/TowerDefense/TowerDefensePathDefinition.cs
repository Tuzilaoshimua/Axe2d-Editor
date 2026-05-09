namespace Axe2DEditor.Core.TowerDefense;

public sealed class TowerDefensePathDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string MapId { get; set; } = "";

    public string SpawnPointId { get; set; } = "";

    public string GoalPointId { get; set; } = "";

    public string PathMode { get; set; } = "waypoints";

    public bool AllowBranching { get; set; }

    public List<TowerDefenseWaypointDefinition> Waypoints { get; set; } = [];

    public bool BuiltIn { get; set; }
}

public sealed class TowerDefenseWaypointDefinition
{
    public string Key { get; set; } = "";

    public double X { get; set; }

    public double Y { get; set; }

    public double WaitSeconds { get; set; }
}
