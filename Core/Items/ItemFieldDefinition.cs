namespace Axe2DEditor.Core.Items;

public sealed class ItemFieldDefinition
{
    public string Key { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public ItemFieldValueType ValueType { get; set; } = ItemFieldValueType.Text;

    public string DefaultValue { get; set; } = "";

    public bool Required { get; set; }

    public bool ReadOnly { get; set; }

    public string Category { get; set; } = "General";

    public string CategoryKey { get; set; } = "";

    public int Order { get; set; }

    public List<string> Options { get; set; } = [];
}
