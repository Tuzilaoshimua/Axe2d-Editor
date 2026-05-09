namespace Axe2DEditor.Editor.Localization;

public sealed class LocaleDefinition
{
    public LocaleDefinition(string code, string displayKey, string fileName)
    {
        Code = code;
        DisplayKey = displayKey;
        FileName = fileName;
    }

    public string Code { get; }

    public string DisplayKey { get; }

    public string FileName { get; }
}
