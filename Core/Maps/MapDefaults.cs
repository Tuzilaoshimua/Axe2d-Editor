using System.Drawing;

namespace Axe2DEditor.Core.Maps;

public static class MapDefaults
{
    public const string RuleAuto4 = "Auto4";
    public const string RuleRpgMakerA1 = "RpgMakerA1";
    public const string RuleRpgMakerA2 = "RpgMakerA2";
    public const string RuleRpgMakerA3 = "RpgMakerA3";
    public const string RuleRpgMakerA4 = "RpgMakerA4";
    public const string RuleWangSet = "WangSet";

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
        map.TilesetPlan.Tiles ??= [];
        map.TilesetPlan.Advanced ??= new TilesetAdvancedPlanDefinition();
        map.TilesetPlan.Advanced.WangSets ??= [];
        NormalizeTilesetRegions(map.TilesetPlan);
        NormalizeTilesetTileMetadata(map.TilesetPlan);
        NormalizeTilesetAdvancedPlan(map.TilesetPlan.Advanced);
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
            terrain.Frames ??= [];
            for (var frameIndex = terrain.Frames.Count - 1; frameIndex >= 0; frameIndex--)
            {
                var frame = terrain.Frames[frameIndex];
                frame.DurationMs = Math.Clamp(frame.DurationMs <= 0 ? 100 : frame.DurationMs, 16, 2000);
                if (frame.TileX < 0 || frame.TileY < 0)
                {
                    terrain.Frames.RemoveAt(frameIndex);
                }
            }
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
            region.AnimationFrameDurationMs = Math.Clamp(region.AnimationFrameDurationMs <= 0 ? 100 : region.AnimationFrameDurationMs, 16, 2000);
            region.AnimationFrames ??= [];
            for (var frameIndex = region.AnimationFrames.Count - 1; frameIndex >= 0; frameIndex--)
            {
                var frame = region.AnimationFrames[frameIndex];
                frame.DurationMs = Math.Clamp(frame.DurationMs <= 0 ? region.AnimationFrameDurationMs : frame.DurationMs, 16, 2000);
                if (frame.TileX < 0 || frame.TileY < 0)
                {
                    region.AnimationFrames.RemoveAt(frameIndex);
                }
            }
            if (!seen.Add(region.Id))
            {
                plan.Regions.RemoveAt(index);
            }
        }
    }

    private static void NormalizeTilesetAdvancedPlan(TilesetAdvancedPlanDefinition advanced)
    {
        var seenSets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var setIndex = advanced.WangSets.Count - 1; setIndex >= 0; setIndex--)
        {
            var set = advanced.WangSets[setIndex];
            set.Id = string.IsNullOrWhiteSpace(set.Id) ? $"wangset.{Guid.NewGuid():N}" : set.Id.Trim();
            set.Name = string.IsNullOrWhiteSpace(set.Name) ? set.Id : set.Name.Trim();
            set.Type = NormalizeWangSetType(set.Type);
            set.TileX = set.TileX < -1 ? -1 : set.TileX;
            set.TileY = set.TileY < -1 ? -1 : set.TileY;
            set.Colors ??= [];
            set.Tiles ??= [];
            if (!seenSets.Add(set.Id))
            {
                advanced.WangSets.RemoveAt(setIndex);
                continue;
            }

            NormalizeWangColors(set);
            NormalizeWangTiles(set);
        }
    }

    private static void NormalizeTilesetTileMetadata(TilesetPlanDefinition plan)
    {
        var seen = new HashSet<(int X, int Y)>();
        for (var index = plan.Tiles.Count - 1; index >= 0; index--)
        {
            var tile = plan.Tiles[index];
            tile.TileX = Math.Max(0, tile.TileX);
            tile.TileY = Math.Max(0, tile.TileY);
            tile.DisplayName ??= string.Empty;
            tile.Category ??= string.Empty;
            if (tile.MoveCost is { } moveCost)
            {
                tile.MoveCost = Math.Clamp(moveCost <= 0 ? 1d : moveCost, 0.01d, 999d);
            }
            tile.MaterialTag ??= string.Empty;
            tile.FootstepSoundId ??= string.Empty;
            tile.Tags = tile.Tags?
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];
            tile.CustomProperties = tile.CustomProperties?
                .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
                .ToDictionary(pair => pair.Key.Trim(), pair => pair.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                ?? [];
            tile.CollisionShapes ??= [];
            NormalizeTileCollisionShapes(tile);
            if (!seen.Add((tile.TileX, tile.TileY)))
            {
                plan.Tiles.RemoveAt(index);
            }
        }
    }

    private static void NormalizeTileCollisionShapes(TilesetTileMetadataDefinition tile)
    {
        for (var index = tile.CollisionShapes.Count - 1; index >= 0; index--)
        {
            var shape = tile.CollisionShapes[index];
            shape.ShapeType = NormalizeCollisionShapeType(shape.ShapeType);
            shape.X = Math.Clamp(shape.X, 0f, 1f);
            shape.Y = Math.Clamp(shape.Y, 0f, 1f);
            shape.Width = Math.Clamp(shape.Width <= 0f ? 1f : shape.Width, 0.01f, 1f);
            shape.Height = Math.Clamp(shape.Height <= 0f ? 1f : shape.Height, 0.01f, 1f);
            shape.Tag ??= string.Empty;
            shape.Points ??= [];
            foreach (var point in shape.Points)
            {
                point.X = Math.Clamp(point.X, 0f, 1f);
                point.Y = Math.Clamp(point.Y, 0f, 1f);
            }

            if (shape.ShapeType == TileCollisionShapeTypes.Polygon && shape.Points.Count < 3)
            {
                tile.CollisionShapes.RemoveAt(index);
            }
        }
    }

    private static string NormalizeCollisionShapeType(string? shapeType)
    {
        if (string.Equals(shapeType, TileCollisionShapeTypes.Ellipse, StringComparison.OrdinalIgnoreCase))
        {
            return TileCollisionShapeTypes.Ellipse;
        }

        if (string.Equals(shapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase))
        {
            return TileCollisionShapeTypes.Polygon;
        }

        return TileCollisionShapeTypes.Rectangle;
    }

    private static void NormalizeWangColors(TilesetWangSetDefinition set)
    {
        var seenIndexes = new HashSet<int>();
        for (var index = set.Colors.Count - 1; index >= 0; index--)
        {
            var color = set.Colors[index];
            color.Index = Math.Max(1, color.Index);
            color.Name = string.IsNullOrWhiteSpace(color.Name) ? $"颜色 {color.Index}" : color.Name.Trim();
            color.ColorHex = string.IsNullOrWhiteSpace(color.ColorHex) ? "#22c55e" : color.ColorHex.Trim();
            color.Probability = Math.Clamp(color.Probability <= 0d ? 1d : color.Probability, 0d, 999d);
            color.TileX = color.TileX < -1 ? -1 : color.TileX;
            color.TileY = color.TileY < -1 ? -1 : color.TileY;
            if (!seenIndexes.Add(color.Index))
            {
                set.Colors.RemoveAt(index);
            }
        }

        if (set.Colors.Count <= 0)
        {
            set.Colors.Add(new TilesetWangColorDefinition
            {
                Index = 1,
                Name = "地形",
                ColorHex = "#22c55e"
            });
        }

        set.Colors = set.Colors.OrderBy(v => v.Index).ToList();
    }

    private static void NormalizeWangTiles(TilesetWangSetDefinition set)
    {
        var colorIndexes = set.Colors.Select(v => v.Index).ToHashSet();
        var seenTiles = new HashSet<(int X, int Y)>();
        for (var index = set.Tiles.Count - 1; index >= 0; index--)
        {
            var tile = set.Tiles[index];
            tile.TileX = Math.Max(0, tile.TileX);
            tile.TileY = Math.Max(0, tile.TileY);
            tile.Probability = Math.Clamp(tile.Probability <= 0d ? 1d : tile.Probability, 0d, 999d);
            tile.WangId ??= [];
            while (tile.WangId.Count < 8)
            {
                tile.WangId.Add(0);
            }

            if (tile.WangId.Count > 8)
            {
                tile.WangId = tile.WangId.Take(8).ToList();
            }

            for (var valueIndex = 0; valueIndex < tile.WangId.Count; valueIndex++)
            {
                var value = tile.WangId[valueIndex];
                tile.WangId[valueIndex] = value > 0 && colorIndexes.Contains(value) ? value : 0;
            }

            if (tile.WangId.All(v => v == 0) || !seenTiles.Add((tile.TileX, tile.TileY)))
            {
                set.Tiles.RemoveAt(index);
            }
        }
    }

    private static string NormalizeWangSetType(string? type)
    {
        return type?.Trim().ToLowerInvariant() switch
        {
            TilesetWangSetTypes.Corner => TilesetWangSetTypes.Corner,
            TilesetWangSetTypes.Edge => TilesetWangSetTypes.Edge,
            _ => TilesetWangSetTypes.Mixed
        };
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

        if (string.Equals(kind, TilesetRegionKinds.RpgMakerA3, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetRegionKinds.RpgMakerA3;
        }

        if (string.Equals(kind, TilesetRegionKinds.RpgMakerA4, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetRegionKinds.RpgMakerA4;
        }

        if (string.Equals(kind, TilesetRegionKinds.Ignored, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetRegionKinds.Ignored;
        }

        if (string.Equals(kind, TilesetRegionKinds.Hidden, StringComparison.OrdinalIgnoreCase))
        {
            return TilesetRegionKinds.Hidden;
        }

        return TilesetRegionKinds.Normal;
    }

    private static string NormalizeTilesetRegionVariant(string kind, string? variant)
    {
        if (string.Equals(kind, TilesetRegionKinds.RpgMakerA4, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(variant, RpgMakerA4RegionVariants.Wall, StringComparison.OrdinalIgnoreCase)
                ? RpgMakerA4RegionVariants.Wall
                : RpgMakerA4RegionVariants.Roof;
        }

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
