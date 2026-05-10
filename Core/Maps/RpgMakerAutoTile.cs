using System.Drawing.Imaging;

namespace Axe2DEditor.Core.Maps;

public static class RpgMakerAutoTile
{
    public const int A3StandaloneShape = 15;
    public const int A4WallInteriorShape = 0;

    private static readonly int[] A1WaterSurfaceSequence = [0, 1, 2, 1];
    private static readonly Point[][] WallAutoTileTable =
    [
        [new Point(2, 2), new Point(1, 2), new Point(2, 1), new Point(1, 1)],
        [new Point(0, 2), new Point(1, 2), new Point(0, 1), new Point(1, 1)],
        [new Point(2, 0), new Point(1, 0), new Point(2, 1), new Point(1, 1)],
        [new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1)],
        [new Point(2, 2), new Point(3, 2), new Point(2, 1), new Point(3, 1)],
        [new Point(0, 2), new Point(3, 2), new Point(0, 1), new Point(3, 1)],
        [new Point(2, 0), new Point(3, 0), new Point(2, 1), new Point(3, 1)],
        [new Point(0, 0), new Point(3, 0), new Point(0, 1), new Point(3, 1)],
        [new Point(2, 2), new Point(1, 2), new Point(2, 3), new Point(1, 3)],
        [new Point(0, 2), new Point(1, 2), new Point(0, 3), new Point(1, 3)],
        [new Point(2, 0), new Point(1, 0), new Point(2, 3), new Point(1, 3)],
        [new Point(0, 0), new Point(1, 0), new Point(0, 3), new Point(1, 3)],
        [new Point(2, 2), new Point(3, 2), new Point(2, 3), new Point(3, 3)],
        [new Point(0, 2), new Point(3, 2), new Point(0, 3), new Point(3, 3)],
        [new Point(2, 0), new Point(3, 0), new Point(2, 3), new Point(3, 3)],
        [new Point(0, 0), new Point(3, 0), new Point(0, 3), new Point(3, 3)]
    ];
    private const int A1WaterfallFrameCount = 3;

    public static bool IsA1(MapTerrainDefinition? terrain)
    {
        return terrain is not null
            && terrain.Rule.Equals(MapDefaults.RuleRpgMakerA1, StringComparison.OrdinalIgnoreCase)
            && terrain.TileX >= 0
            && terrain.TileY >= 0;
    }

    public static bool IsA2(MapTerrainDefinition? terrain)
    {
        return terrain is not null
            && terrain.Rule.Equals(MapDefaults.RuleRpgMakerA2, StringComparison.OrdinalIgnoreCase)
            && terrain.TileX >= 0
            && terrain.TileY >= 0;
    }

    public static bool IsA3(MapTerrainDefinition? terrain)
    {
        return terrain is not null
            && terrain.Rule.Equals(MapDefaults.RuleRpgMakerA3, StringComparison.OrdinalIgnoreCase)
            && terrain.TileX >= 0
            && terrain.TileY >= 0;
    }

    public static bool IsA4(MapTerrainDefinition? terrain)
    {
        return terrain is not null
            && terrain.Rule.Equals(MapDefaults.RuleRpgMakerA4, StringComparison.OrdinalIgnoreCase)
            && terrain.TileX >= 0
            && terrain.TileY >= 0;
    }

    public static bool IsA4Wall(MapTerrainDefinition? terrain)
    {
        return IsA4(terrain)
            && terrain is not null
            && IsA4WallTerrain(terrain);
    }

    public static Point SnapA2Origin(int tileX, int tileY)
    {
        return new Point(Math.Max(0, tileX / 2 * 2), Math.Max(0, tileY / 3 * 3));
    }

    public static Point SnapA3Origin(int tileX, int tileY)
    {
        return new Point(Math.Max(0, tileX / 2 * 2), Math.Max(0, tileY / 2 * 2));
    }

    public static Point SnapA4Origin(int tileX, int tileY)
    {
        var x = Math.Max(0, tileX / 2 * 2);
        var y = Math.Max(0, tileY) switch
        {
            < 3 => 0,
            < 5 => 3,
            < 8 => 5,
            < 10 => 8,
            < 13 => 10,
            _ => 13
        };
        return new Point(x, y);
    }

    public static Point SnapA1Origin(int tileX, int tileY)
    {
        return new Point(Math.Max(0, tileX / 6 * 6), Math.Max(0, tileY / 3 * 3));
    }

    public static bool DrawA1(
        Graphics graphics,
        Image? tilesetImage,
        int tileSize,
        RectangleF destination,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity,
        int animationFrame)
    {
        if (!IsA1(terrain))
        {
            return false;
        }

        var kind = A1Kind(terrain);
        if (kind < 0)
        {
            return false;
        }

        if (kind >= 4 && kind % 2 == 1)
        {
            return DrawA1Waterfall(graphics, tilesetImage, tileSize, destination, cell, terrain, lookup, opacity, animationFrame, kind);
        }

        var waterSurfaceIndex = terrain.Animated
            ? A1WaterSurfaceSequence[animationFrame % A1WaterSurfaceSequence.Length]
            : 0;
        var block = A1FloorBlock(kind, waterSurfaceIndex);
        if (kind is 2 or 3)
        {
            DrawAutoTileBlock(graphics, tilesetImage, tileSize, destination, cell, terrain, lookup, opacity, 0, 0);
        }

        return DrawAutoTileBlock(graphics, tilesetImage, tileSize, destination, cell, terrain, lookup, opacity, block.X, block.Y);
    }

    private static int A1Kind(MapTerrainDefinition terrain)
    {
        if (terrain.Id.Contains(".ocean.", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (terrain.Id.Contains(".deepsea.", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (terrain.Id.Contains(".decor.", StringComparison.OrdinalIgnoreCase))
        {
            return terrain.TileY >= 3 ? 3 : 2;
        }

        if (terrain.Id.Contains(".waterfall.", StringComparison.OrdinalIgnoreCase))
        {
            return FindA1KindByBlock(terrain.TileX, terrain.TileY, waterfall: true);
        }

        return FindA1KindByBlock(terrain.TileX, terrain.TileY, waterfall: false);
    }

    private static int FindA1KindByBlock(int tileX, int tileY, bool waterfall)
    {
        for (var kind = waterfall ? 5 : 4; kind <= 15; kind += 2)
        {
            var block = waterfall ? A1WaterfallBlock(kind, 0) : A1FloorBlock(kind, 0);
            if (block.X == tileX && block.Y == tileY)
            {
                return kind;
            }
        }

        return waterfall ? 5 : 4;
    }

    private static Point A1FloorBlock(int kind, int waterSurfaceIndex)
    {
        if (kind == 0)
        {
            return new Point(waterSurfaceIndex * 2, 0);
        }

        if (kind == 1)
        {
            return new Point(waterSurfaceIndex * 2, 3);
        }

        if (kind == 2)
        {
            return new Point(6, 0);
        }

        if (kind == 3)
        {
            return new Point(6, 3);
        }

        var tx = kind % 8;
        var ty = kind / 8;
        var bx = tx / 4 * 8 + waterSurfaceIndex * 2;
        var by = ty * 6 + (tx / 2 % 2) * 3;
        return new Point(bx, by);
    }

    private static Point A1WaterfallBlock(int kind, int frame)
    {
        var tx = kind % 8;
        var ty = kind / 8;
        return new Point(tx / 4 * 8 + 6, ty * 6 + (tx / 2 % 2) * 3 + frame);
    }

    private static bool DrawA1Waterfall(
        Graphics graphics,
        Image? tilesetImage,
        int tileSize,
        RectangleF destination,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity,
        int animationFrame,
        int kind)
    {
        if (tilesetImage is null || tileSize <= 1 || terrain.TileX < 0 || terrain.TileY < 0)
        {
            return false;
        }

        var quarterSize = tileSize / 2f;
        var frame = terrain.Animated ? animationFrame % A1WaterfallFrameCount : 0;
        var block = A1WaterfallBlock(kind, frame);
        var sourceX = block.X * tileSize;
        var sourceY = block.Y * tileSize;
        if (sourceX < 0 || sourceY < 0 || sourceX + tileSize * 2 > tilesetImage.Width || sourceY + tileSize > tilesetImage.Height)
        {
            return false;
        }

        var sameW = IsSameTerrain(lookup, cell.X - 1, cell.Y, cell.TerrainId);
        var sameE = IsSameTerrain(lookup, cell.X + 1, cell.Y, cell.TerrainId);
        var table = WaterfallQuarters(sameW, sameE);

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);

        for (var index = 0; index < table.Length; index++)
        {
            var source = table[index];
            graphics.DrawImage(
                tilesetImage,
                GetDestinationQuarter(destination, index),
                (int)MathF.Round(sourceX + source.X * quarterSize),
                (int)MathF.Round(sourceY + source.Y * quarterSize),
                (int)MathF.Ceiling(quarterSize),
                (int)MathF.Ceiling(quarterSize),
                GraphicsUnit.Pixel,
                attributes);
        }

        return true;
    }

    private static Point[] WaterfallQuarters(bool west, bool east)
    {
        if (west && east)
        {
            return [new Point(2, 0), new Point(1, 0), new Point(2, 1), new Point(1, 1)];
        }

        if (east)
        {
            return [new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1)];
        }

        if (west)
        {
            return [new Point(2, 0), new Point(3, 0), new Point(2, 1), new Point(3, 1)];
        }

        return [new Point(0, 0), new Point(3, 0), new Point(0, 1), new Point(3, 1)];
    }

    public static bool DrawA2(
        Graphics graphics,
        Image? tilesetImage,
        int tileSize,
        RectangleF destination,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity,
        int animationFrame)
    {
        if (tilesetImage is null || tileSize <= 1 || !IsA2(terrain))
        {
            return false;
        }

        var frameOffset = terrain.Animated ? animationFrame % Math.Max(1, terrain.AnimationFrames) : 0;
        return DrawAutoTileBlock(graphics, tilesetImage, tileSize, destination, cell, terrain, lookup, opacity, terrain.TileX + frameOffset * 2, terrain.TileY);
    }

    public static bool DrawA3(
        Graphics graphics,
        Image? tilesetImage,
        int tileSize,
        RectangleF destination,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity)
    {
        int? fixedShape = TryParseA3FixedShapeTag(cell.Tag, out var shape) ? shape : null;
        return IsA3(terrain)
            && DrawWallAutoTileBlock(graphics, tilesetImage, tileSize, destination, cell, terrain, lookup, opacity, terrain.TileX, terrain.TileY, fixedShape);
    }

    public static bool DrawA4(
        Graphics graphics,
        Image? tilesetImage,
        int tileSize,
        RectangleF destination,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity)
    {
        if (!IsA4(terrain))
        {
            return false;
        }

        var fixedShape = TryParseA4FixedShapeTag(cell.Tag, out var shape) ? shape : (int?)null;
        return IsA4WallTerrain(terrain)
            ? DrawWallAutoTileBlock(graphics, tilesetImage, tileSize, destination, cell, terrain, lookup, opacity, terrain.TileX, terrain.TileY, fixedShape)
            : DrawAutoTileBlock(graphics, tilesetImage, tileSize, destination, cell, terrain, lookup, opacity, terrain.TileX, terrain.TileY);
    }

    public static string A3FixedShapeTag(int shape)
    {
        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"a3Shape:{Math.Clamp(shape, 0, WallAutoTileTable.Length - 1)}");
    }

    public static bool TryParseA3FixedShapeTag(string? tag, out int shape)
    {
        const string prefix = "a3Shape:";
        shape = 0;
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(tag[prefix.Length..], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out shape)
            && shape >= 0
            && shape < WallAutoTileTable.Length;
    }

    public static string A4FixedShapeTag(int shape)
    {
        return string.Create(System.Globalization.CultureInfo.InvariantCulture, $"a4Shape:{Math.Clamp(shape, 0, WallAutoTileTable.Length - 1)}");
    }

    public static bool TryParseA4FixedShapeTag(string? tag, out int shape)
    {
        const string prefix = "a4Shape:";
        shape = 0;
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(tag[prefix.Length..], System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out shape)
            && shape >= 0
            && shape < WallAutoTileTable.Length;
    }

    private static bool DrawAutoTileBlock(
        Graphics graphics,
        Image? tilesetImage,
        int tileSize,
        RectangleF destination,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity,
        int blockTileX,
        int blockTileY)
    {
        if (tilesetImage is null || tileSize <= 1 || blockTileX < 0 || blockTileY < 0)
        {
            return false;
        }

        var quarterSize = tileSize / 2f;
        var blockPixelX = blockTileX * tileSize;
        var blockPixelY = blockTileY * tileSize;
        if (blockPixelX < 0
            || blockPixelY < 0
            || blockPixelX + tileSize * 2 > tilesetImage.Width
            || blockPixelY + tileSize * 3 > tilesetImage.Height)
        {
            return false;
        }

        var sameN = IsSameTerrain(lookup, cell.X, cell.Y - 1, cell.TerrainId);
        var sameE = IsSameTerrain(lookup, cell.X + 1, cell.Y, cell.TerrainId);
        var sameS = IsSameTerrain(lookup, cell.X, cell.Y + 1, cell.TerrainId);
        var sameW = IsSameTerrain(lookup, cell.X - 1, cell.Y, cell.TerrainId);
        var sameNw = IsSameTerrain(lookup, cell.X - 1, cell.Y - 1, cell.TerrainId);
        var sameNe = IsSameTerrain(lookup, cell.X + 1, cell.Y - 1, cell.TerrainId);
        var sameSe = IsSameTerrain(lookup, cell.X + 1, cell.Y + 1, cell.TerrainId);
        var sameSw = IsSameTerrain(lookup, cell.X - 1, cell.Y + 1, cell.TerrainId);

        Span<Point> quarters =
        [
            PickTopLeft(sameN, sameW, sameNw),
            PickTopRight(sameN, sameE, sameNe),
            PickBottomLeft(sameS, sameW, sameSw),
            PickBottomRight(sameS, sameE, sameSe)
        ];

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);

        for (var index = 0; index < quarters.Length; index++)
        {
            var source = quarters[index];
            var destinationQuarter = GetDestinationQuarter(destination, index);
            graphics.DrawImage(
                tilesetImage,
                destinationQuarter,
                (int)MathF.Round(blockPixelX + source.X * quarterSize),
                (int)MathF.Round(blockPixelY + source.Y * quarterSize),
                (int)MathF.Ceiling(quarterSize),
                (int)MathF.Ceiling(quarterSize),
                GraphicsUnit.Pixel,
                attributes);
        }

        return true;
    }

    private static Point A4Block(int kind)
    {
        var tx = kind % 8;
        var ty = kind / 8;
        var y = (int)MathF.Floor(ty * 2.5f + (ty % 2 == 1 ? 0.5f : 0f));
        return new Point(tx * 2, y);
    }

    private static int A4Kind(int tileX, int tileY)
    {
        for (var kind = 0; kind < 48; kind++)
        {
            var block = A4Block(kind);
            if (block.X == tileX && block.Y == tileY)
            {
                return kind;
            }
        }

        return -1;
    }

    private static bool IsA4WallKind(int kind)
    {
        return kind % 16 is >= 8;
    }

    private static bool IsA4WallTerrain(MapTerrainDefinition terrain)
    {
        if (terrain.Id.Contains(".wall.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var kind = A4Kind(terrain.TileX, terrain.TileY);
        return kind >= 0 && IsA4WallKind(kind);
    }

    private static bool DrawWallAutoTileBlock(
        Graphics graphics,
        Image? tilesetImage,
        int tileSize,
        RectangleF destination,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity,
        int blockTileX,
        int blockTileY,
        int? fixedShape = null)
    {
        if (tilesetImage is null || tileSize <= 1 || blockTileX < 0 || blockTileY < 0)
        {
            return false;
        }

        var quarterSize = tileSize / 2f;
        var blockPixelX = blockTileX * tileSize;
        var blockPixelY = blockTileY * tileSize;
        if (blockPixelX < 0
            || blockPixelY < 0
            || blockPixelX + tileSize * 2 > tilesetImage.Width
            || blockPixelY + tileSize * 2 > tilesetImage.Height)
        {
            return false;
        }

        var shape = fixedShape ?? WallAutoTileShape(
            IsSameTerrain(lookup, cell.X, cell.Y - 1, cell.TerrainId),
            IsSameTerrain(lookup, cell.X + 1, cell.Y, cell.TerrainId),
            IsSameTerrain(lookup, cell.X, cell.Y + 1, cell.TerrainId),
            IsSameTerrain(lookup, cell.X - 1, cell.Y, cell.TerrainId));
        var quarters = WallAutoTileTable[shape];

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);

        for (var index = 0; index < quarters.Length; index++)
        {
            var source = quarters[index];
            graphics.DrawImage(
                tilesetImage,
                GetDestinationQuarter(destination, index),
                (int)MathF.Round(blockPixelX + source.X * quarterSize),
                (int)MathF.Round(blockPixelY + source.Y * quarterSize),
                (int)MathF.Ceiling(quarterSize),
                (int)MathF.Ceiling(quarterSize),
                GraphicsUnit.Pixel,
                attributes);
        }

        return true;
    }

    private static Point PickTopLeft(bool north, bool west, bool northWest)
    {
        if (!north && !west) return new Point(0, 0);
        if (!north) return new Point(2, 2);
        if (!west) return new Point(0, 4);
        return northWest ? new Point(2, 4) : new Point(2, 0);
    }

    private static Point PickTopRight(bool north, bool east, bool northEast)
    {
        if (!north && !east) return new Point(1, 0);
        if (!north) return new Point(1, 2);
        if (!east) return new Point(3, 4);
        return northEast ? new Point(1, 4) : new Point(3, 0);
    }

    private static Point PickBottomLeft(bool south, bool west, bool southWest)
    {
        if (!south && !west) return new Point(0, 1);
        if (!south) return new Point(2, 5);
        if (!west) return new Point(0, 3);
        return southWest ? new Point(2, 3) : new Point(2, 1);
    }

    private static Point PickBottomRight(bool south, bool east, bool southEast)
    {
        if (!south && !east) return new Point(1, 1);
        if (!south) return new Point(1, 5);
        if (!east) return new Point(3, 3);
        return southEast ? new Point(1, 3) : new Point(3, 1);
    }

    private static int WallAutoTileShape(bool north, bool east, bool south, bool west)
    {
        return (west ? 0 : 1)
            + (north ? 0 : 2)
            + (east ? 0 : 4)
            + (south ? 0 : 8);
    }

    private static Rectangle GetDestinationQuarter(RectangleF destination, int index)
    {
        var left = index % 2 == 0 ? destination.Left : destination.Left + destination.Width / 2f;
        var top = index < 2 ? destination.Top : destination.Top + destination.Height / 2f;
        var right = index % 2 == 0 ? destination.Left + destination.Width / 2f : destination.Right;
        var bottom = index < 2 ? destination.Top + destination.Height / 2f : destination.Bottom;
        return Rectangle.Round(RectangleF.FromLTRB(left, top, right, bottom));
    }

    private static bool IsSameTerrain(Dictionary<(int X, int Y), MapTileCell> lookup, int x, int y, string terrainId)
    {
        return lookup.TryGetValue((x, y), out var other)
            && string.Equals(other.TerrainId, terrainId, StringComparison.OrdinalIgnoreCase);
    }
}
