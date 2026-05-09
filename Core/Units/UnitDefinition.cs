using System.Text.Json;
using System.Text.Json.Serialization;
using Axe2DEditor.Core.Actors;
using Axe2DEditor.Core.Components;
using Axe2DEditor.Core.Entities;

namespace Axe2DEditor.Core.Units;

public sealed class UnitDefinition : EntityDefinition
{
    public string UnitKind { get; set; } = "enemy";

    public string FactionId { get; set; } = "faction.neutral";

    public string AIProfileId { get; set; } = "";

    // Legacy bridges for older project files. The canonical values live in AIProfileId and the current unit structure.
    [JsonPropertyName("aiPreset")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? AiPreset { get; set; }

    [JsonPropertyName("legacyKind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? LegacyKind { get; set; }

    public string LootTableId { get; set; } = "";

    public string InteractionProfileId { get; set; } = "";

    public string Portrait { get; set; } = "";

    public SpriteSheetConfig Sprite { get; set; } = new();

    public List<ComponentConfig> Components { get; set; } = [];

    public Dictionary<string, string> Animations { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? LegacyData { get; set; }
}
