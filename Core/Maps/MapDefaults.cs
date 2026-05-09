using System.Drawing;

namespace Axe2DEditor.Core.Maps;

public static class MapDefaults
{
    public const string RuleAuto4 = "Auto4";
    public const string RuleRpgMakerA1 = "RpgMakerA1";
    public const string RuleRpgMakerA2 = "RpgMakerA2";

    public const string GroundLayerId = "layer.ground";
    public const string OverlayLayerId = "layer.overlay";
    public const string CollisionLayerId = "layer.collision";
    public const string RegionLayerId = "layer.region";

    public static List<MapTerrainDefinition> CreateDefaultTerrains()
    {
        return [];
    }

    public static List<MapLayerDefinition> CreateDefaultLayers(int width, int height)
    {
        return
        [
            new()
            {
                Id = GroundLayerId,
                Name = "地形",
                Kind = "Tile",
                Visible = true,
                Locked = false,
                Opacity = 1f,
                Tiles = []
            },
            new()
            {
                Id = OverlayLayerId,
                Name = "覆盖",
                Kind = "Tile",
                Visible = true,
                Locked = false,
                Opacity = 1f,
                Tiles = []
            },
            new()
            {
                Id = CollisionLayerId,
                Name = "碰撞",
                Kind = "Collision",
                Visible = true,
                Locked = false,
                Opacity = 0.65f,
                Tiles = []
            },
            new()
            {
                Id = RegionLayerId,
                Name = "区域",
                Kind = "Region",
                Visible = true,
                Locked = false,
                Opacity = 0.55f,
                Tiles = []
            }
        ];
    }

    public static void Normalize(MapDefinition map)
    {
        map.Id ??= string.Empty;
        map.DisplayName ??= string.Empty;
        map.DisplayNameKey ??= string.Empty;
        map.Description ??= string.Empty;
        map.DescriptionKey ??= string.Empty;
        map.ViewType = string.IsNullOrWhiteSpace(map.ViewType) ? "TopDown" : map.ViewType;
        map.Width = Math.Clamp(map.Width, 8, 512);
        map.Height = Math.Clamp(map.Height, 8, 512);
        map.TileSize = Math.Clamp(map.TileSize <= 0 ? 32 : map.TileSize, 8, 128);
        map.Tileset = string.IsNullOrWhiteSpace(map.Tileset) ? "tileset.default" : map.Tileset;
        map.TilesetImagePath ??= string.Empty;
        map.BackgroundColor = string.IsNullOrWhiteSpace(map.BackgroundColor) ? "#1f2937" : map.BackgroundColor;
        map.TilesetPlan ??= new TilesetPlanDefinition();
        map.TilesetPlan.TileSize = Math.Clamp(map.TilesetPlan.TileSize <= 0 ? map.TileSize : map.TilesetPlan.TileSize, 8, 128);
        map.TilesetPlan.Mode = NormalizeTilesetPlanMode(map.TilesetPlan.Mode);
        map.TilesetPlan.RpgMakerKind = NormalizeRpgMakerTilesetKind(map.TilesetPlan.RpgMakerKind);
        map.TilesetPlan.RpgMakerLayout = NormalizeRpgMakerTilesetLayout(map.TilesetPlan.RpgMakerLayout);
        map.TilesetPlan.Regions ??= [];
        NormalizeTilesetRegions(map.TilesetPlan);
        map.Terrains ??= [];
        map.Layers ??= [];

        AddMissingLayers(map);

        foreach (var terrain in map.Terrains)
        {
            terrain.Id ??= string.Empty;
            terrain.DisplayName ??= terrain.Id;
            terrain.ColorHex = string.IsNullOrWhiteSpace(terrain.ColorHex) ? "#6b7280" : terrain.ColorHex;
            terrain.EdgeColorHex = string.IsNullOrWhiteSpace(terrain.EdgeColorHex) ? "#374151" : terrain.EdgeColorHex;
            terrain.Rule = string.IsNullOrWhiteSpace(terrain.Rule) ? RuleAuto4 : terrain.Rule.Trim();
            terrain.AnimationFrames = Math.Clamp(terrain.AnimationFrames <= 0 ? 1 : terrain.AnimationFrames, 1, 32);
            terrain.AnimationFps = Math.Clamp(terrain.AnimationFps <= 0 ? 4 : terrain.AnimationFps, 1, 30);
            terrain.TileX = terrain.TileX < -1 ? -1 : terrain.TileX;
            terrain.TileY = terrain.TileY < -1 ? -1 : terrain.TileY;
        }

        foreach (var layer in map.Layers)
        {
            layer.Id = string.IsNullOrWhiteSpace(layer.Id) ? Guid.NewGuid().ToString("N") : layer.Id;
            layer.Name = string.IsNullOrWhiteSpace(layer.Name) ? layer.Kind : layer.Name;
            layer.Kind = string.IsNullOrWhiteSpace(layer.Kind) ? "Tile" : layer.Kind;
            layer.Opacity = Math.Clamp(layer.Opacity <= 0f ? 1f : layer.Opacity, 0.05f, 1f);
            layer.Tiles ??= [];
            NormalizeTiles(layer, map.Width, map.Height);
        }
    }

