using Axe2DEditor.Core.Components;
using Axe2DEditor.Core.Entities;

namespace Axe2DEditor.Core.Actors;

public sealed class ActorDefinition : EntityDefinition
{
    public string Portrait { get; set; } = "";

    public SpriteSheetConfig Sprite { get; set; } = new();

    public List<ComponentConfig> Components { get; set; } = [];

    public Dictionary<string, string> Animations { get; set; } = [];
}
