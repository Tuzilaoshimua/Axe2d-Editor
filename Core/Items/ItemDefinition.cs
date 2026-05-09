using Axe2DEditor.Core.Entities;
using Axe2DEditor.Core.Effects;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Items;

public sealed class ItemDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string TypeId { get; set; } = "consumable";

    public string Rarity { get; set; } = "common";

    public string EquipmentSlot { get; set; } = "";

    public int Price { get; set; }

    public int StackLimit { get; set; } = 1;

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    [Browsable(false)]
    public string EffectType { get; set; } = "effect.heal";

    [Browsable(false)]
    public double EffectValue { get; set; } = 30;

    public bool Consumable { get; set; } = true;

    public List<GameplayEffectReference> Effects { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> EffectIds { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> GrantedSkillIds { get; set; } = [];

    public string Tags { get; set; } = "";

    public List<ItemFieldValue> CustomValues { get; set; } = [];
}
