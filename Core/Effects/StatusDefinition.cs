using Axe2DEditor.Core.Entities;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Effects;

public sealed class StatusDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string StatusKind { get; set; } = "debuff";

    public double DurationSeconds { get; set; } = 3;

    public int MaxStacks { get; set; } = 1;

    public double TickIntervalSeconds { get; set; }

    public List<GameplayEffectReference> OnApplyEffects { get; set; } = [];

    public List<GameplayEffectReference> PeriodicEffects { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> OnApplyEffectIds { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> PeriodicEffectIds { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> Tags { get; set; } = [];

    public bool BuiltIn { get; set; }
}
