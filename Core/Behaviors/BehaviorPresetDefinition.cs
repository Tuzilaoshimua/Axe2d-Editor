using Axe2DEditor.Core.Actors;
using Axe2DEditor.Core.Components;
using Axe2DEditor.Core.Entities;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Behaviors;

public sealed class BehaviorPresetDefinition : EntityDefinition
{
    public string Portrait { get; set; } = "";

    public SpriteSheetConfig Sprite { get; set; } = new();

    public List<ComponentConfig> Components { get; set; } = [];

    public Dictionary<string, string> Animations { get; set; } = [];

    // Legacy bridge for older project files. The canonical value lives in Stats["rewardExp"].
    [JsonPropertyName("rewardExp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double LegacyRewardExp { get; set; }

    public bool BuiltIn { get; set; }
}
