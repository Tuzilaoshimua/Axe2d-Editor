using System.Text.Json;

namespace Axe2DEditor.Editor.Localization;

public sealed class EditorSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string SettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Axe2D",
        "EditorSettings.json");

    public EditorSettings Load()
    {
        if (!File.Exists(SettingsFilePath))
        {
            return new EditorSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<EditorSettings>(json, JsonOptions) ?? new EditorSettings();
        }
        catch
        {
            return new EditorSettings();
        }
    }

    public void Save(EditorSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }
}
