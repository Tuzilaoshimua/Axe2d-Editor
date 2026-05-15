namespace Axe2DEditor.Core.Maps;

public sealed class MapDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string DisplayNameKey { get; set; } = "";

    public string Description { get; set; } = "";

    public string DescriptionKey { get; set; } = "";

    public string ViewType { get; set; } = "TopDown";

    public int Width { get; set; } = 64;

    public int Height { get; set; } = 64;

    public int TileSize { get; set; } = 32;

    public string Tileset { get; set; } = "tileset.default";

    public string TilesetImagePath { get; set; } = "";

    public string BackgroundColor { get; set; } = "#1f2937";

    public TilesetPlanDefinition TilesetPlan { get; set; } = new();

    public List<MapTerrainDefinition> Terrains { get; set; } = [];

    public List<MapLayerDefinition> Layers { get; set; } = [];
}

public sealed class TilesetPlanDefinition
{
    public int TileSize { get; set; } = 32;

    public string Mode { get; set; } = TilesetPlanModes.Normal;

    public string RpgMakerKind { get; set; } = RpgMakerTilesetKinds.A2;

    public string RpgMakerLayout { get; set; } = RpgMakerTilesetLayouts.Standard;

    public List<TilesetRegionDefinition> Regions { get; set; } = [];

    public List<TilesetTileMetadataDefinition> Tiles { get; set; } = [];

    public TilesetAdvancedPlanDefinition Advanced { get; set; } = new();
}

public sealed class TilesetRegionDefinition
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Kind { get; set; } = TilesetRegionKinds.Normal;

    public string Variant { get; set; } = "";

    public int X { get; set; }

    public int Y { get; set; }

    public int Width { get; set; } = 1;

    public int Height { get; set; } = 1;

    public bool Animated { get; set; }

    public int AnimationFrameDurationMs { get; set; } = 100;

    public List<TilesetFrameDefinition> AnimationFrames { get; set; } = [];
}

public sealed class TilesetFrameDefinition
{
    public int TileX { get; set; }

    public int TileY { get; set; }

    public int DurationMs { get; set; } = 100;
}

public sealed class TilesetTileMetadataDefinition
{
    public int TileX { get; set; }

    public int TileY { get; set; }

    public string DisplayName { get; set; } = "";

    public string Category { get; set; } = "";

    public bool Walkable { get; set; } = true;

    public bool BlocksSight { get; set; }

    public double? MoveCost { get; set; }

    public string MaterialTag { get; set; } = "";

    public string FootstepSoundId { get; set; } = "";

    public List<string> Tags { get; set; } = [];

    public Dictionary<string, string> CustomProperties { get; set; } = [];

    public List<TileCollisionShapeDefinition> CollisionShapes { get; set; } = [];
}

public sealed class TileCollisionShapeDefinition
{
    public string ShapeType { get; set; } = TileCollisionShapeTypes.Rectangle;

    public float X { get; set; }

    public float Y { get; set; }

    public float Width { get; set; } = 1f;

    public float Height { get; set; } = 1f;

    public List<TileCollisionPointDefinition> Points { get; set; } = [];

    public string Tag { get; set; } = "";
}

public sealed class TileCollisionPointDefinition
{
    public float X { get; set; }

    public float Y { get; set; }
}

public static class TileCollisionShapeTypes
{
    public const string Rectangle = "Rectangle";
    public const string Ellipse = "Ellipse";
    public const string Polygon = "Polygon";
}

public static class TilesetRegionKinds
{
    public const string Normal = "Normal";
    public const string RpgMakerA1 = "RpgMakerA1";
    public const string RpgMakerA2 = "RpgMakerA2";
    public const string RpgMakerA3 = "RpgMakerA3";
    public const string RpgMakerA4 = "RpgMakerA4";
    public const string AdvancedWang = "AdvancedWang";
    public const string Ignored = "Ignored";
    public const string Hidden = "Hidden";
}

public static class RpgMakerA1RegionVariants
{
    public const string Ocean = "Ocean";
    public const string DeepSea = "DeepSea";
    public const string OceanDecor = "OceanDecor";
    public const string Water = "Water";
    public const string Waterfall = "Waterfall";
}

public static class RpgMakerA4RegionVariants
{
    public const string Roof = "Roof";
    public const string Wall = "Wall";
}

public static class TilesetPlanModes
{
    public const string Normal = "Normal";
    public const string RpgMaker = "RpgMaker";
    public const string Advanced = "Advanced";
}

public sealed class TilesetAdvancedPlanDefinition
{
    public List<TilesetWangSetDefinition> WangSets { get; set; } = [];

    public bool AllowFlipHorizontally { get; set; }

    public bool AllowFlipVertically { get; set; }

    public bool AllowRotate { get; set; }

    public bool PreferUntransformedTiles { get; set; } = true;
}

public sealed class TilesetWangSetDefinition
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Type { get; set; } = TilesetWangSetTypes.Mixed;

    public int TileX { get; set; } = -1;

    public int TileY { get; set; } = -1;

    public List<TilesetWangColorDefinition> Colors { get; set; } = [];

    public List<TilesetWangTileDefinition> Tiles { get; set; } = [];
}

public sealed class TilesetWangColorDefinition
{
    public int Index { get; set; } = 1;

    public string Name { get; set; } = "";

    public string ColorHex { get; set; } = "#22c55e";

    public double Probability { get; set; } = 1d;

    public int TileX { get; set; } = -1;

    public int TileY { get; set; } = -1;
}

public sealed class TilesetWangTileDefinition
{
    public int TileX { get; set; }

    public int TileY { get; set; }

    public List<int> WangId { get; set; } = [0, 0, 0, 0, 0, 0, 0, 0];

    public double Probability { get; set; } = 1d;
}

public static class TilesetWangSetTypes
{
    public const string Corner = "corner";
    public const string Edge = "edge";
    public const string Mixed = "mixed";
}

public static class RpgMakerTilesetLayouts
{
    public const string Standard = "Standard";
    public const string Custom = "Custom";
}

public static class RpgMakerTilesetKinds
{
    public const string A1 = "A1";
    public const string A2 = "A2";
    public const string A3 = "A3";
    public const string A4 = "A4";
    public const string A5 = "A5";
}

public sealed class MapTerrainDefinition
{
    public string Id { get; set; } = "";

    public string DisplayName { get; set; } = "";

    public string ColorHex { get; set; } = "#6b7280";

    public string EdgeColorHex { get; set; } = "#374151";

    public string Rule { get; set; } = "Auto4";

    public bool Animated { get; set; }

    public int AnimationFrames { get; set; } = 1;

    public int AnimationFps { get; set; } = 4;

    public int TileX { get; set; } = -1;

    public int TileY { get; set; } = -1;

    public List<TilesetFrameDefinition> Frames { get; set; } = [];

    public int AnimationFrameDurationMs { get; set; } = 100;
}

public sealed class MapLayerDefinition
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Kind { get; set; } = "Tile";

    public bool Visible { get; set; } = true;

    public bool Locked { get; set; }

    public float Opacity { get; set; } = 1f;

    public List<MapTileCell> Tiles { get; set; } = [];
}

public sealed class MapTileCell
{
    public int X { get; set; }

    public int Y { get; set; }

    public string TerrainId { get; set; } = "";

    public int TileX { get; set; } = -1;

    public int TileY { get; set; } = -1;

    public int Variant { get; set; }

    public bool Solid { get; set; }

    public string Tag { get; set; } = "";
}
