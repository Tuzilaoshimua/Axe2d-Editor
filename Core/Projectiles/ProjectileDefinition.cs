using Axe2DEditor.Core.Entities;
using Axe2DEditor.Core.Effects;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Projectiles;

public sealed class ProjectileDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public double Speed { get; set; } = 360;

    public double LifetimeSeconds { get; set; } = 2;

    public double Radius { get; set; } = 0.25;

    public bool Piercing { get; set; }

    public string VisualEffectId { get; set; } = "";

    public List<GameplayEffectReference> Effects { get; set; } = [];

    [JsonConverter(typeof(DelimitedStringListConverter))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<string> EffectIds { get; set; } = [];

    public bool BuiltIn { get; set; }
}
