namespace Axe2DEditor.Core.Actors;

public sealed class SpriteSheetConfig
{
    public string Sheet { get; set; } = "";

    public int TileWidth { get; set; } = 32;

    public int TileHeight { get; set; } = 48;

    public int PivotX { get; set; } = 16;

    public int PivotY { get; set; } = 40;
}
