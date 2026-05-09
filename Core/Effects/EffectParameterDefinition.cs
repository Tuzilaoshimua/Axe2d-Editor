namespace Axe2DEditor.Core.Effects;

public sealed class EffectParameterDefinition
{
    public string Key { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public EffectParameterValueType ValueType { get; set; } = EffectParameterValueType.Text;

    public string DefaultValue { get; set; } = "";

    public bool Required { get; set; }

    public bool ReadOnly { get; set; }

    public string Category { get; set; } = "General";

    public string CategoryKey { get; set; } = "";

    public int Order { get; set; }

    public List<string> Options { get; set; } = [];

    // Data-driven option source, for example: tag, stat, formula, status, skill, projectile.
    public string OptionSourceId { get; set; } = "";
}
