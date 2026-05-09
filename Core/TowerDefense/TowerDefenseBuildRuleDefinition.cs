namespace Axe2DEditor.Core.TowerDefense;

public sealed class TowerDefenseBuildRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string BuildSurfaceTag { get; set; } = "buildable";

    public bool PreventPathBlocking { get; set; } = true;

    public bool AllowSell { get; set; } = true;

    public double SellRefundRatio { get; set; } = 0.7;

    public bool AllowUpgradeDuringWave { get; set; } = true;

    public string CurrencyStatKey { get; set; } = "gold";

    public bool BuiltIn { get; set; }
}
