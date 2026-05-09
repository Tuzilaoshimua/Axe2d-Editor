namespace Axe2DEditor.Core.Loot;

public sealed class LootEntryDefinition
{
    public string ItemId { get; set; } = "";

    public int MinQuantity { get; set; } = 1;

    public int MaxQuantity { get; set; } = 1;

    public double Chance { get; set; } = 1;
}
