namespace Axe2DEditor.Core.Rules;

public sealed class ElementDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string ColorHex { get; set; } = "#ffffff";

    public bool BuiltIn { get; set; }
}
