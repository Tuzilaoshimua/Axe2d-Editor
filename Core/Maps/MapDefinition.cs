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
}

public static class TilesetRegionKinds
{
    public const string Normal = "Normal";
    public const string RpgMakerA1 = "RpgMakerA1";
    public const string RpgMakerA2 = "RpgMakerA2";
    public const string Ignored = "Ignored";
}

public static class RpgMakerA1RegionVariants
{
    public const string Water = "Water";
    public const string Decor = "Decor";
    public const string Waterfall = "Waterfall";
    public const string Frame = "Frame";
}

public static class TilesetPlanModes
{
    public const string Normal = "Normal";
    public const string RpgMaker = "RpgMaker";
    public const string Advanced = "Advanced";
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
