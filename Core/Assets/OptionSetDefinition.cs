namespace Axe2DEditor.Core.Assets;

public sealed class OptionSetDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public Dictionary<string, string> Values { get; set; } = [];
}
