using System.Drawing.Drawing2D;
using Axe2DEditor.Core.Maps;

namespace Axe2DEditor.Editor.Controls;

public sealed class TilesetPalettePanel : Panel
{
    private Image? _image;
    private int _tileSize = 32;
    private Point _selectedTile = new(-1, -1);
    private Size _selectedTileSize = new(1, 1);
    private Point _highlightBlockOrigin = new(-1, -1);
    private Size _highlightBlockSize = Size.Empty;
    private TilesetPlanDefinition? _tilesetPlan;
    private List<TilesetRegionDefinition> _plannedRegions = [];
    private List<TilesetPaletteEntry> _foldedEntries = [];
    private int _foldedColumns = 1;
    private bool _dragSelecting;
    private bool _showShiftAutoTerrainMode;
    private Point _dragStartTile = new(-1, -1);

    public event EventHandler<TilesetTileSelectedEventArgs>? TileSelected;

    public Point SelectedTile
    {
        get => _selectedTile;
        set
        {
            _selectedTile = value;
            Invalidate();
        }
    }

    public Point HighlightBlockOrigin
    {
        get => _highlightBlockOrigin;
        set
        {
            _highlightBlockOrigin = value;
            Invalidate();
        }
    }

    public Size HighlightBlockSize
    {
        get => _highlightBlockSize;
        set
        {
            _highlightBlockSize = value;
            Invalidate();
        }
    }

    public bool ShowShiftAutoTerrainMode
    {
        get => _showShiftAutoTerrainMode;
        set
        {
            if (_showShiftAutoTerrainMode == value)
            {
                return;
            }

            _showShiftAutoTerrainMode = value;
            Invalidate();
        }
    }

    public void SetPlannedRegions(IEnumerable<TilesetRegionDefinition>? regions)
    {
        SetTilesetPlan(regions is null
            ? null
            : new TilesetPlanDefinition { Regions = CloneRegions(regions) });
    }

    public void SetTilesetPlan(TilesetPlanDefinition? plan)
    {
        _tilesetPlan = plan is null
            ? null
            : new TilesetPlanDefinition
            {
                TileSize = plan.TileSize,
                Mode = plan.Mode,
                RpgMakerKind = plan.RpgMakerKind,
                RpgMakerLayout = plan.RpgMakerLayout,
                Regions = CloneRegions(plan.Regions)
            };
        _plannedRegions = _tilesetPlan?.Regions ?? [];
        RebuildFoldedEntries();
        Invalidate();
    }

