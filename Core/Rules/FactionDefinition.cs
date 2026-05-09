namespace Axe2DEditor.Core.Rules;

public sealed class FactionDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string AttitudeToPlayer { get; set; } = "neutral";

    public bool BuiltIn { get; set; }
}
