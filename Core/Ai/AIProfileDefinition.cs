using Axe2DEditor.Core.Entities;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Ai;

public sealed class AIProfileDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string BehaviorType { get; set; } = "passive";

    public string MovementMode { get; set; } = "topDown";

    public string TargetSelector { get; set; } = "nearestHostile";

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> TargetTags { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> SkillIds { get; set; } = [];

    public double PerceptionRange { get; set; } = 6;

    public double LeashRange { get; set; } = 10;

    public double PreferredRange { get; set; } = 1.2;

    public double FleeHealthPercent { get; set; }

    public string PatrolMode { get; set; } = "none";

    public Dictionary<string, string> Parameters { get; set; } = [];

    public bool BuiltIn { get; set; }
}
