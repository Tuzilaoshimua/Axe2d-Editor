namespace Axe2DEditor.Core.Effects;

public sealed class VisualEffectDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string EffectKind { get; set; } = "spriteAnimation";

    public string SpriteSheet { get; set; } = "";

    public string AnimationKey { get; set; } = "";

    public string SoundCue { get; set; } = "";

    public double DurationSeconds { get; set; } = 0.5;

    public bool Loop { get; set; }

    public bool BuiltIn { get; set; }
}
