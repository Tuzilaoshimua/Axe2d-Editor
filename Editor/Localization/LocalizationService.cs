using System.Text.Json;
using System.Text.RegularExpressions;

namespace Axe2DEditor.Editor.Localization;

public sealed class LocalizationService
{
    private static readonly Regex LocaleRegex = new(
        @"window\.Axe2DLocale\s*=\s*(\{[\s\S]*\})\s*;?",
        RegexOptions.Compiled);

    private readonly Dictionary<string, string> _strings = [];

    public IReadOnlyList<LocaleDefinition> AvailableLocales { get; } =
    [
        new("zh-CN", "language.chinese", "zh-CN.js"),
        new("en", "language.english", "en.js"),
        new("ja-JP", "language.japanese", "ja-JP.js")
    ];

    public string CurrentLanguage { get; private set; } = "zh-CN";

    public void Load(string language)
    {
        var locale = AvailableLocales.FirstOrDefault(item => item.Code == language)
            ?? AvailableLocales.First();

        var values = new Dictionary<string, string>();
        var baseLocale = AvailableLocales.First();
        var baseValues = LoadLocaleFile(baseLocale.FileName);
        foreach (var pair in baseValues)
        {
            values[pair.Key] = pair.Value;
        }

        if (!string.Equals(locale.Code, baseLocale.Code, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var pair in LoadLocaleFile(locale.FileName))
            {
                values[pair.Key] = pair.Value;
            }
        }

        _strings.Clear();
        foreach (var pair in values)
        {
            _strings[pair.Key] = pair.Value;
        }

        CurrentLanguage = locale.Code;
    }

    private static Dictionary<string, string> LoadLocaleFile(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Locales", fileName);
        if (!File.Exists(path))
        {
            path = Path.Combine(AppContext.BaseDirectory, fileName);
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Locale file was not found.", fileName);
        }

        var script = File.ReadAllText(path);
        var match = LocaleRegex.Match(script);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Locale file '{fileName}' is not valid.");
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(match.Groups[1].Value)
            ?? throw new InvalidOperationException($"Locale file '{fileName}' is empty.");
    }

    public string T(string key)
    {
        return _strings.TryGetValue(key, out var value) ? value : key;
    }

    public string T(string primaryKey, string fallbackKey)
    {
        if (_strings.TryGetValue(primaryKey, out var value))
        {
            return value;
        }

        return _strings.TryGetValue(fallbackKey, out var fallbackValue) ? fallbackValue : fallbackKey;
    }

    public string Format(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }
}
