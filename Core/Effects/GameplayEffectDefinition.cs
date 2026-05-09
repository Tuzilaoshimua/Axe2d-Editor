using Axe2DEditor.Core.Entities;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Effects;

public sealed class GameplayEffectDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string IconPath { get; set; } = "";

    public string EffectKind { get; set; } = "damage";

    public List<EffectParameterDefinition> Parameters { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> Tags { get; set; } = [];

    // Legacy bridges for older projects. The canonical values live in Parameters.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string StatKey { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string FormulaId { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double BaseValue { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string DamageTypeId { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string ElementId { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string StatusId { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double DurationSeconds { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Chance { get; set; } = 1;

    public bool BuiltIn { get; set; }
}
