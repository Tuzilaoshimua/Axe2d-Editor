namespace Axe2DEditor.Core.Strategy;

public sealed class ProductionRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string ProducerUnitId { get; set; } = "";

    public string ProducedAssetId { get; set; } = "";

    public string ProducedAssetKind { get; set; } = "unit";

    public int BuildTimeTurns { get; set; } = 1;

    public double BuildTimeSeconds { get; set; }

    public Dictionary<string, double> ResourceCosts { get; set; } = [];

    public Dictionary<string, double> ResourceOutputs { get; set; } = [];

    public bool RequiresBuildQueue { get; set; } = true;

    public bool Repeatable { get; set; } = true;

    public string EventGraphId { get; set; } = "";

    public bool BuiltIn { get; set; }
}
