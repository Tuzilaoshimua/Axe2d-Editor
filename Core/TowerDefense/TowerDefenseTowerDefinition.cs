namespace Axe2DEditor.Core.TowerDefense;

public sealed class TowerDefenseTowerDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string UnitId { get; set; } = "";

    public string SkillId { get; set; } = "";

    public string TowerRole { get; set; } = "damage";

    public int BuildCost { get; set; } = 100;

    public double Range { get; set; } = 5;

    public double AttackIntervalSeconds { get; set; } = 1;

    public string TargetPriority { get; set; } = "first";

    public List<TowerDefenseTowerLevelDefinition> Levels { get; set; } = [];

    public bool BuiltIn { get; set; }
}

public sealed class TowerDefenseTowerLevelDefinition
{
    public int Level { get; set; } = 1;

    public int UpgradeCost { get; set; }

    public double RangeBonus { get; set; }

    public double DamageMultiplier { get; set; } = 1;

    public double AttackIntervalMultiplier { get; set; } = 1;

    public string SkillId { get; set; } = "";
}
