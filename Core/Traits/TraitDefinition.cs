namespace Axe2DEditor.Core.Traits;

public sealed class TraitDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string Category { get; set; } = "General";

    public string CategoryKey { get; set; } = "trait.category.general";

    public bool BuiltIn { get; set; }
}
