using Axe2DEditor.Core.Entities;
using Axe2DEditor.Core.Effects;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Skills;

public sealed class SkillDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string SkillType { get; set; } = "active";

    public string TargetingMode { get; set; } = "selfForward";

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> RequiredTargetTags { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> BlockedTargetTags { get; set; } = [];

    public string ElementId { get; set; } = "element.none";

    public string DamageTypeId { get; set; } = "damage.physical";

    public string PowerStatKey { get; set; } = "attack";

    public double PowerMultiplier { get; set; } = 1.0;

    public double BasePower { get; set; } = 0;

    public string CostStatKey { get; set; } = "";

    public double CostAmount { get; set; }

    public double CastTimeSeconds { get; set; }

    public double CooldownSeconds { get; set; } = 1.0;

    public double Range { get; set; } = 1.5;

    public double AreaRadius { get; set; }

    public string ProjectileId { get; set; } = "";

    public string FormulaId { get; set; } = "";

    public List<GameplayEffectReference> Effects { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> StatusIds { get; set; } = [];

    // Legacy bridge for older project files. The canonical value lives in Effects.
    [JsonConverter(typeof(DelimitedStringListConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> EffectIds { get; set; } = [];

    public string VisualEffectId { get; set; } = "";

    public string SoundCue { get; set; } = "";

    public string Tags { get; set; } = "";
}
