namespace Axe2DEditor.Core.Effects;

public sealed class GameplayEffectReference
{
    public string EffectId { get; set; } = "";

    public List<EffectParameterValue> Parameters { get; set; } = [];
}
