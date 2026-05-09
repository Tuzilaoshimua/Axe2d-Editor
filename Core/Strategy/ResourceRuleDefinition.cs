namespace Axe2DEditor.Core.Strategy;

public sealed class ResourceRuleDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string ResourceKind { get; set; } = "currency";

    public string StatKey { get; set; } = "";

    public int StartingAmount { get; set; }

    public int StorageLimit { get; set; }

    public bool SharedByFaction { get; set; } = true;

    public bool CanGoNegative { get; set; }

    public string IconPath { get; set; } = "";

    public List<string> Tags { get; set; } = [];

    public bool BuiltIn { get; set; }
}
