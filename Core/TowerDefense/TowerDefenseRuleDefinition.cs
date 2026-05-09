namespace Axe2DEditor.Core.TowerDefense;

public sealed class TowerDefenseRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string MapId { get; set; } = "";

    public int StartingGold { get; set; } = 200;

    public int BaseLife { get; set; } = 20;

    public int LeakDamagePerUnit { get; set; } = 1;

    public string BuildRuleId { get; set; } = "";

    public string WaveStartMode { get; set; } = "manual";

    public string VictoryCondition { get; set; } = "allWavesCleared";

    public string DefeatCondition { get; set; } = "baseLifeZero";

    public List<string> WaveIds { get; set; } = [];

    public bool BuiltIn { get; set; }
}
