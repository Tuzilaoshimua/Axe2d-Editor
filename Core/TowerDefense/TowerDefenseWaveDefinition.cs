namespace Axe2DEditor.Core.TowerDefense;

public sealed class TowerDefenseWaveDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string MapId { get; set; } = "";

    public string PathId { get; set; } = "";

    public double StartDelaySeconds { get; set; }

    public double SpawnIntervalSeconds { get; set; } = 1;

    public int RewardGold { get; set; }

    public int RewardExp { get; set; }

    public List<TowerDefenseSpawnGroupDefinition> SpawnGroups { get; set; } = [];

    public bool BuiltIn { get; set; }
}

public sealed class TowerDefenseSpawnGroupDefinition
{
    public string UnitId { get; set; } = "";

    public int Count { get; set; } = 1;

    public double IntervalSeconds { get; set; } = 1;

    public double DelaySeconds { get; set; }

    public string PathId { get; set; } = "";
}
