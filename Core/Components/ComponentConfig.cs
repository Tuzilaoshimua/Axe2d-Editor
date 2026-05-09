namespace Axe2DEditor.Core.Components;

public sealed class ComponentConfig
{
    public string Type { get; set; } = "";

    public Dictionary<string, object?> Parameters { get; set; } = [];
}