    public TilesetPalettePanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
        AutoScroll = true;
        BackColor = Color.FromArgb(34, 34, 34);
    }

    public void SetTileset(Image? image, int tileSize)
    {
        _image?.Dispose();
        _image = image;
        _tileSize = Math.Max(8, tileSize);
        AutoScrollMinSize = _image is null ? Size.Empty : new Size(_image.Width + 1, _image.Height + 1);
        _selectedTile = new Point(-1, -1);
            _selectedTileSize = new Size(1, 1);
        _highlightBlockOrigin = new Point(-1, -1);
        _highlightBlockSize = Size.Empty;
        RebuildFoldedEntries();
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_image is null || e.Button != MouseButtons.Left)
        {
            return;
        }

        if (UseFoldedRpgAutoPalette())
        {
            SelectFoldedEntry(e.Location);
            return;
        }

        var tile = HitTile(e.Location);
        if (IsIgnoredTile(tile))
        {
            return;
        }

        _dragSelecting = true;
        _dragStartTile = tile;
        SelectTileRange(tile, tile);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragSelecting || _image is null || UseFoldedRpgAutoPalette())
        {
            return;
        }

        var tile = HitTile(e.Location);
        if (tile.X < 0 || tile.Y < 0)
        {
            return;
        }

        SelectTileRange(_dragStartTile, tile);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragSelecting = false;
    }

    private void SelectTileRange(Point start, Point end)
    {
        if (_image is null)
        {
            return;
        }

        var imageColumns = Math.Max(1, _image.Width / _tileSize);
        var imageRows = Math.Max(1, _image.Height / _tileSize);
        var left = Math.Clamp(Math.Min(start.X, end.X), 0, imageColumns - 1);
        var top = Math.Clamp(Math.Min(start.Y, end.Y), 0, imageRows - 1);
        var right = Math.Clamp(Math.Max(start.X, end.X), 0, imageColumns - 1);
        var bottom = Math.Clamp(Math.Max(start.Y, end.Y), 0, imageRows - 1);
        var tile = new Point(left, top);
        _selectedTile = tile;
        _selectedTileSize = new Size(right - left + 1, bottom - top + 1);
        TileSelected?.Invoke(this, new TilesetTileSelectedEventArgs(tile.X, tile.Y, _selectedTileSize.Width, _selectedTileSize.Height));
        Invalidate();
    }

    private Point HitTile(Point point)
    {
        if (_image is null)
        {
            return new Point(-1, -1);
        }

        var x = point.X - AutoScrollPosition.X;
        var y = point.Y - AutoScrollPosition.Y;
        if (x < 0 || y < 0 || x >= _image.Width || y >= _image.Height)
        {
            return new Point(-1, -1);
        }

        return new Point(x / _tileSize, y / _tileSize);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        g.SmoothingMode = SmoothingMode.None;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        if (_image is null)
        {
            DrawEmpty(g);
            return;
        }

        if (UseFoldedRpgAutoPalette())
        {
            DrawFoldedRpgAutoPalette(g);
            return;
        }

        var offset = AutoScrollPosition;
        g.DrawImage(_image, offset.X, offset.Y, _image.Width, _image.Height);
        DrawPlannedRegions(g, offset);
        DrawGrid(g, offset);
        DrawHighlightBlock(g, offset);
        DrawSelection(g, offset);
    }

    private void DrawEmpty(Graphics g)
    {
        using var brush = new SolidBrush(Color.FromArgb(190, 190, 190));
        using var font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        const string text = "导入图集后选择瓦片";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, (Width - size.Width) / 2f, (Height - size.Height) / 2f);
    }

    private void SelectFoldedEntry(Point location)
    {
        var offset = AutoScrollPosition;
        var local = new Point(location.X - offset.X, location.Y - offset.Y);
        foreach (var entry in _foldedEntries)
        {
            if (!entry.DisplayRect.Contains(local))
            {
                continue;
            }

            _selectedTile = entry.Tile;
            _selectedTileSize = entry.SourceSize;
            TileSelected?.Invoke(this, new TilesetTileSelectedEventArgs(entry.Tile.X, entry.Tile.Y, entry.SourceSize.Width, entry.SourceSize.Height));
            Invalidate();
            return;
        }
    }

    private void DrawFoldedRpgAutoPalette(Graphics g)
    {
        var offset = AutoScrollPosition;
        foreach (var entry in _foldedEntries)
        {
            var dest = new Rectangle(
                offset.X + entry.DisplayRect.X,
                offset.Y + entry.DisplayRect.Y,
                entry.DisplayRect.Width,
                entry.DisplayRect.Height);
            if (entry.PreviewAsWaterfall)
            {
                DrawWaterfallPreview(g, dest, entry.PreviewTile);
            }
            else
            {
                var source = new Rectangle(
                    entry.PreviewTile.X * _tileSize,
                    entry.PreviewTile.Y * _tileSize,
                    entry.SourceSize.Width * _tileSize,
                    entry.SourceSize.Height * _tileSize);
                g.DrawImage(_image!, dest, source, GraphicsUnit.Pixel);
            }
            DrawFoldedEntryFrame(g, dest, entry);
        }

        if (_foldedEntries.Count <= 0)
        {
            using var brush = new SolidBrush(Color.FromArgb(190, 190, 190));
            using var font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            const string text = "RM 自动元件尚未生成区域";
            var size = g.MeasureString(text, font);
            g.DrawString(text, font, brush, (Width - size.Width) / 2f, (Height - size.Height) / 2f);
        }
    }

    private void DrawFoldedEntryFrame(Graphics g, Rectangle rect, TilesetPaletteEntry entry)
    {
        using var gridPen = new Pen(Color.FromArgb(120, 255, 255, 255));
        g.DrawRectangle(gridPen, rect);

        if (entry.Kind == TilesetRegionKinds.RpgMakerA1 || entry.Kind == TilesetRegionKinds.RpgMakerA2)
        {
            using var tagBack = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
            using var tagBrush = new SolidBrush(Color.White);
            using var tagFont = new Font("Microsoft YaHei UI", 7F, FontStyle.Bold);
            var text = entry.Kind == TilesetRegionKinds.RpgMakerA1 ? A1EntryLabel(entry.Variant) : "A2";
            var tagSize = g.MeasureString(text, tagFont);
            g.FillRectangle(tagBack, rect.Left + 3, rect.Top + 3, Math.Max(18, (int)Math.Ceiling(tagSize.Width) + 8), 16);
            g.DrawString(text, tagFont, tagBrush, rect.Left + 6, rect.Top + 3);
        }

        if (_selectedTile == entry.Tile)
        {
            var color = _showShiftAutoTerrainMode
                ? Color.FromArgb(255, 255, 185, 0)
                : Color.FromArgb(255, 0, 120, 215);
            using var fill = new SolidBrush(Color.FromArgb(_showShiftAutoTerrainMode ? 64 : 48, color));
            using var pen = new Pen(color, 3f);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect);
            return;
        }

        if (_highlightBlockOrigin == entry.Tile && (entry.Kind == TilesetRegionKinds.RpgMakerA1 || entry.Kind == TilesetRegionKinds.RpgMakerA2))
        {
            using var fill = new SolidBrush(Color.FromArgb(38, 255, 170, 0));
            using var pen = new Pen(Color.FromArgb(255, 170, 0), 3f);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect);
        }
    }

    private void DrawWaterfallPreview(Graphics g, Rectangle dest, Point previewTile)
    {
        if (_image is null)
        {
            return;
        }

        var half = _tileSize / 2;
        var sourceTileX = previewTile.X * _tileSize;
        var sourceTileY = previewTile.Y * _tileSize;
        Span<Point> quarters =
        [
            new Point(0, 0),
            new Point(3, 0),
            new Point(0, 1),
            new Point(3, 1)
        ];

        for (var index = 0; index < quarters.Length; index++)
        {
            var source = quarters[index];
            var sourceRect = new Rectangle(sourceTileX + source.X * half, sourceTileY + source.Y * half, half, half);
            g.DrawImage(_image, GetDestinationQuarter(dest, index), sourceRect, GraphicsUnit.Pixel);
        }
    }

    private static Rectangle GetDestinationQuarter(Rectangle destination, int index)
    {
        var left = index % 2 == 0 ? destination.Left : destination.Left + destination.Width / 2;
        var top = index < 2 ? destination.Top : destination.Top + destination.Height / 2;
        var right = index % 2 == 0 ? destination.Left + destination.Width / 2 : destination.Right;
        var bottom = index < 2 ? destination.Top + destination.Height / 2 : destination.Bottom;
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private void DrawGrid(Graphics g, Point offset)
    {
        if (_image is null)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(110, 255, 255, 255));
        for (var x = 0; x <= _image.Width; x += _tileSize)
        {
            g.DrawLine(pen, offset.X + x, offset.Y, offset.X + x, offset.Y + _image.Height);
        }

        for (var y = 0; y <= _image.Height; y += _tileSize)
        {
            g.DrawLine(pen, offset.X, offset.Y + y, offset.X + _image.Width, offset.Y + y);
        }
    }

    private void DrawSelection(Graphics g, Point offset)
    {
        if (_image is null || _selectedTile.X < 0 || _selectedTile.Y < 0)
        {
            return;
        }

        var color = _showShiftAutoTerrainMode
            ? Color.FromArgb(255, 255, 185, 0)
            : Color.FromArgb(255, 0, 120, 215);
        using var fill = new SolidBrush(Color.FromArgb(_showShiftAutoTerrainMode ? 64 : 48, color));
        using var pen = new Pen(color, 3f);
        var rect = new Rectangle(
            offset.X + _selectedTile.X * _tileSize,
            offset.Y + _selectedTile.Y * _tileSize,
            _selectedTileSize.Width * _tileSize,
            _selectedTileSize.Height * _tileSize);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(pen, rect);
    }

    private void DrawPlannedRegions(Graphics g, Point offset)
    {
        foreach (var region in _plannedRegions)
        {
            if (region.Width <= 0 || region.Height <= 0)
            {
                continue;
            }

            var color = RegionColor(region.Kind);
            var rect = new Rectangle(
                offset.X + region.X * _tileSize,
                offset.Y + region.Y * _tileSize,
                region.Width * _tileSize,
                region.Height * _tileSize);
            using var fill = new SolidBrush(Color.FromArgb(34, color));
            using var pen = new Pen(color, 2f);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect);
            DrawRegionLabel(g, rect, region.Kind);
        }
    }

    private void DrawHighlightBlock(Graphics g, Point offset)
    {
        if (_image is null || _highlightBlockOrigin.X < 0 || _highlightBlockOrigin.Y < 0 || _highlightBlockSize.Width <= 0 || _highlightBlockSize.Height <= 0)
        {
            return;
        }

        using var fill = new SolidBrush(Color.FromArgb(36, 255, 170, 0));
        using var pen = new Pen(Color.FromArgb(255, 255, 170, 0), 3f);
        var rect = new Rectangle(
            offset.X + _highlightBlockOrigin.X * _tileSize,
            offset.Y + _highlightBlockOrigin.Y * _tileSize,
            _highlightBlockSize.Width * _tileSize,
            _highlightBlockSize.Height * _tileSize);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(pen, rect);
    }

    private bool IsIgnoredTile(Point tile)
    {
        return _plannedRegions.Any(region =>
            string.Equals(region.Kind, TilesetRegionKinds.Ignored, StringComparison.OrdinalIgnoreCase)
            && tile.X >= region.X
            && tile.Y >= region.Y
            && tile.X < region.X + region.Width
            && tile.Y < region.Y + region.Height);
    }

    private void RebuildFoldedEntries()
    {
        _foldedEntries = [];
        if (_image is null || !UseFoldedRpgAutoPalette())
        {
            AutoScrollMinSize = _image is null ? Size.Empty : new Size(_image.Width + 1, _image.Height + 1);
            return;
        }

        var imageColumns = Math.Max(1, _image.Width / _tileSize);
        var imageRows = Math.Max(1, _image.Height / _tileSize);
        _foldedColumns = Math.Max(1, imageColumns / 2);
        var autoRegions = _plannedRegions
            .Where(IsRpgAutoRegion)
            .OrderBy(v => v.Y)
            .ThenBy(v => v.X)
            .ToList();

        var column = 0;
        var row = 0;

        foreach (var region in autoRegions)
        {
            if (column >= _foldedColumns)
            {
                column = 0;
                row++;
            }

            var preview = PreviewTileForRegion(region, imageColumns, imageRows);
            _foldedEntries.Add(new TilesetPaletteEntry(
                new Point(region.X, region.Y),
                preview,
                new Size(1, 1),
                region.Kind,
                region.Variant,
                EntryRect(column, row, 1, 1),
                IsWaterfallRegion(region)));
            column++;
        }

        var normalTop = autoRegions.Count > 0 ? row + 1 : 0;

        foreach (var tile in EnumerateNormalRegionTiles(imageColumns, imageRows))
        {
            _foldedEntries.Add(new TilesetPaletteEntry(tile, tile, new Size(1, 1), TilesetRegionKinds.Normal, string.Empty, NormalEntryRect(tile, normalTop), false));
        }

        var contentRight = _foldedEntries.Count == 0 ? _tileSize : _foldedEntries.Max(v => v.DisplayRect.Right);
        var contentBottom = _foldedEntries.Count == 0 ? _tileSize : _foldedEntries.Max(v => v.DisplayRect.Bottom);
        AutoScrollMinSize = new Size(contentRight + 1, contentBottom + 1);
    }

    private IEnumerable<Point> EnumerateNormalRegionTiles(int imageColumns, int imageRows)
    {
        foreach (var region in _plannedRegions.Where(v => string.Equals(v.Kind, TilesetRegionKinds.Normal, StringComparison.OrdinalIgnoreCase)))
        {
            for (var y = Math.Max(0, region.Y); y < region.Y + region.Height && y < imageRows; y++)
            {
                for (var x = Math.Max(0, region.X); x < region.X + region.Width && x < imageColumns; x++)
                {
                    yield return new Point(x, y);
                }
            }
        }
    }

    private Rectangle EntryRect(int column, int row, int width, int height)
    {
        return new Rectangle(column * _tileSize, row * _tileSize, width * _tileSize, height * _tileSize);
    }

    private Rectangle NormalEntryRect(Point tile, int topRows)
    {
        return new Rectangle(tile.X * _tileSize, (topRows + tile.Y) * _tileSize, _tileSize, _tileSize);
    }

    private bool UseFoldedRpgAutoPalette()
    {
        return _tilesetPlan is not null
            && _plannedRegions.Any(IsRpgAutoRegion);
    }

    private static bool IsRpgAutoRegion(TilesetRegionDefinition region)
    {
        return string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase)
            || string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA2, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWaterfallRegion(TilesetRegionDefinition region)
    {
        return string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase)
            && string.Equals(region.Variant, RpgMakerA1RegionVariants.Waterfall, StringComparison.OrdinalIgnoreCase);
    }

    private static Point PreviewTileForRegion(TilesetRegionDefinition region, int imageColumns, int imageRows)
    {
        var x = region.X;
        var y = region.Y;
        if (string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase))
        {
            x = region.Variant == RpgMakerA1RegionVariants.Waterfall ? region.X : region.X + Math.Min(2, region.Width - 1);
            y = region.Y + Math.Min(1, region.Height - 1);
        }

        return new Point(
            Math.Clamp(x, 0, imageColumns - 1),
            Math.Clamp(y, 0, imageRows - 1));
    }

    private static string A1EntryLabel(string variant)
    {
        return variant switch
        {
            RpgMakerA1RegionVariants.Ocean => "A",
            RpgMakerA1RegionVariants.DeepSea => "B",
            RpgMakerA1RegionVariants.OceanDecor => "C",
            RpgMakerA1RegionVariants.Waterfall => "E",
            _ => "D"
        };
    }

    private static List<TilesetRegionDefinition> CloneRegions(IEnumerable<TilesetRegionDefinition>? regions)
    {
        return regions?
            .Select(v => new TilesetRegionDefinition
            {
                Id = v.Id,
                Name = v.Name,
                Kind = v.Kind,
                Variant = v.Variant,
                X = v.X,
                Y = v.Y,
                Width = v.Width,
                Height = v.Height
            })
            .ToList() ?? [];
    }

    private static Color RegionColor(string kind)
    {
        return kind switch
        {
            TilesetRegionKinds.RpgMakerA1 => Color.FromArgb(255, 78, 172, 255),
            TilesetRegionKinds.RpgMakerA2 => Color.FromArgb(255, 229, 126, 32),
            TilesetRegionKinds.Ignored => Color.FromArgb(255, 150, 150, 150),
            _ => Color.FromArgb(255, 38, 166, 91)
        };
    }

    private static void DrawRegionLabel(Graphics g, Rectangle rect, string kind)
    {
        if (rect.Width < 34 || rect.Height < 20)
        {
            return;
        }

        using var font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold);
        using var backBrush = new SolidBrush(Color.FromArgb(170, 0, 0, 0));
        using var textBrush = new SolidBrush(Color.White);
        var text = kind == TilesetRegionKinds.RpgMakerA1
            ? "A1"
            : kind == TilesetRegionKinds.RpgMakerA2
                ? "A2"
                : kind == TilesetRegionKinds.Ignored ? "忽略" : "普通";
        var size = g.MeasureString(text, font);
        var labelRect = new RectangleF(rect.Left + 4, rect.Top + 4, size.Width + 8, size.Height + 4);
        g.FillRectangle(backBrush, labelRect);
        g.DrawString(text, font, textBrush, labelRect.Left + 4, labelRect.Top + 2);
    }
}

internal sealed record TilesetPaletteEntry(Point Tile, Point PreviewTile, Size SourceSize, string Kind, string Variant, Rectangle DisplayRect, bool PreviewAsWaterfall);

public sealed class TilesetTileSelectedEventArgs : EventArgs
{
    public TilesetTileSelectedEventArgs(int tileX, int tileY, int width = 1, int height = 1)
    {
        TileX = tileX;
        TileY = tileY;
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);
    }

    public int TileX { get; }

    public int TileY { get; }

    public int Width { get; }

    public int Height { get; }
}