    public static Color ParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        try
        {
            return ColorTranslator.FromHtml(hex);
        }
        catch
        {
            return fallback;
        }
    }

    private static void AddMissingLayers(MapDefinition map)
    {
        var existing = map.Layers
            .Where(v => !string.IsNullOrWhiteSpace(v.Id))
            .Select(v => v.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var layer in CreateDefaultLayers(map.Width, map.Height))
        {
            if (existing.Add(layer.Id))
            {
                if (map.Layers.Count > 0 && layer.Id == GroundLayerId)
                {
                    layer.Tiles = [];
                }

                map.Layers.Add(layer);
            }
        }
    }

    private static void NormalizeTiles(MapLayerDefinition layer, int width, int height)
    {
        var seen = new HashSet<(int X, int Y)>();
        for (var index = layer.Tiles.Count - 1; index >= 0; index--)
        {
            var tile = layer.Tiles[index];
            tile.TerrainId ??= string.Empty;
            tile.Tag ??= string.Empty;
            tile.TileX = tile.TileX < -1 ? -1 : tile.TileX;
            tile.TileY = tile.TileY < -1 ? -1 : tile.TileY;
            if (tile.X < 0 || tile.Y < 0 || tile.X >= width || tile.Y >= height || !seen.Add((tile.X, tile.Y)))
            {
                layer.Tiles.RemoveAt(index);
            }
        }
    }

    private static void NormalizeTilesetRegions(TilesetPlanDefinition plan)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = plan.Regions.Count - 1; index >= 0; index--)
        {
            var region = plan.Regions[index];
            region.Id = string.IsNullOrWhiteSpace(region.Id) ? Guid.NewGuid().ToString("N") : region.Id.Trim();
            region.Name = string.IsNullOrWhiteSpace(region.Name) ? region.Id : region.Name.Trim();
            region.Kind = NormalizeTilesetRegionKind(region.Kind);
            region.Variant = NormalizeTilesetRegionVariant(region.Kind, region.Variant);
            region.X = Math.Max(0, region.X);
            region.Y = Math.Max(0, region.Y);
            region.Width = Math.Max(1, region.Width);
            region.Height = Math.Max(1, region.Height);
            if (!seen.Add(region.Id))
            {
                plan.Regions.RemoveAt(index);
            }
        }
    }

    private static string NormalizeTilesetRegionKind(string? kind)
    {
        if (string.Equals(kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetRegionKinds.RpgMakerA1;
        }

        if (string.Equals(kind, TilesetRegionKinds.RpgMakerA2, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetRegionKinds.RpgMakerA2;
        }

        if (string.Equals(kind, TilesetRegionKinds.Ignored, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetRegionKinds.Ignored;
        }

        return TilesetRegionKinds.Normal;
    }

    private static string NormalizeTilesetRegionVariant(string kind, string? variant)
    {
        if (!string.Equals(kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (string.Equals(variant, RpgMakerA1RegionVariants.Ocean, StringComparison.OrdinalIgnoreCase))
        {
            return RpgMakerA1RegionVariants.Ocean;
        }

        if (string.Equals(variant, RpgMakerA1RegionVariants.DeepSea, StringComparison.OrdinalIgnoreCase))
        {
            return RpgMakerA1RegionVariants.DeepSea;
        }

        if (string.Equals(variant, RpgMakerA1RegionVariants.OceanDecor, StringComparison.OrdinalIgnoreCase)
            || string.Equals(variant, "Decor", StringComparison.OrdinalIgnoreCase))
        {
            return RpgMakerA1RegionVariants.OceanDecor;
        }

        if (string.Equals(variant, RpgMakerA1RegionVariants.Waterfall, StringComparison.OrdinalIgnoreCase)
            || string.Equals(variant, "Frame", StringComparison.OrdinalIgnoreCase))
        {
            return RpgMakerA1RegionVariants.Waterfall;
        }

        return RpgMakerA1RegionVariants.Water;
    }

    private static string NormalizeTilesetPlanMode(string? mode)
    {
        if (string.Equals(mode, TilesetPlanModes.RpgMaker, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetPlanModes.RpgMaker;
        }

        if (string.Equals(mode, TilesetPlanModes.Advanced, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetPlanModes.Advanced;
        }

        return TilesetPlanModes.Normal;
    }

    private static string NormalizeRpgMakerTilesetLayout(string? layout)
    {
        return string.Equals(layout, RpgMakerTilesetLayouts.Custom, StringComparison.OrdinalIgnoreCase)
            ? RpgMakerTilesetLayouts.Custom
            : RpgMakerTilesetLayouts.Standard;
    }

    private static string NormalizeRpgMakerTilesetKind(string? kind)
    {
        return kind?.Trim().ToUpperInvariant() switch
        {
            RpgMakerTilesetKinds.A1 => RpgMakerTilesetKinds.A1,
            RpgMakerTilesetKinds.A3 => RpgMakerTilesetKinds.A3,
            RpgMakerTilesetKinds.A4 => RpgMakerTilesetKinds.A4,
            RpgMakerTilesetKinds.A5 => RpgMakerTilesetKinds.A5,
            _ => RpgMakerTilesetKinds.A2
        };
    }

}
