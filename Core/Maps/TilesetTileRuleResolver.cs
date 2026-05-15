using Axe2DEditor.Core.Tactics;

namespace Axe2DEditor.Core.Maps;

public static class TilesetTileRuleResolver
{
    public static ResolvedTileRule Resolve(
        TilesetTileMetadataDefinition? metadata,
        IEnumerable<TerrainRuleDefinition>? terrainRules)
    {
        var terrainRule = FindTerrainRule(metadata, terrainRules);
        var moveCost = metadata?.MoveCost
            ?? terrainRule?.MovementCost
            ?? 1d;
        var walkable = metadata?.Walkable ?? true;
        if (terrainRule?.BlocksMovement == true)
        {
            walkable = false;
        }

        var blocksSight = metadata?.BlocksSight == true || terrainRule?.BlocksLineOfSight == true;
        return new ResolvedTileRule(
            metadata,
            terrainRule,
            Math.Clamp(moveCost <= 0 ? 1d : moveCost, 0.01d, 999d),
            walkable,
            blocksSight,
            metadata?.MoveCost is not null);
    }

    public static TerrainRuleDefinition? FindTerrainRule(
        TilesetTileMetadataDefinition? metadata,
        IEnumerable<TerrainRuleDefinition>? terrainRules)
    {
        if (metadata is null || terrainRules is null)
        {
            return null;
        }

        var candidates = TerrainTagCandidates(metadata).ToList();
        if (candidates.Count <= 0)
        {
            return null;
        }

        return terrainRules.FirstOrDefault(rule =>
            !string.IsNullOrWhiteSpace(rule.TerrainTag)
            && candidates.Contains(rule.TerrainTag, StringComparer.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> TerrainTagCandidates(TilesetTileMetadataDefinition metadata)
    {
        if (metadata.CustomProperties.TryGetValue("terrainTag", out var terrainTag))
        {
            yield return terrainTag;
        }

        if (!string.IsNullOrWhiteSpace(metadata.MaterialTag))
        {
            yield return metadata.MaterialTag;
        }

        if (!string.IsNullOrWhiteSpace(metadata.Category))
        {
            yield return metadata.Category;
        }

        foreach (var tag in metadata.Tags)
        {
            yield return tag;
        }
    }
}

public sealed record ResolvedTileRule(
    TilesetTileMetadataDefinition? Metadata,
    TerrainRuleDefinition? TerrainRule,
    double MoveCost,
    bool Walkable,
    bool BlocksSight,
    bool HasTileMoveCostOverride);
