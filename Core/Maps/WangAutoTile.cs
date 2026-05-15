using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

namespace Axe2DEditor.Core.Maps;

public static class WangAutoTile
{
    private const string TerrainPrefix = "terrain.tileset.wang.";
    private const int Top = 0;
    private const int TopRight = 1;
    private const int Right = 2;
    private const int BottomRight = 3;
    private const int Bottom = 4;
    private const int BottomLeft = 5;
    private const int Left = 6;
    private const int TopLeft = 7;

    public static bool IsWang(MapTerrainDefinition? terrain)
    {
        return terrain is not null
            && terrain.Rule.Equals(MapDefaults.RuleWangSet, StringComparison.OrdinalIgnoreCase)
            && TryParseTerrainId(terrain.Id, out _, out _);
    }

    public static string TerrainId(string setId, int colorIndex)
    {
        return $"{TerrainPrefix}{SetKey(setId)}.{Math.Max(1, colorIndex)}";
    }

    public static bool Draw(
        Graphics graphics,
        Image? tilesetImage,
        int tileSize,
        RectangleF destination,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        TilesetPlanDefinition? plan,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity)
    {
        if (tilesetImage is null || plan?.Advanced?.WangSets is null || tileSize <= 1 || !TryParseTerrainId(terrain.Id, out var setKey, out var colorIndex))
        {
            return false;
        }

        var set = plan.Advanced.WangSets.FirstOrDefault(v => string.Equals(SetKey(v.Id), setKey, StringComparison.OrdinalIgnoreCase));
        if (set is null)
        {
            return false;
        }

        var desired = BuildDesiredWangId(set.Type, setKey, colorIndex, lookup, cell);
        var candidate = PickBestTile(set, desired, plan.Advanced, cell.X, cell.Y);
        if (candidate is null)
        {
            return false;
        }

        var sourceX = candidate.Tile.TileX * tileSize;
        var sourceY = candidate.Tile.TileY * tileSize;
        if (sourceX < 0 || sourceY < 0 || sourceX + tileSize > tilesetImage.Width || sourceY + tileSize > tilesetImage.Height)
        {
            return false;
        }

        using var tileBitmap = new Bitmap(tileSize, tileSize);
        using (var tileGraphics = Graphics.FromImage(tileBitmap))
        {
            tileGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            tileGraphics.PixelOffsetMode = PixelOffsetMode.Half;
            tileGraphics.DrawImage(
                tilesetImage,
                new Rectangle(0, 0, tileSize, tileSize),
                sourceX,
                sourceY,
                tileSize,
                tileSize,
                GraphicsUnit.Pixel);
        }

        ApplyTransform(tileBitmap, candidate.Transform);

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);
        graphics.DrawImage(tileBitmap, Rectangle.Round(destination), 0, 0, tileSize, tileSize, GraphicsUnit.Pixel, attributes);
        return true;
    }

    public static string SetKey(string? setId)
    {
        var source = string.IsNullOrWhiteSpace(setId) ? "default" : setId.Trim();
        var chars = source
            .Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_')
            .ToArray();
        var key = new string(chars).Trim('_');
        return string.IsNullOrWhiteSpace(key) ? "default" : key;
    }

    private static bool TryParseTerrainId(string? terrainId, out string setKey, out int colorIndex)
    {
        setKey = "";
        colorIndex = 0;
        if (string.IsNullOrWhiteSpace(terrainId) || !terrainId.StartsWith(TerrainPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var body = terrainId[TerrainPrefix.Length..];
        var lastDot = body.LastIndexOf('.');
        if (lastDot <= 0 || lastDot >= body.Length - 1)
        {
            return false;
        }

        setKey = body[..lastDot];
        return int.TryParse(body[(lastDot + 1)..], out colorIndex) && colorIndex > 0;
    }

    private static int[] BuildDesiredWangId(
        string setType,
        string setKey,
        int colorIndex,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        MapTileCell cell)
    {
        var result = new int[8];

        if (setType is TilesetWangSetTypes.Edge or TilesetWangSetTypes.Mixed)
        {
            result[Top] = HorizontalEdgeColor(lookup, setKey, cell.X, cell.Y);
            result[Right] = VerticalEdgeColor(lookup, setKey, cell.X + 1, cell.Y);
            result[Bottom] = HorizontalEdgeColor(lookup, setKey, cell.X, cell.Y + 1);
            result[Left] = VerticalEdgeColor(lookup, setKey, cell.X, cell.Y);
        }

        if (setType is TilesetWangSetTypes.Corner or TilesetWangSetTypes.Mixed)
        {
            result[TopRight] = VertexColor(lookup, setKey, cell.X + 1, cell.Y);
            result[BottomRight] = VertexColor(lookup, setKey, cell.X + 1, cell.Y + 1);
            result[BottomLeft] = VertexColor(lookup, setKey, cell.X, cell.Y + 1);
            result[TopLeft] = VertexColor(lookup, setKey, cell.X, cell.Y);
        }

        return MaskForSetType(result, setType);
    }

    private static int[] MaskForSetType(int[] wangId, string setType)
    {
        var result = new int[8];
        for (var index = 0; index < wangId.Length && index < result.Length; index++)
        {
            if (IsActivePosition(setType, index))
            {
                result[index] = wangId[index];
            }
        }

        return result;
    }

    private static int HorizontalEdgeColor(Dictionary<(int X, int Y), MapTileCell> lookup, string setKey, int x, int edgeY)
    {
        return FirstColor(
            ColorAt(lookup, x, edgeY, setKey),
            ColorAt(lookup, x, edgeY - 1, setKey));
    }

    private static int VerticalEdgeColor(Dictionary<(int X, int Y), MapTileCell> lookup, string setKey, int edgeX, int y)
    {
        return FirstColor(
            ColorAt(lookup, edgeX, y, setKey),
            ColorAt(lookup, edgeX - 1, y, setKey));
    }

    private static int VertexColor(Dictionary<(int X, int Y), MapTileCell> lookup, string setKey, int vertexX, int vertexY)
    {
        return FirstColor(
            ColorAt(lookup, vertexX, vertexY, setKey),
            ColorAt(lookup, vertexX - 1, vertexY, setKey),
            ColorAt(lookup, vertexX, vertexY - 1, setKey),
            ColorAt(lookup, vertexX - 1, vertexY - 1, setKey));
    }

    private static int ColorAt(Dictionary<(int X, int Y), MapTileCell> lookup, int x, int y, string setKey)
    {
        return lookup.TryGetValue((x, y), out var other)
            && TryParseTerrainId(other.TerrainId, out var otherSetKey, out var otherColorIndex)
            && string.Equals(otherSetKey, setKey, StringComparison.OrdinalIgnoreCase)
            ? otherColorIndex
            : 0;
    }

    private static int FirstColor(params int[] colors)
    {
        return colors.FirstOrDefault(color => color > 0);
    }

    private static WangTileCandidate? PickBestTile(
        TilesetWangSetDefinition set,
        int[] desired,
        TilesetAdvancedPlanDefinition advanced,
        int cellX,
        int cellY)
    {
        var activeIndexes = Enumerable.Range(0, 8)
            .Where(index => IsActivePosition(set.Type, index))
            .ToArray();
        var candidates = ExpandCandidates(set, advanced).ToList();
        var exactMatches = candidates
            .Where(tile => Matches(tile.WangId, desired, activeIndexes))
            .ToList();
        if (exactMatches.Count > 0)
        {
            return PickWeightedTile(set, exactMatches, advanced, cellX, cellY, desired, activeIndexes);
        }

        var bestScore = int.MinValue;
        var bestTiles = new List<WangTileCandidate>();
        foreach (var tile in candidates)
        {
            var score = Score(tile.WangId, desired, set.Type);
            if (score > bestScore)
            {
                bestScore = score;
                bestTiles.Clear();
                bestTiles.Add(tile);
            }
            else if (score == bestScore)
            {
                bestTiles.Add(tile);
            }
        }

        return bestTiles.Count <= 0
            ? null
            : PickWeightedTile(set, bestTiles, advanced, cellX, cellY, desired, activeIndexes);
    }

    private static WangTileCandidate PickWeightedTile(
        TilesetWangSetDefinition set,
        IReadOnlyList<WangTileCandidate> candidates,
        TilesetAdvancedPlanDefinition advanced,
        int cellX,
        int cellY,
        IReadOnlyList<int> desired,
        IReadOnlyList<int> activeIndexes)
    {
        var pool = advanced.PreferUntransformedTiles && candidates.Any(candidate => candidate.Transform == WangTransform.Identity)
            ? candidates.Where(candidate => candidate.Transform == WangTransform.Identity).ToList()
            : candidates.ToList();

        if (pool.Count == 1)
        {
            return pool[0];
        }

        var weightedCandidates = pool
            .Select(tile => new WeightedTile(tile, ComputeTileWeight(set, tile.Tile, desired, activeIndexes)))
            .Where(entry => entry.Weight > 0d)
            .ToList();
        if (weightedCandidates.Count <= 0)
        {
            return pool
                .OrderBy(tile => tile.Tile.TileY)
                .ThenBy(tile => tile.Tile.TileX)
                .First();
        }

        var totalWeight = weightedCandidates.Sum(entry => entry.Weight);
        if (totalWeight <= 0d)
        {
            return weightedCandidates[0].Tile;
        }

        var threshold = StableUnit(cellX, cellY, set.Id, desired) * totalWeight;
        var cumulative = 0d;
        foreach (var entry in weightedCandidates
                     .OrderBy(candidate => candidate.Tile.Tile.TileY)
                     .ThenBy(candidate => candidate.Tile.Tile.TileX)
                     .ThenBy(candidate => candidate.Tile.Transform))
        {
            cumulative += entry.Weight;
            if (threshold <= cumulative)
            {
                return entry.Tile;
            }
        }

        return weightedCandidates[^1].Tile;
    }

    private static IEnumerable<WangTileCandidate> ExpandCandidates(TilesetWangSetDefinition set, TilesetAdvancedPlanDefinition advanced)
    {
        foreach (var tile in set.Tiles)
        {
            foreach (var transform in AllowedTransforms(advanced))
            {
                yield return new WangTileCandidate(tile, transform, TransformWangId(tile.WangId, transform));
            }
        }
    }

    private static IReadOnlyList<WangTransform> AllowedTransforms(TilesetAdvancedPlanDefinition advanced)
    {
        var transforms = new HashSet<WangTransform> { WangTransform.Identity };
        if (advanced.AllowRotate && (advanced.AllowFlipHorizontally || advanced.AllowFlipVertically))
        {
            foreach (var transform in Enum.GetValues<WangTransform>())
            {
                transforms.Add(transform);
            }

            return transforms.OrderBy(value => value).ToArray();
        }

        if (advanced.AllowRotate)
        {
            transforms.Add(WangTransform.Rotate90);
            transforms.Add(WangTransform.Rotate180);
            transforms.Add(WangTransform.Rotate270);
        }

        if (advanced.AllowFlipHorizontally)
        {
            transforms.Add(WangTransform.FlipH);
        }

        if (advanced.AllowFlipVertically)
        {
            transforms.Add(WangTransform.FlipV);
        }

        if (advanced.AllowFlipHorizontally && advanced.AllowFlipVertically)
        {
            transforms.Add(WangTransform.Rotate180);
        }

        return transforms.OrderBy(value => value).ToArray();
    }

    private static List<int> TransformWangId(IReadOnlyList<int> source, WangTransform transform)
    {
        var values = source.Take(8).ToArray();
        Array.Resize(ref values, 8);
        var result = new int[8];
        for (var index = 0; index < 8; index++)
        {
            var vector = IndexToVector(index);
            var transformed = TransformVector(vector, transform);
            var targetIndex = VectorToIndex(transformed.X, transformed.Y);
            result[targetIndex] = values[index];
        }

        return result.ToList();
    }

    private static Point IndexToVector(int index)
    {
        return index switch
        {
            Top => new Point(0, -1),
            TopRight => new Point(1, -1),
            Right => new Point(1, 0),
            BottomRight => new Point(1, 1),
            Bottom => new Point(0, 1),
            BottomLeft => new Point(-1, 1),
            Left => new Point(-1, 0),
            _ => new Point(-1, -1)
        };
    }

    private static Point TransformVector(Point vector, WangTransform transform)
    {
        return transform switch
        {
            WangTransform.FlipH => new Point(-vector.X, vector.Y),
            WangTransform.FlipV => new Point(vector.X, -vector.Y),
            WangTransform.Rotate90 => new Point(-vector.Y, vector.X),
            WangTransform.Rotate180 => new Point(-vector.X, -vector.Y),
            WangTransform.Rotate270 => new Point(vector.Y, -vector.X),
            WangTransform.Diagonal => new Point(vector.Y, vector.X),
            WangTransform.AntiDiagonal => new Point(-vector.Y, -vector.X),
            _ => vector
        };
    }

    private static int VectorToIndex(int x, int y)
    {
        return (x, y) switch
        {
            (0, -1) => Top,
            (1, -1) => TopRight,
            (1, 0) => Right,
            (1, 1) => BottomRight,
            (0, 1) => Bottom,
            (-1, 1) => BottomLeft,
            (-1, 0) => Left,
            _ => TopLeft
        };
    }

    private static void ApplyTransform(Bitmap bitmap, WangTransform transform)
    {
        var rotateFlip = transform switch
        {
            WangTransform.FlipH => RotateFlipType.RotateNoneFlipX,
            WangTransform.FlipV => RotateFlipType.RotateNoneFlipY,
            WangTransform.Rotate90 => RotateFlipType.Rotate90FlipNone,
            WangTransform.Rotate180 => RotateFlipType.Rotate180FlipNone,
            WangTransform.Rotate270 => RotateFlipType.Rotate270FlipNone,
            WangTransform.Diagonal => RotateFlipType.Rotate90FlipX,
            WangTransform.AntiDiagonal => RotateFlipType.Rotate270FlipX,
            _ => RotateFlipType.RotateNoneFlipNone
        };
        bitmap.RotateFlip(rotateFlip);
    }

    private static double ComputeTileWeight(
        TilesetWangSetDefinition set,
        TilesetWangTileDefinition tile,
        IReadOnlyList<int> desired,
        IReadOnlyList<int> activeIndexes)
    {
        var tileProbability = Math.Max(0d, tile.Probability);
        if (tileProbability <= 0d)
        {
            return 0d;
        }

        var terrainProbability = 1d;
        foreach (var index in activeIndexes)
        {
            var colorIndex = index < tile.WangId.Count && tile.WangId[index] > 0
                ? tile.WangId[index]
                : index < desired.Count
                    ? desired[index]
                    : 0;
            if (colorIndex <= 0)
            {
                continue;
            }

            var colorProbability = set.Colors.FirstOrDefault(color => color.Index == colorIndex)?.Probability ?? 1d;
            terrainProbability *= Math.Max(0d, colorProbability);
        }

        return tileProbability * terrainProbability;
    }

    private static double StableUnit(int cellX, int cellY, string setId, IReadOnlyList<int> desired)
    {
        unchecked
        {
            uint hash = 2166136261;
            hash = Mix(hash, (uint)cellX);
            hash = Mix(hash, (uint)cellY);
            foreach (var ch in setId)
            {
                hash = Mix(hash, ch);
            }

            foreach (var value in desired)
            {
                hash = Mix(hash, (uint)value);
            }

            return hash / (double)uint.MaxValue;
        }
    }

    private static uint Mix(uint hash, uint value)
    {
        unchecked
        {
            hash ^= value;
            return hash * 16777619;
        }
    }

    private static bool Matches(IReadOnlyList<int> actual, IReadOnlyList<int> desired, IReadOnlyList<int> activeIndexes)
    {
        foreach (var index in activeIndexes)
        {
            var actualValue = index < actual.Count ? actual[index] : 0;
            if (actualValue != desired[index])
            {
                return false;
            }
        }

        return true;
    }

    private static int Score(IReadOnlyList<int> actual, IReadOnlyList<int> desired, string setType)
    {
        var score = 0;
        for (var index = 0; index < 8; index++)
        {
            if (!IsActivePosition(setType, index))
            {
                continue;
            }

            var actualValue = index < actual.Count ? actual[index] : 0;
            var desiredValue = desired[index];
            if (actualValue == desiredValue)
            {
                score += 6;
            }
            else if (actualValue == 0 || desiredValue == 0)
            {
                score -= 2;
            }
            else
            {
                score -= 5;
            }
        }

        return score;
    }

    private static bool IsActivePosition(string setType, int index)
    {
        return setType switch
        {
            TilesetWangSetTypes.Corner => index % 2 == 1,
            TilesetWangSetTypes.Edge => index % 2 == 0,
            _ => true
        };
    }

    private sealed record WeightedTile(WangTileCandidate Tile, double Weight);

    private sealed record WangTileCandidate(TilesetWangTileDefinition Tile, WangTransform Transform, IReadOnlyList<int> WangId);

    private enum WangTransform
    {
        Identity,
        FlipH,
        FlipV,
        Rotate90,
        Rotate180,
        Rotate270,
        Diagonal,
        AntiDiagonal
    }
}
