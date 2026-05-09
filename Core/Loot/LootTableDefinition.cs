namespace Axe2DEditor.Core.Loot;

public sealed class LootTableDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public List<LootEntryDefinition> Entries { get; set; } = [];

    public bool BuiltIn { get; set; }
}
