using Axe2DEditor.Core.Components;

namespace Axe2DEditor.Core.Components;

public sealed class ComponentPresetDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public ComponentConfig Component { get; set; } = new();

    public bool BuiltIn { get; set; }
}
