using Axe2DEditor.Core.Entities;
using System.Text.Json.Serialization;

namespace Axe2DEditor.Core.Decorations;

public sealed class DecorationDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string DecorationKind { get; set; } = "static";

    public string SpriteSheet { get; set; } = "";

    public string AnimationKey { get; set; } = "";

    public bool BlocksMovement { get; set; }

    public bool Destructible { get; set; }

    public string LootTableId { get; set; } = "";

    public string InteractionProfileId { get; set; } = "";

    [JsonConverter(typeof(DelimitedStringListConverter))]
    public List<string> Tags { get; set; } = [];

    public bool BuiltIn { get; set; }
}
