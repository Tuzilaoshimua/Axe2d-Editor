using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Axe2DEditor.Core.Maps;

namespace Axe2DEditor.Editor.Controls;

public sealed class MapCanvasPanel : Panel
{
    private const float MinZoom = 0.2f;
    private const float MaxZoom = 6f;
    private const float DefaultZoom = 1f;
    private const float A1OverlayFrameMinWaterRatio = 0.30f;
    private const float A1OverlayFrameMinForegroundRatio = 0.18f;
    private static readonly int[] A1OverlayWaterSurfaceSequence = [0, 1, 2, 1];

    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 160 };
    private readonly Dictionary<A1OverlayFrameKey, bool> _a1OverlayFrameCache = new();

    private readonly record struct A1OverlayFrameKey(int SourceX, int SourceY, int TileSize);

    private MapDefinition? _map;
    private MapLayerDefinition? _activeLayer;
    private MapTerrainDefinition? _selectedTerrain;
    private MapTerrainDefinition? _shiftTerrain;
    private Image? _tilesetImage;
    private Point _selectedTilesetTile = new(-1, -1);
    private Size _selectedTilesetTileSize = new(1, 1);
    private bool _selectedTilesetTileIsA1Overlay;
    private bool _selectedTilesetTileIsA1OverlayAnimated;
    private bool _shiftAutoTerrainMode;
    private MapBrushSource _brushSource = MapBrushSource.Terrain;
    private MapEditorTool _activeTool = MapEditorTool.Paint;
    private bool _showGrid = true;
    private bool _showAutoEdges = true;
    private bool _isMouseDown;
    private bool _isPanning;
    private bool _isRectSelecting;
    private Point _panStart;
    private Point _hoverCell = new(-1, -1);
    private Point _rectStart = new(-1, -1);
    private Point _rectEnd = new(-1, -1);
    private float _cameraX;
    private float _cameraY;
    private float _zoom = DefaultZoom;
    private int _animationTick;

    public event EventHandler? MapChanged;
    public event EventHandler<MapTileHoverEventArgs>? TileHovered;
    public event EventHandler<MapTerrainPickedEventArgs>? TerrainPicked;
    public event EventHandler<TilesetTileSelectedEventArgs>? TilesetTilePicked;

    public MapBrushSource BrushSource => _brushSource;

    public MapEditorTool ActiveTool
    {
        get => _activeTool;
        set
        {
            _activeTool = value;
            _isRectSelecting = false;
            Invalidate();
        }
    }

    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            _showGrid = value;
            Invalidate();
        }
    }

    public bool ShowAutoEdges
    {
        get => _showAutoEdges;
        set
        {
            _showAutoEdges = value;
            Invalidate();
        }
    }

    public MapCanvasPanel()
    {
        SetStyle(ControlStyles.Selectable, true);
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
        Cursor = Cursors.Cross;
        BackColor = Color.FromArgb(30, 30, 30);
        _animationTimer.Tick += (_, _) =>
        {
            _animationTick++;
            if (HasAnimatedTerrain())
            {
                Invalidate();
            }
        };
        _animationTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
            ClearA1OverlayFrameCache();
            _tilesetImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    public void SetMap(MapDefinition? map)
    {
        _map = map;
        if (_map is not null)
        {
            MapDefaults.Normalize(_map);
            _cameraX = _map.Width / 2f;
            _cameraY = _map.Height / 2f;
            _zoom = DefaultZoom;
        }

        Invalidate();
    }

    public void SetActiveLayer(MapLayerDefinition? layer)
    {
        _activeLayer = layer;
        Invalidate();
    }

    public void SetSelectedTerrain(MapTerrainDefinition? terrain)
    {
        _selectedTerrain = terrain;
        Invalidate();
    }

    public void UseTerrainBrush()
    {
        _brushSource = MapBrushSource.Terrain;
        Invalidate();
    }

    public void SetShiftTerrain(MapTerrainDefinition? terrain)
    {
        _shiftTerrain = terrain;
    }

    public void SetShiftAutoTerrainMode(bool enabled)
    {
        if (_shiftAutoTerrainMode == enabled)
        {
            return;
        }

        _shiftAutoTerrainMode = enabled;
        Invalidate();
    }

    public void SetTilesetImage(Image? image)
    {
        ClearA1OverlayFrameCache();
        _tilesetImage?.Dispose();
        _tilesetImage = image;
        Invalidate();
    }

    public void SetSelectedTilesetTile(int tileX, int tileY)
    {
        SetTilesetSelection(tileX, tileY);
    }

    public void SetTilesetSelection(int tileX, int tileY)
    {
        SetTilesetSelection(tileX, tileY, 1, 1);
    }

    public void SetTilesetSelection(int tileX, int tileY, int width, int height)
    {
        SetTilesetSelection(tileX, tileY, width, height, isA1Overlay: false, isA1OverlayAnimated: false);
    }

    public void SetTilesetSelection(int tileX, int tileY, int width, int height, bool isA1Overlay, bool isA1OverlayAnimated)
    {
        _selectedTilesetTile = new Point(tileX, tileY);
        _selectedTilesetTileSize = new Size(Math.Max(1, width), Math.Max(1, height));
        _selectedTilesetTileIsA1Overlay = isA1Overlay;
        _selectedTilesetTileIsA1OverlayAnimated = isA1OverlayAnimated;
        Invalidate();
    }

    public void UseTilesetBrush()
    {
        if (_selectedTilesetTile.X < 0 || _selectedTilesetTile.Y < 0)
        {
            return;
        }

        _brushSource = MapBrushSource.Tileset;
        Invalidate();
    }

    public void ResetView()
    {
        if (_map is null)
        {
            return;
        }

        _cameraX = _map.Width / 2f;
        _cameraY = _map.Height / 2f;
        _zoom = DefaultZoom;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Focus();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        Focus();
        if (_map is null)
        {
            return;
        }

        if (e.Button is MouseButtons.Right or MouseButtons.Middle)
        {
            _isPanning = true;
            _panStart = e.Location;
            Cursor = Cursors.SizeAll;
            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var cell = ScreenToCell(e.Location);
        if (!IsInside(cell))
        {
            return;
        }

        _isMouseDown = true;
        if (_activeTool == MapEditorTool.Rectangle)
        {
            _isRectSelecting = true;
            _rectStart = cell;
            _rectEnd = cell;
            Invalidate();
            return;
        }

        ApplyTool(cell);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_map is null)
        {
            return;
        }

        if (_isPanning)
        {
            var tilePixels = TilePixels;
            _cameraX -= (e.X - _panStart.X) / tilePixels;
            _cameraY -= (e.Y - _panStart.Y) / tilePixels;
            _panStart = e.Location;
            Invalidate();
            return;
        }

        var cell = ScreenToCell(e.Location);
        if (cell != _hoverCell)
        {
            _hoverCell = cell;
            TileHovered?.Invoke(this, new MapTileHoverEventArgs(cell.X, cell.Y, IsInside(cell)));
            Invalidate();
        }

        if (!_isMouseDown || e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_activeTool == MapEditorTool.Rectangle && _isRectSelecting)
        {
            _rectEnd = cell;
            Invalidate();
            return;
        }

        if (_activeTool is MapEditorTool.Paint or MapEditorTool.Erase)
        {
            ApplyTool(cell);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button is MouseButtons.Right or MouseButtons.Middle)
        {
            _isPanning = false;
            Cursor = Cursors.Cross;
            return;
        }

        if (!_isMouseDown)
        {
            return;
        }

        if (_activeTool == MapEditorTool.Rectangle && _isRectSelecting)
        {
            _rectEnd = ScreenToCell(e.Location);
            PaintRectangle(_rectStart, _rectEnd);
            _isRectSelecting = false;
            _rectStart = new Point(-1, -1);
            _rectEnd = new Point(-1, -1);
        }

        _isMouseDown = false;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverCell = new Point(-1, -1);
        TileHovered?.Invoke(this, new MapTileHoverEventArgs(-1, -1, false));
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_map is null)
        {
            return;
        }

        var before = ScreenToWorld(e.Location);
        _zoom = e.Delta > 0 ? _zoom * 1.12f : _zoom / 1.12f;
        _zoom = Math.Clamp(_zoom, MinZoom, MaxZoom);
        var after = ScreenToWorld(e.Location);
        _cameraX += before.X - after.X;
        _cameraY += before.Y - after.Y;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.None;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.Clear(Color.FromArgb(34, 34, 34));

        if (_map is null)
        {
            DrawEmpty(g);
            return;
        }

        var background = MapDefaults.ParseColor(_map.BackgroundColor, Color.FromArgb(31, 41, 55));
        using (var backBrush = new SolidBrush(background))
        {
            g.FillRectangle(backBrush, ClientRectangle);
        }

        DrawLayers(g);
        if (_showGrid)
        {
            DrawGrid(g);
        }

        DrawMapBorder(g);
        DrawBrushPreview(g);
        DrawHud(g);
    }

    private void DrawEmpty(Graphics g)
    {
        using var brush = new SolidBrush(Color.FromArgb(190, 190, 190));
        using var font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        const string text = "请选择或创建一张地图";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, (Width - size.Width) / 2f, (Height - size.Height) / 2f);
    }

    private void DrawLayers(Graphics g)
    {
        if (_map is null)
        {
            return;
        }

        var terrains = _map.Terrains.ToDictionary(v => v.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var layer in _map.Layers.Where(v => v.Visible))
        {
            if (layer.Kind.Equals("Tile", StringComparison.OrdinalIgnoreCase))
            {
                DrawTileLayer(g, layer, terrains);
            }
            else if (layer.Kind.Equals("Collision", StringComparison.OrdinalIgnoreCase))
            {
                DrawCollisionLayer(g, layer);
            }
            else if (layer.Kind.Equals("Region", StringComparison.OrdinalIgnoreCase))
            {
                DrawRegionLayer(g, layer);
            }
        }
    }

    private void DrawTileLayer(Graphics g, MapLayerDefinition layer, Dictionary<string, MapTerrainDefinition> terrains)
    {
        if (_map is null)
        {
            return;
        }

        var visible = GetVisibleRange();
        var lookup = BuildLookup(layer);
        foreach (var cell in layer.Tiles)
        {
            if (cell.X < visible.Left || cell.X > visible.Right || cell.Y < visible.Top || cell.Y > visible.Bottom)
            {
                continue;
            }

            terrains.TryGetValue(cell.TerrainId, out var terrain);

            var rect = CellToScreenRect(cell.X, cell.Y);
            var usesRpgMakerAutoTile = RpgMakerAutoTile.IsA1(terrain) || RpgMakerAutoTile.IsA2(terrain);
            var drawnFromTileset = RpgMakerAutoTile.IsA1(terrain)
                && terrain is not null
                && RpgMakerAutoTile.DrawA1(g, _tilesetImage, _map.TileSize, rect, cell, terrain, lookup, layer.Opacity, _animationTick);
            drawnFromTileset = drawnFromTileset || (RpgMakerAutoTile.IsA2(terrain)
                && terrain is not null
                && RpgMakerAutoTile.DrawA2(g, _tilesetImage, _map.TileSize, rect, cell, terrain, lookup, layer.Opacity, _animationTick));
            if (!drawnFromTileset && !usesRpgMakerAutoTile)
            {
                drawnFromTileset = DrawTilesetTile(g, rect, cell, terrain, layer.Opacity);
            }

            DrawA1Overlay(g, rect, cell, layer, layer.Opacity);

            if (!drawnFromTileset && terrain is not null)
            {
                var baseColor = MapDefaults.ParseColor(terrain.ColorHex, Color.Gray);
                if (terrain.Animated)
                {
                    baseColor = AnimateColor(baseColor, terrain);
                }

                using var tileBrush = new SolidBrush(WithOpacity(baseColor, layer.Opacity));
                g.FillRectangle(tileBrush, rect);

                if (_showAutoEdges && terrain.Rule.StartsWith("Auto", StringComparison.OrdinalIgnoreCase))
                {
                    DrawAutoEdges(g, rect, cell, terrain, lookup, layer.Opacity);
                }
            }
        }
    }

    private void DrawAutoEdges(
        Graphics g,
        RectangleF rect,
        MapTileCell cell,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity)
    {
        var edgeColor = WithOpacity(MapDefaults.ParseColor(terrain.EdgeColorHex, Color.Black), Math.Min(1f, opacity * 0.88f));
        using var edgeBrush = new SolidBrush(edgeColor);
        var edge = Math.Max(2f, rect.Width * 0.16f);
        var sameN = IsSameTerrain(lookup, cell.X, cell.Y - 1, cell.TerrainId);
        var sameE = IsSameTerrain(lookup, cell.X + 1, cell.Y, cell.TerrainId);
        var sameS = IsSameTerrain(lookup, cell.X, cell.Y + 1, cell.TerrainId);
        var sameW = IsSameTerrain(lookup, cell.X - 1, cell.Y, cell.TerrainId);

        if (!sameN) g.FillRectangle(edgeBrush, rect.Left, rect.Top, rect.Width, edge);
        if (!sameS) g.FillRectangle(edgeBrush, rect.Left, rect.Bottom - edge, rect.Width, edge);
        if (!sameW) g.FillRectangle(edgeBrush, rect.Left, rect.Top, edge, rect.Height);
        if (!sameE) g.FillRectangle(edgeBrush, rect.Right - edge, rect.Top, edge, rect.Height);

        var cornerSize = edge * 1.35f;
        using var cornerBrush = new SolidBrush(WithOpacity(edgeColor, opacity));
        if (sameN && sameW && !IsSameTerrain(lookup, cell.X - 1, cell.Y - 1, cell.TerrainId))
        {
            g.FillPie(cornerBrush, rect.Left - cornerSize / 2f, rect.Top - cornerSize / 2f, cornerSize, cornerSize, 0, 90);
        }

        if (sameN && sameE && !IsSameTerrain(lookup, cell.X + 1, cell.Y - 1, cell.TerrainId))
        {
            g.FillPie(cornerBrush, rect.Right - cornerSize / 2f, rect.Top - cornerSize / 2f, cornerSize, cornerSize, 90, 90);
        }

        if (sameS && sameE && !IsSameTerrain(lookup, cell.X + 1, cell.Y + 1, cell.TerrainId))
        {
            g.FillPie(cornerBrush, rect.Right - cornerSize / 2f, rect.Bottom - cornerSize / 2f, cornerSize, cornerSize, 180, 90);
        }

        if (sameS && sameW && !IsSameTerrain(lookup, cell.X - 1, cell.Y + 1, cell.TerrainId))
        {
            g.FillPie(cornerBrush, rect.Left - cornerSize / 2f, rect.Bottom - cornerSize / 2f, cornerSize, cornerSize, 270, 90);
        }
    }

    private void DrawCollisionLayer(Graphics g, MapLayerDefinition layer)
    {
        var visible = GetVisibleRange();
        using var brush = new HatchBrush(
            HatchStyle.BackwardDiagonal,
            Color.FromArgb((int)(170 * layer.Opacity), 230, 70, 80),
            Color.FromArgb((int)(60 * layer.Opacity), 230, 70, 80));
        using var pen = new Pen(Color.FromArgb((int)(180 * layer.Opacity), 255, 120, 120), 1f);
        foreach (var cell in layer.Tiles.Where(v => v.Solid || !string.IsNullOrWhiteSpace(v.TerrainId)))
        {
            if (cell.X < visible.Left || cell.X > visible.Right || cell.Y < visible.Top || cell.Y > visible.Bottom)
            {
                continue;
            }

            var rect = CellToScreenRect(cell.X, cell.Y);
            g.FillRectangle(brush, rect);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    private void DrawRegionLayer(Graphics g, MapLayerDefinition layer)
    {
        var visible = GetVisibleRange();
        using var brush = new HatchBrush(
            HatchStyle.Percent40,
            Color.FromArgb((int)(130 * layer.Opacity), 252, 186, 3),
            Color.FromArgb((int)(50 * layer.Opacity), 252, 186, 3));
        using var pen = new Pen(Color.FromArgb((int)(190 * layer.Opacity), 255, 214, 96), 1f);
        foreach (var cell in layer.Tiles)
        {
            if (cell.X < visible.Left || cell.X > visible.Right || cell.Y < visible.Top || cell.Y > visible.Bottom)
            {
                continue;
            }

            var rect = CellToScreenRect(cell.X, cell.Y);
            g.FillRectangle(brush, rect);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    private void DrawGrid(Graphics g)
    {
        if (_map is null)
        {
            return;
        }

        var visible = GetVisibleRange();
        using var pen = new Pen(Color.FromArgb(TilePixels < 15f ? 45 : 78, 255, 255, 255), 1f);
        using var majorPen = new Pen(Color.FromArgb(110, 255, 255, 255), 1f);
        for (var x = visible.Left; x <= visible.Right + 1; x++)
        {
            var sx = CellToScreenRect(x, visible.Top).Left;
            g.DrawLine(x % 8 == 0 ? majorPen : pen, sx, CellToScreenRect(x, visible.Top).Top, sx, CellToScreenRect(x, visible.Bottom).Bottom);
        }

        for (var y = visible.Top; y <= visible.Bottom + 1; y++)
        {
            var sy = CellToScreenRect(visible.Left, y).Top;
            g.DrawLine(y % 8 == 0 ? majorPen : pen, CellToScreenRect(visible.Left, y).Left, sy, CellToScreenRect(visible.Right, y).Right, sy);
        }
    }

    private void DrawMapBorder(Graphics g)
    {
        if (_map is null)
        {
            return;
        }

        var topLeft = WorldToScreen(0, 0);
        var bottomRight = WorldToScreen(_map.Width, _map.Height);
        using var pen = new Pen(Color.FromArgb(210, 120, 190, 255), 2f);
        g.DrawRectangle(pen, topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);
    }

    private void DrawBrushPreview(Graphics g)
    {
        if (_map is null || !IsInside(_hoverCell))
        {
            return;
        }

        if (_activeTool == MapEditorTool.Rectangle && _isRectSelecting)
        {
            var rect = GetScreenRectFromCells(_rectStart, _rectEnd);
            using var fill = new SolidBrush(Color.FromArgb(35, 255, 255, 255));
            using var pen = new Pen(Color.FromArgb(230, 255, 255, 255), 2f) { DashStyle = DashStyle.Dash };
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
            return;
        }

        var hoverRect = CellToScreenRect(_hoverCell.X, _hoverCell.Y);
        using var brush = new SolidBrush(Color.FromArgb(38, 255, 255, 255));
        using var outline = new Pen(Color.White, 2f);
        g.FillRectangle(brush, hoverRect);
        g.DrawRectangle(outline, hoverRect.X, hoverRect.Y, hoverRect.Width, hoverRect.Height);
    }

    private void DrawHud(Graphics g)
    {
        if (_map is null)
        {
            return;
        }

        using var backBrush = new SolidBrush(Color.FromArgb(145, 20, 20, 20));
        using var textBrush = new SolidBrush(Color.FromArgb(235, 245, 245, 245));
        using var font = new Font("Consolas", 9F);
        var terrain = _selectedTerrain?.DisplayName ?? _selectedTerrain?.Id ?? "-";
        var layer = _activeLayer?.Name ?? "-";
        var brush = _brushSource == MapBrushSource.Tileset && _selectedTilesetTile.X >= 0
            ? $"atlas {_selectedTilesetTile.X},{_selectedTilesetTile.Y}"
            : terrain;
        var text = $"{_map.Width}x{_map.Height}  zoom {MathF.Round(_zoom * 100f)}%  layer {layer}  brush {brush}";
        var size = g.MeasureString(text, font);
        var rect = new RectangleF(10, Height - size.Height - 14, size.Width + 16, size.Height + 8);
        g.FillRectangle(backBrush, rect);
        g.DrawString(text, font, textBrush, rect.Left + 8, rect.Top + 4);
    }

    private void ApplyTool(Point cell)
    {
        if (_map is null || _activeLayer is null || !IsInside(cell) || _activeLayer.Locked)
        {
            return;
        }

        switch (_activeTool)
        {
            case MapEditorTool.Paint:
                SetCells(_activeLayer, cell.X, cell.Y);
                break;
            case MapEditorTool.Erase:
                RemoveCell(_activeLayer, cell.X, cell.Y);
                break;
            case MapEditorTool.Fill:
                FloodFill(cell);
                break;
            case MapEditorTool.Eyedropper:
                PickTerrain(cell);
                return;
        }

        MapChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    private void PaintRectangle(Point start, Point end)
    {
        if (_map is null || _activeLayer is null || _activeLayer.Locked)
        {
            return;
        }

        var left = Math.Clamp(Math.Min(start.X, end.X), 0, _map.Width - 1);
        var right = Math.Clamp(Math.Max(start.X, end.X), 0, _map.Width - 1);
        var top = Math.Clamp(Math.Min(start.Y, end.Y), 0, _map.Height - 1);
        var bottom = Math.Clamp(Math.Max(start.Y, end.Y), 0, _map.Height - 1);
        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                SetCells(_activeLayer, x, y);
            }
        }

        MapChanged?.Invoke(this, EventArgs.Empty);
    }

    private void SetCells(MapLayerDefinition layer, int x, int y)
    {
        if (_brushSource == MapBrushSource.Tileset && (_selectedTilesetTileSize.Width > 1 || _selectedTilesetTileSize.Height > 1))
        {
            for (var offsetY = 0; offsetY < _selectedTilesetTileSize.Height; offsetY++)
            {
                for (var offsetX = 0; offsetX < _selectedTilesetTileSize.Width; offsetX++)
                {
                    SetCell(layer, x + offsetX, y + offsetY, _selectedTilesetTile.X + offsetX, _selectedTilesetTile.Y + offsetY);
                }
            }

            return;
        }

        SetCell(layer, x, y);
    }

    private void SetCell(MapLayerDefinition layer, int x, int y, int? overrideTileX = null, int? overrideTileY = null)
    {
        if (_map is null || x < 0 || y < 0 || x >= _map.Width || y >= _map.Height)
        {
            return;
        }

        var existing = layer.Tiles.FirstOrDefault(v => v.X == x && v.Y == y);
        var created = false;
        if (existing is null)
        {
            existing = new MapTileCell { X = x, Y = y };
            layer.Tiles.Add(existing);
            created = true;
        }

        if (layer.Kind.Equals("Collision", StringComparison.OrdinalIgnoreCase))
        {
            existing.Solid = true;
            existing.TerrainId = "collision";
            return;
        }

        if (layer.Kind.Equals("Region", StringComparison.OrdinalIgnoreCase))
        {
            existing.TerrainId = _selectedTerrain?.Id ?? "region";
            existing.TileX = -1;
            existing.TileY = -1;
            existing.Tag = string.IsNullOrWhiteSpace(existing.Tag) ? "region" : existing.Tag;
            return;
        }

        if (_brushSource == MapBrushSource.Tileset && _selectedTilesetTile.X >= 0 && _selectedTilesetTile.Y >= 0)
        {
            existing.TileX = overrideTileX ?? _selectedTilesetTile.X;
            existing.TileY = overrideTileY ?? _selectedTilesetTile.Y;
            if (_shiftAutoTerrainMode && _shiftTerrain is not null)
            {
                existing.TerrainId = _shiftTerrain.Id;
                existing.TileX = -1;
                existing.TileY = -1;
                existing.Tag = string.Empty;
                return;
            }

            if (_selectedTilesetTileIsA1Overlay)
            {
                existing.Tag = A1OverlayTag(existing.TileX, existing.TileY, _selectedTilesetTileIsA1OverlayAnimated);
                return;
            }

            existing.TerrainId = string.Empty;
            existing.Tag = string.Empty;
            return;
        }

        if (ShouldPaintSelectedTerrainAsTileset())
        {
            existing.TileX = _selectedTilesetTile.X;
            existing.TileY = _selectedTilesetTile.Y;
            existing.Tag = A1OverlayTag(existing.TileX, existing.TileY, _selectedTilesetTileIsA1OverlayAnimated);
            return;
        }

        if (_selectedTerrain is null)
        {
            if (created)
            {
                RemoveCell(layer, x, y);
            }

            return;
        }

        existing.TerrainId = _selectedTerrain.Id;
        existing.TileX = -1;
        existing.TileY = -1;
        existing.Tag = string.Empty;
    }

    private static void RemoveCell(MapLayerDefinition layer, int x, int y)
    {
        for (var index = layer.Tiles.Count - 1; index >= 0; index--)
        {
            if (layer.Tiles[index].X == x && layer.Tiles[index].Y == y)
            {
                layer.Tiles.RemoveAt(index);
            }
        }
    }

    private void FloodFill(Point origin)
    {
        if (_map is null || _activeLayer is null || !IsInside(origin))
        {
            return;
        }

        var lookup = BuildLookup(_activeLayer);
        var targetKey = GetFillKey(_activeLayer, lookup.TryGetValue((origin.X, origin.Y), out var originCell) ? originCell : null);
        var replacementKey = GetReplacementKey(_activeLayer);
        if (string.Equals(targetKey, replacementKey, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var queue = new Queue<Point>();
        var visited = new HashSet<(int X, int Y)>();
        queue.Enqueue(origin);
        visited.Add((origin.X, origin.Y));

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            lookup.TryGetValue((current.X, current.Y), out var currentCell);
            if (!string.Equals(GetFillKey(_activeLayer, currentCell), targetKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            SetCell(_activeLayer, current.X, current.Y);
            foreach (var next in EnumerateNeighbors(current))
            {
                if (!IsInside(next) || !visited.Add((next.X, next.Y)))
                {
                    continue;
                }

                queue.Enqueue(next);
            }
        }
    }

    private void PickTerrain(Point cell)
    {
        if (_map is null || _activeLayer is null)
        {
            return;
        }

        var picked = _activeLayer.Tiles.FirstOrDefault(v => v.X == cell.X && v.Y == cell.Y);
        if (picked is null)
        {
            return;
        }

        if (picked.TileX >= 0 && picked.TileY >= 0)
        {
            TilesetTilePicked?.Invoke(this, new TilesetTileSelectedEventArgs(picked.TileX, picked.TileY));
            return;
        }

        if (!string.IsNullOrWhiteSpace(picked.TerrainId))
        {
            TerrainPicked?.Invoke(this, new MapTerrainPickedEventArgs(picked.TerrainId));
        }
    }

    private string GetReplacementKey(MapLayerDefinition layer)
    {
        if (layer.Kind.Equals("Collision", StringComparison.OrdinalIgnoreCase))
        {
            return "solid";
        }

        if (layer.Kind.Equals("Region", StringComparison.OrdinalIgnoreCase))
        {
            return _selectedTerrain?.Id ?? "region";
        }

        return _brushSource == MapBrushSource.Tileset || ShouldPaintSelectedTerrainAsTileset()
            ? $"atlas:{_selectedTilesetTile.X},{_selectedTilesetTile.Y}"
            : _selectedTerrain?.Id ?? string.Empty;
    }

    private bool ShouldPaintSelectedTerrainAsTileset()
    {
        return _brushSource == MapBrushSource.Terrain
            && _selectedTilesetTileIsA1Overlay
            && _selectedTilesetTile.X >= 0
            && _selectedTilesetTile.Y >= 0
            && !_shiftAutoTerrainMode;
    }

    private static string GetFillKey(MapLayerDefinition layer, MapTileCell? cell)
    {
        if (cell is null)
        {
            return "";
        }

        if (layer.Kind.Equals("Collision", StringComparison.OrdinalIgnoreCase))
        {
            return cell.Solid ? "solid" : "";
        }

        return cell.TileX >= 0 && cell.TileY >= 0
            ? $"atlas:{cell.TileX},{cell.TileY}"
            : cell.TerrainId ?? "";
    }

    private static IEnumerable<Point> EnumerateNeighbors(Point cell)
    {
        yield return new Point(cell.X + 1, cell.Y);
        yield return new Point(cell.X - 1, cell.Y);
        yield return new Point(cell.X, cell.Y + 1);
        yield return new Point(cell.X, cell.Y - 1);
    }

    private Rectangle GetVisibleRange()
    {
        if (_map is null)
        {
            return Rectangle.Empty;
        }

        var topLeft = ScreenToCell(new Point(0, 0));
        var bottomRight = ScreenToCell(new Point(Width, Height));
        var left = Math.Clamp(Math.Min(topLeft.X, bottomRight.X) - 2, 0, _map.Width - 1);
        var top = Math.Clamp(Math.Min(topLeft.Y, bottomRight.Y) - 2, 0, _map.Height - 1);
        var right = Math.Clamp(Math.Max(topLeft.X, bottomRight.X) + 2, 0, _map.Width - 1);
        var bottom = Math.Clamp(Math.Max(topLeft.Y, bottomRight.Y) + 2, 0, _map.Height - 1);
        return Rectangle.FromLTRB(left, top, right, bottom);
    }

    private RectangleF GetScreenRectFromCells(Point a, Point b)
    {
        var left = Math.Min(a.X, b.X);
        var top = Math.Min(a.Y, b.Y);
        var right = Math.Max(a.X, b.X) + 1;
        var bottom = Math.Max(a.Y, b.Y) + 1;
        var topLeft = WorldToScreen(left, top);
        var bottomRight = WorldToScreen(right, bottom);
        return RectangleF.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
    }

    private RectangleF CellToScreenRect(int x, int y)
    {
        var topLeft = WorldToScreen(x, y);
        return new RectangleF(topLeft.X, topLeft.Y, TilePixels, TilePixels);
    }

    private PointF WorldToScreen(float x, float y)
    {
        return new PointF(
            Width / 2f + (x - _cameraX) * TilePixels,
            Height / 2f + (y - _cameraY) * TilePixels);
    }

    private PointF ScreenToWorld(Point point)
    {
        return new PointF(
            _cameraX + (point.X - Width / 2f) / TilePixels,
            _cameraY + (point.Y - Height / 2f) / TilePixels);
    }

    private Point ScreenToCell(Point point)
    {
        var world = ScreenToWorld(point);
        return new Point((int)MathF.Floor(world.X), (int)MathF.Floor(world.Y));
    }

    private bool IsInside(Point cell)
    {
        return _map is not null && cell.X >= 0 && cell.Y >= 0 && cell.X < _map.Width && cell.Y < _map.Height;
    }

    private float TilePixels => Math.Max(4f, (_map?.TileSize ?? 32) * _zoom);

    private bool HasAnimatedTerrain()
    {
        return _map?.Terrains.Any(v => v.Animated) == true
            || _map?.Layers.Any(layer => layer.Tiles.Any(tile => IsAnimatedA1OverlayTag(tile.Tag))) == true;
    }

    private static bool IsAnimatedA1OverlayTag(string? tag)
    {
        return TryParseA1OverlayTag(tag, out _, out _, out var animated) && animated;
    }

    private Color AnimateColor(Color color, MapTerrainDefinition terrain)
    {
        var frame = _animationTick % Math.Max(1, terrain.AnimationFrames);
        var delta = (int)(Math.Sin((_animationTick + frame) * 0.65) * 16);
        return Color.FromArgb(
            color.A,
            Math.Clamp(color.R + delta / 2, 0, 255),
            Math.Clamp(color.G + delta / 2, 0, 255),
            Math.Clamp(color.B + delta, 0, 255));
    }

    private static Color WithOpacity(Color color, float opacity)
    {
        return Color.FromArgb((int)Math.Clamp(color.A * opacity, 0, 255), color.R, color.G, color.B);
    }

    private static bool IsSameTerrain(Dictionary<(int X, int Y), MapTileCell> lookup, int x, int y, string terrainId)
    {
        return lookup.TryGetValue((x, y), out var other)
            && string.Equals(other.TerrainId, terrainId, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<(int X, int Y), MapTileCell> BuildLookup(MapLayerDefinition layer)
    {
        var lookup = new Dictionary<(int X, int Y), MapTileCell>();
        foreach (var tile in layer.Tiles)
        {
            lookup[(tile.X, tile.Y)] = tile;
        }

        return lookup;
    }

    private bool DrawTilesetTile(Graphics g, RectangleF rect, MapTileCell cell, MapTerrainDefinition? terrain, float opacity)
    {
        if (_tilesetImage is null || _map is null)
        {
            return false;
        }

        var tileX = cell.TileX;
        var tileY = cell.TileY;
        var frameOffset = 0;
        if (tileX < 0 || tileY < 0)
        {
            tileX = terrain?.TileX ?? -1;
            tileY = terrain?.TileY ?? -1;
            if (terrain?.Animated == true)
            {
                frameOffset = _animationTick % Math.Max(1, terrain.AnimationFrames);
            }
        }

        if (tileX < 0 || tileY < 0)
        {
            return false;
        }

        var sourceX = (tileX + frameOffset) * _map.TileSize;
        var sourceY = tileY * _map.TileSize;
        if (sourceX < 0 || sourceY < 0 || sourceX + _map.TileSize > _tilesetImage.Width || sourceY + _map.TileSize > _tilesetImage.Height)
        {
            return false;
        }

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);
        g.DrawImage(
            _tilesetImage,
            Rectangle.Round(rect),
            sourceX,
            sourceY,
            _map.TileSize,
            _map.TileSize,
            GraphicsUnit.Pixel,
            attributes);
        return true;
    }

    private void DrawA1Overlay(Graphics g, RectangleF rect, MapTileCell cell, MapLayerDefinition layer, float opacity)
    {
        if (_tilesetImage is null || _map is null || !TryParseA1OverlayTag(cell.Tag, out var tileX, out var tileY, out var animated))
        {
            return;
        }

        if (!animated)
        {
            DrawA1StaticOverlay(g, rect, cell, layer, opacity, tileX, tileY);
            return;
        }

        var frame = A1OverlayUsesWaterSurfaceSequence(tileX, tileY)
            ? A1OverlayWaterSurfaceSequence[_animationTick % A1OverlayWaterSurfaceSequence.Length]
            : _animationTick % 3;
        tileY += frame;
        var sourceX = tileX * _map.TileSize;
        var sourceY = tileY * _map.TileSize;
        if (sourceX < 0 || sourceY < 0 || sourceX + _map.TileSize > _tilesetImage.Width || sourceY + _map.TileSize > _tilesetImage.Height)
        {
            return;
        }

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);
        g.DrawImage(
            _tilesetImage,
            Rectangle.Round(rect),
            sourceX,
            sourceY,
            _map.TileSize,
            _map.TileSize,
            GraphicsUnit.Pixel,
            attributes);
    }

    private void DrawA1StaticOverlay(Graphics g, RectangleF rect, MapTileCell cell, MapLayerDefinition layer, float opacity, int tileX, int tileY)
    {
        if (_tilesetImage is null || _map is null)
        {
            return;
        }

        DrawA1AutoOverlay(g, rect, cell, layer, opacity, tileX, tileY);
    }

    private void ClearA1OverlayFrameCache()
    {
        _a1OverlayFrameCache.Clear();
    }

    private bool A1OverlayUsesWaterSurfaceSequence(int tileX, int tileY)
    {
        if (_tilesetImage is null || _map is null)
        {
            return false;
        }

        var key = new A1OverlayFrameKey(tileX * _map.TileSize, tileY * _map.TileSize, _map.TileSize);
        if (!_a1OverlayFrameCache.TryGetValue(key, out var usesWaterSurfaceSequence))
        {
            usesWaterSurfaceSequence = IsA1OverlayWaterSurfaceObject(key.SourceX, key.SourceY, key.TileSize);
            _a1OverlayFrameCache[key] = usesWaterSurfaceSequence;
        }

        return usesWaterSurfaceSequence;
    }

    private bool IsA1OverlayWaterSurfaceObject(int sourceX, int sourceY, int tileSize)
    {
        if (_tilesetImage is null
            || sourceX < 0
            || sourceY < 0
            || sourceX + tileSize > _tilesetImage.Width
            || sourceY + tileSize > _tilesetImage.Height)
        {
            return false;
        }

        Bitmap? disposableTileset = null;
        var tilesetBitmap = _tilesetImage as Bitmap ?? (disposableTileset = new Bitmap(_tilesetImage));
        try
        {
            var waterPixels = 0;
            var foregroundPixels = 0;
            for (var y = 0; y < tileSize; y++)
            {
                for (var x = 0; x < tileSize; x++)
                {
                    var color = tilesetBitmap.GetPixel(sourceX + x, sourceY + y);
                    if (color.A <= 8 || IsA1OverlayWaterPixel(color))
                    {
                        waterPixels++;
                    }
                    else
                    {
                        foregroundPixels++;
                    }
                }
            }

            var totalPixels = Math.Max(1, waterPixels + foregroundPixels);
            return waterPixels / (float)totalPixels >= A1OverlayFrameMinWaterRatio
                && foregroundPixels / (float)totalPixels >= A1OverlayFrameMinForegroundRatio;
        }
        finally
        {
            disposableTileset?.Dispose();
        }
    }

    private static bool IsA1OverlayWaterPixel(Color color)
    {
        var max = Math.Max(color.R, Math.Max(color.G, color.B));
        var min = Math.Min(color.R, Math.Min(color.G, color.B));
        var brightness = max / 255f;
        var saturation = max == 0 ? 0f : (max - min) / (float)max;
        var hue = color.GetHue();
        if (color.R >= 232 && color.G >= 232 && color.B >= 232)
        {
            return true;
        }

        return color.B >= color.R
            && color.G >= color.R
            && hue is >= 175f and <= 220f
            && saturation >= 0.15f
            && brightness >= 0.38f;
    }

    private void DrawA1AutoOverlay(Graphics g, RectangleF rect, MapTileCell cell, MapLayerDefinition layer, float opacity, int tileX, int tileY)
    {
        if (_tilesetImage is null || _map is null)
        {
            return;
        }

        var tileSize = _map.TileSize;
        var quarterSize = tileSize / 2f;
        var blockPixelX = tileX * tileSize;
        var blockPixelY = tileY * tileSize;
        if (blockPixelX < 0 || blockPixelY < 0 || blockPixelX + tileSize * 2 > _tilesetImage.Width || blockPixelY + tileSize * 3 > _tilesetImage.Height)
        {
            return;
        }

        var sameN = IsSameA1Overlay(layer, cell, 0, -1);
        var sameE = IsSameA1Overlay(layer, cell, 1, 0);
        var sameS = IsSameA1Overlay(layer, cell, 0, 1);
        var sameW = IsSameA1Overlay(layer, cell, -1, 0);
        var sameNw = IsSameA1Overlay(layer, cell, -1, -1);
        var sameNe = IsSameA1Overlay(layer, cell, 1, -1);
        var sameSe = IsSameA1Overlay(layer, cell, 1, 1);
        var sameSw = IsSameA1Overlay(layer, cell, -1, 1);

        Span<Point> quarters =
        [
            PickOverlayTopLeft(sameN, sameW, sameNw),
            PickOverlayTopRight(sameN, sameE, sameNe),
            PickOverlayBottomLeft(sameS, sameW, sameSw),
            PickOverlayBottomRight(sameS, sameE, sameSe)
        ];

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);

        for (var index = 0; index < quarters.Length; index++)
        {
            var source = quarters[index];
            g.DrawImage(
                _tilesetImage,
                GetDestinationQuarter(rect, index),
                (int)MathF.Round(blockPixelX + source.X * quarterSize),
                (int)MathF.Round(blockPixelY + source.Y * quarterSize),
                (int)MathF.Ceiling(quarterSize),
                (int)MathF.Ceiling(quarterSize),
                GraphicsUnit.Pixel,
                attributes);
        }
    }

    private bool IsSameA1Overlay(MapLayerDefinition layer, MapTileCell cell, int offsetX, int offsetY)
    {
        var other = layer.Tiles.FirstOrDefault(v => v.X == cell.X + offsetX && v.Y == cell.Y + offsetY);
        return other is not null
            && TryParseA1OverlayTag(cell.Tag, out var tileX, out var tileY, out var animated)
            && TryParseA1OverlayTag(other.Tag, out var otherTileX, out var otherTileY, out var otherAnimated)
            && tileX == otherTileX
            && tileY == otherTileY
            && animated == otherAnimated;
    }

    private static Point PickOverlayTopLeft(bool north, bool west, bool northWest)
    {
        if (!north && !west) return new Point(0, 0);
        if (!north) return new Point(2, 2);
        if (!west) return new Point(0, 4);
        return northWest ? new Point(2, 4) : new Point(2, 0);
    }

    private static Point PickOverlayTopRight(bool north, bool east, bool northEast)
    {
        if (!north && !east) return new Point(1, 0);
        if (!north) return new Point(1, 2);
        if (!east) return new Point(3, 4);
        return northEast ? new Point(1, 4) : new Point(3, 0);
    }

    private static Point PickOverlayBottomLeft(bool south, bool west, bool southWest)
    {
        if (!south && !west) return new Point(0, 1);
        if (!south) return new Point(2, 5);
        if (!west) return new Point(0, 3);
        return southWest ? new Point(2, 3) : new Point(2, 1);
    }

    private static Point PickOverlayBottomRight(bool south, bool east, bool southEast)
    {
        if (!south && !east) return new Point(1, 1);
        if (!south) return new Point(1, 5);
        if (!east) return new Point(3, 3);
        return southEast ? new Point(1, 3) : new Point(3, 1);
    }

    private static Rectangle GetDestinationQuarter(RectangleF destination, int index)
    {
        var left = index % 2 == 0 ? destination.Left : destination.Left + destination.Width / 2f;
        var top = index < 2 ? destination.Top : destination.Top + destination.Height / 2f;
        var right = index % 2 == 0 ? destination.Left + destination.Width / 2f : destination.Right;
        var bottom = index < 2 ? destination.Top + destination.Height / 2f : destination.Bottom;
        return Rectangle.Round(RectangleF.FromLTRB(left, top, right, bottom));
    }

    private static string A1OverlayTag(int tileX, int tileY, bool animated)
    {
        return $"a1Overlay:{(animated ? "animated" : "static")}:{tileX},{tileY}";
    }

    private static bool TryParseA1OverlayTag(string? tag, out int tileX, out int tileY, out bool animated)
    {
        tileX = -1;
        tileY = -1;
        animated = false;
        const string prefix = "a1Overlay:";
        if (string.IsNullOrWhiteSpace(tag) || !tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var payload = tag[prefix.Length..];
        var typeEnd = payload.IndexOf(':');
        if (typeEnd <= 0)
        {
            return false;
        }

        animated = payload[..typeEnd].Equals("animated", StringComparison.OrdinalIgnoreCase);
        var parts = payload[(typeEnd + 1)..].Split(',', 2);
        return parts.Length == 2
            && int.TryParse(parts[0], out tileX)
            && int.TryParse(parts[1], out tileY)
            && tileX >= 0
            && tileY >= 0;
    }
}

public enum MapEditorTool
{
    Paint,
    Erase,
    Fill,
    Rectangle,
    Eyedropper
}

public enum MapBrushSource
{
    Terrain,
    Tileset
}

public sealed class MapTileHoverEventArgs : EventArgs
{
    public MapTileHoverEventArgs(int x, int y, bool inside)
    {
        X = x;
        Y = y;
        Inside = inside;
    }

    public int X { get; }

    public int Y { get; }

    public bool Inside { get; }
}

public sealed class MapTerrainPickedEventArgs : EventArgs
{
    public MapTerrainPickedEventArgs(string terrainId)
    {
        TerrainId = terrainId;
    }

    public string TerrainId { get; }
}
