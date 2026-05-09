namespace Axe2DEditor.Core.Assets;

public static class AssetId
{
    public static string Create(string prefix, string name)
    {
        var normalized = new string(name
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray());

        while (normalized.Contains("__", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("__", "_", StringComparison.Ordinal);
        }

        normalized = normalized.Trim('_');
        return string.IsNullOrWhiteSpace(normalized)
            ? $"{prefix}.unnamed"
            : $"{prefix}.{normalized}";
    }
}
