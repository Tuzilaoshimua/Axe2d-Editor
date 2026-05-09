using Axe2DEditor.Core.Entities;

namespace Axe2DEditor.Core.Stats;

public static class StatResolver
{
    public static Dictionary<string, double> CreateDefaultStatMap(IEnumerable<StatDefinition> definitions)
    {
        var map = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var stat in definitions)
        {
            map[stat.Key] = stat.DefaultValue;
        }

        return map;
    }

    public static Dictionary<string, double> ResolveEntityStats(EntityDefinition entity, IEnumerable<StatDefinition> definitions)
    {
        return ResolveStats(definitions, entity.Stats);
    }

    public static double ResolveStatValue(IReadOnlyDictionary<string, double>? values, StatDefinition definition)
    {
        return values is not null && values.TryGetValue(definition.Key, out var value)
            ? value
            : definition.DefaultValue;
    }

    private static Dictionary<string, double> ResolveStats(IEnumerable<StatDefinition> definitions, IReadOnlyDictionary<string, double>? source)
    {
        var resolved = CreateDefaultStatMap(definitions);
        if (source is null)
        {
            return resolved;
        }

        foreach (var pair in source)
        {
            resolved[pair.Key] = pair.Value;
        }

        return resolved;
    }
}
