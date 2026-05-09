using Axe2DEditor.Core.Entities;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Enemies;

public sealed class EnemyDefinition : EntityDefinition
{
    public string ActorRefId { get; set; } = "actor.hero";

    public string AiPreset { get; set; } = "MeleeChase";

    // Legacy bridge for older project files. The canonical value lives in Stats["rewardExp"].
    [JsonPropertyName("rewardExp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double LegacyRewardExp { get; set; }
}
