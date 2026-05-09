namespace Axe2DEditor.Core.Stats;

public sealed class StatDefinition
{
    public string Id { get; set; } = "";

    public string Key { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string CategoryKey { get; set; } = "";

    public string ValueType { get; set; } = "Number";

    public double DefaultValue { get; set; }

    public double Min { get; set; }

    public double Max { get; set; } = 999;

    public string Category { get; set; } = "General";
}
