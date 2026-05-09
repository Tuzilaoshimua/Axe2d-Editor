namespace Axe2DEditor.Core.Interactions;

public sealed class InteractionProfileDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string InteractionKind { get; set; } = "generic";

    public string TriggerName { get; set; } = "OnInteract";

    public string EventGraphId { get; set; } = "";

    public bool OnceOnly { get; set; }

    public bool BuiltIn { get; set; }
}
