namespace Axe2DEditor.Core.Tactics;

public sealed class TerrainRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string TerrainTag { get; set; } = "";

    public double MovementCost { get; set; } = 1;

    public bool BlocksMovement { get; set; }

    public bool BlocksLineOfSight { get; set; }

    public double DefenseBonus { get; set; }

    public double EvasionBonus { get; set; }

    public double DamageModifier { get; set; } = 1;

    public List<string> AllowedUnitTags { get; set; } = [];

    public List<string> ForbiddenUnitTags { get; set; } = [];

    public bool BuiltIn { get; set; }
}
