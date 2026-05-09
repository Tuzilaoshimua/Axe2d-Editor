namespace Axe2DEditor.Editor.Localization;

public sealed class EditorSettings
{
    public string Language { get; set; } = "zh-CN";

    public string Theme { get; set; } = "light";

    public string LastProjectDirectory { get; set; } = string.Empty;

    public string LastTilesetDirectory { get; set; } = string.Empty;
}
