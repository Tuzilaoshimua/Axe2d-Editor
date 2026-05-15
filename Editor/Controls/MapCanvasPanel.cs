using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Axe2DEditor.Core.Maps;

namespace Axe2DEditor.Editor.Controls;

public sealed class MapCanvasPanel : Panel
{
    private const float MinZoom = 0.2f;
    private const float MaxZoom = 6f;
    private const float DefaultZoom = 1f;

    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 160 };
    private readonly System.Windows.Forms.Timer _brushSizePopupTimer = new() { Interval = 1000 };

    private MapDefinition? _map;
    private MapLayerDefinition? _activeLayer;
    private MapTerrainDefinition? _selectedTerrain;
    private MapTerrainDefinition? _shiftTerrain;
    private Image? _tilesetImage;
    private Point _selectedTilesetTile = new(-1, -1);
    private Size _selectedTilesetTileSize = new(1, 1);
    private List<TilesetBrushCell>? _selectedTilesetPattern;
    private List<TerrainBrushCell>? _selectedTerrainPattern;
    private bool _selectedTilesetTileIsA1Overlay;
    private bool _paintSelectedTerrainAsTileset;
    private bool _selectedTilesetTileIsA1OverlayAnimated;
    private bool _terrainFillModeEnabled;
    private bool _shiftAutoTerrainMode;
    private bool _temporaryEyedropperMode;
    private MapBrushSource _brushSource = MapBrushSource.Terrain;
    private MapEditorTool _activeTool = MapEditorTool.Paint;
    private bool _showGrid = true;
    private bool _showAutoEdges = true;
    private bool _showTileCollisionShapes;
    private bool _animationEnabled = true;
    private bool _isMouseDown;
    private bool _isPanning;
    private bool _isRectSelecting;
    private bool _hasActiveEditStroke;
    private bool _activeEditStrokeChanged;
    private Point _panStart;
    private Point _hoverCell = new(-1, -1);
    private Point _rectStart = new(-1, -1);
    private Point _rectEnd = new(-1, -1);
    private Point _brushSizePopupLocation = Point.Empty;
    private float _cameraX;
    private float _cameraY;
    private float _zoom = DefaultZoom;
    private int _animationTick;
    private int _paintBrushSize = 1;
    private int _eraseBrushSize = 1;

    public event EventHandler? MapChanged;
    public event EventHandler? EditStrokeStarting;
    public event EventHandler<MapEditStrokeCompletedEventArgs>? EditStrokeCompleted;
    public event EventHandler<MapTileHoverEventArgs>? TileHovered;
    public event EventHandler<MapTerrainPickedEventArgs>? TerrainPicked;
    public event EventHandler<TilesetTileSelectedEventArgs>? TilesetTilePicked;
    public event EventHandler<BrushSizePopupRequestedEventArgs>? BrushSizePopupRequested;
    public event EventHandler? ShiftAutoTerrainRequested;

    public MapBrushSource BrushSource => _brushSource;

    public int PaintBrushSize
    {
        get => _paintBrushSize;
        set
        {
            var clamped = Math.Clamp(value, 1, 12);
            if (_paintBrushSize == clamped)
            {
                return;
            }

            _paintBrushSize = clamped;
            Invalidate();
        }
    }

    public int EraseBrushSize
    {
        get => _eraseBrushSize;
        set
        {
            var clamped = Math.Clamp(value, 1, 12);
            if (_eraseBrushSize == clamped)
            {
                return;
            }

            _eraseBrushSize = clamped;
            Invalidate();
        }
    }

    public int CurrentBrushSize => _activeTool == MapEditorTool.Erase ? _eraseBrushSize : _paintBrushSize;

    public MapEditorTool ActiveTool
    {
        get => _activeTool;
        set
        {
            if (_activeTool == value)
            {
                return;
            }

            _brushSizePopupTimer.Stop();
            if (_isMouseDown)
            {
                _isMouseDown = false;
                EndEditStrokeIfNeeded();
            }

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

    public bool ShowTileCollisionShapes
    {
        get => _showTileCollisionShapes;
        set
        {
            if (_showTileCollisionShapes == value)
            {
                return;
            }

            _showTileCollisionShapes = value;
            Invalidate();
        }
    }

    public bool AnimationEnabled
    {
        get => _animationEnabled;
        set
        {
            if (_animationEnabled == value)
            {
                return;
            }

            _animationEnabled = value;
            if (_animationEnabled)
            {
                _animationTimer.Start();
            }
            else
            {
                _animationTimer.Stop();
            }

            Invalidate();
        }
    }

    public bool TerrainFillModeEnabled
    {
        get => _terrainFillModeEnabled;
        set
        {
            if (_terrainFillModeEnabled == value)
            {
                return;
            }

            _terrainFillModeEnabled = value;
            Invalidate();
        }
    }

    private int EffectiveAnimationTick => _animationEnabled ? _animationTick : 0;

    public MapCanvasPanel()
    {
        SetStyle(ControlStyles.Selectable, true);
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
        Cursor = Cursors.Arrow;
        BackColor = Color.FromArgb(30, 30, 30);
        _animationTimer.Tick += (_, _) =>
        {
            if (!_animationEnabled)
            {
                return;
            }

            _animationTick++;
            if (HasAnimatedTerrain())
            {
                Invalidate();
            }
        };
        _brushSizePopupTimer.Tick += (_, _) =>
        {
            _brushSizePopupTimer.Stop();
            if (_isMouseDown && _activeTool == MapEditorTool.Paint)
            {
                BrushSizePopupRequested?.Invoke(this, new BrushSizePopupRequestedEventArgs(_brushSizePopupLocation));
            }
        };
        _animationTimer.Start();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Dispose();
            _brushSizePopupTimer.Dispose();
            _tilesetImage?.Dispose();
        }

        base.Dispose(disposing);
    }

    public void SetMap(MapDefinition? map)
    {
        SetMap(map, preserveView: false);
    }

    public void SetMap(MapDefinition? map, bool preserveView)
    {
        var previousCameraX = _cameraX;
        var previousCameraY = _cameraY;
        var previousZoom = _zoom;
        _map = map;
        if (_map is not null)
        {
            MapDefaults.Normalize(_map);
            if (preserveView)
            {
                _cameraX = previousCameraX;
                _cameraY = previousCameraY;
                _zoom = previousZoom;
            }
            else
            {
                _cameraX = _map.Width / 2f;
                _cameraY = _map.Height / 2f;
                _zoom = DefaultZoom;
            }
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
        _selectedTerrainPattern = null;
        if (terrain is not null)
        {
            UseTerrainBrush();
        }
        else
        {
            Invalidate();
        }
    }

    public void UseTerrainBrush()
    {
        _brushSource = MapBrushSource.Terrain;
        UpdateSelectedTerrainPaintMode();
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
        UpdateSelectedTerrainPaintMode();
        Invalidate();
    }

    public void SetTemporaryEyedropperMode(bool enabled)
    {
        if (_temporaryEyedropperMode == enabled)
        {
            return;
        }

        _temporaryEyedropperMode = enabled;
        Cursor = enabled ? Cursors.Hand : Cursors.Arrow;
        Invalidate();
    }

    public void SetTilesetImage(Image? image)
    {
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
        _selectedTilesetPattern = null;
        _selectedTerrainPattern = null;
        _selectedTilesetTileIsA1Overlay = isA1Overlay;
        _selectedTilesetTileIsA1OverlayAnimated = isA1OverlayAnimated;
        UpdateSelectedTerrainPaintMode();
        Invalidate();
    }

    public void SetTilesetPatternSelection(IReadOnlyList<TilesetBrushCell> cells, bool isA1Overlay, bool isA1OverlayAnimated)
    {
        _selectedTilesetPattern = cells
            .Where(cell => cell.TileX >= 0 && cell.TileY >= 0 && cell.OffsetX >= 0 && cell.OffsetY >= 0)
            .Select(cell => new TilesetBrushCell(cell.OffsetX, cell.OffsetY, cell.TileX, cell.TileY))
            .ToList();
        _selectedTerrainPattern = null;
        if (_selectedTilesetPattern.Count <= 0)
        {
            _selectedTilesetPattern = null;
            return;
        }

        _selectedTilesetTile = new Point(_selectedTilesetPattern[0].TileX, _selectedTilesetPattern[0].TileY);
        _selectedTilesetTileSize = new Size(
            _selectedTilesetPattern.Max(cell => cell.OffsetX) + 1,
            _selectedTilesetPattern.Max(cell => cell.OffsetY) + 1);
        _selectedTilesetTileIsA1Overlay = isA1Overlay;
        _selectedTilesetTileIsA1OverlayAnimated = isA1OverlayAnimated;
        UpdateSelectedTerrainPaintMode();
        Invalidate();
    }

    public void SetTerrainPatternSelection(IReadOnlyList<TerrainBrushCell> cells)
    {
        _selectedTerrainPattern = cells
            .Where(cell => !string.IsNullOrWhiteSpace(cell.TerrainId) && cell.OffsetX >= 0 && cell.OffsetY >= 0)
            .Select(cell => new TerrainBrushCell(cell.OffsetX, cell.OffsetY, cell.TerrainId, cell.Tag))
            .ToList();
        _selectedTilesetPattern = null;
        if (_selectedTerrainPattern.Count <= 0)
        {
            _selectedTerrainPattern = null;
            return;
        }

        _selectedTilesetTileSize = new Size(
            _selectedTerrainPattern.Max(cell => cell.OffsetX) + 1,
            _selectedTerrainPattern.Max(cell => cell.OffsetY) + 1);
        UpdateSelectedTerrainPaintMode();
        Invalidate();
    }

    public void UseTilesetBrush()
    {
        if (_selectedTilesetTile.X < 0 || _selectedTilesetTile.Y < 0)
        {
            return;
        }

        _brushSource = MapBrushSource.Tileset;
        _paintSelectedTerrainAsTileset = false;
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

        if (_temporaryEyedropperMode)
        {
            PickTerrain(cell);
            return;
        }

        _isMouseDown = true;
        if ((ModifierKeys & Keys.Shift) == Keys.Shift)
        {
            ShiftAutoTerrainRequested?.Invoke(this, EventArgs.Empty);
        }

        BeginEditStrokeIfNeeded();
        if (_activeTool == MapEditorTool.Paint)
        {
            _brushSizePopupLocation = e.Location;
            _brushSizePopupTimer.Stop();
            _brushSizePopupTimer.Start();
        }

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
            _brushSizePopupTimer.Stop();
            ApplyTool(cell);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button is MouseButtons.Right or MouseButtons.Middle)
        {
            _isPanning = false;
            Cursor = _temporaryEyedropperMode ? Cursors.Hand : Cursors.Arrow;
            return;
        }

        _brushSizePopupTimer.Stop();
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
        EndEditStrokeIfNeeded();
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverCell = new Point(-1, -1);
        _brushSizePopupTimer.Stop();
        if (_isMouseDown)
        {
            _isMouseDown = false;
            _isRectSelecting = false;
            EndEditStrokeIfNeeded();
        }

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

        if (_showTileCollisionShapes)
        {
            DrawTileCollisionShapes(g);
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
            var usesRpgMakerAutoTile = RpgMakerAutoTile.IsA1(terrain)
                || RpgMakerAutoTile.IsA2(terrain)
                || RpgMakerAutoTile.IsA3(terrain)
                || RpgMakerAutoTile.IsA4(terrain);
            var usesWangAutoTile = WangAutoTile.IsWang(terrain);
            var animationTick = EffectiveAnimationTick;
            var drawnFromTileset = RpgMakerAutoTile.IsA1(terrain)
                && terrain is not null
                && RpgMakerAutoTile.DrawA1(g, _tilesetImage, _map.TileSize, rect, cell, terrain, lookup, layer.Opacity, animationTick);
            drawnFromTileset = drawnFromTileset || (RpgMakerAutoTile.IsA2(terrain)
                && terrain is not null
                && RpgMakerAutoTile.DrawA2(g, _tilesetImage, _map.TileSize, rect, cell, terrain, lookup, layer.Opacity, animationTick));
            drawnFromTileset = drawnFromTileset || (RpgMakerAutoTile.IsA3(terrain)
                && terrain is not null
                && RpgMakerAutoTile.DrawA3(g, _tilesetImage, _map.TileSize, rect, cell, terrain, lookup, layer.Opacity));
            drawnFromTileset = drawnFromTileset || (RpgMakerAutoTile.IsA4(terrain)
                && terrain is not null
                && RpgMakerAutoTile.DrawA4(g, _tilesetImage, _map.TileSize, rect, cell, terrain, lookup, layer.Opacity));
            drawnFromTileset = drawnFromTileset || (WangAutoTile.IsWang(terrain)
                && terrain is not null
                && WangAutoTile.Draw(g, _tilesetImage, _map.TileSize, rect, cell, terrain, _map.TilesetPlan, lookup, layer.Opacity));
            if (!drawnFromTileset && !usesRpgMakerAutoTile && !usesWangAutoTile)
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

    private void DrawTileCollisionShapes(Graphics g)
    {
        if (_map?.TilesetPlan?.Tiles is not { Count: > 0 })
        {
            return;
        }

        var visible = GetVisibleRange();
        var terrains = _map.Terrains.ToDictionary(v => v.Id, StringComparer.OrdinalIgnoreCase);
        using var fill = new SolidBrush(Color.FromArgb(52, 255, 80, 80));
        using var pen = new Pen(Color.FromArgb(220, 255, 80, 80), Math.Max(1f, TilePixels * 0.035f));
        foreach (var layer in _map.Layers.Where(layer => layer.Visible && layer.Kind.Equals("Tile", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (var cell in layer.Tiles)
            {
                if (cell.X < visible.Left || cell.X > visible.Right || cell.Y < visible.Top || cell.Y > visible.Bottom)
                {
                    continue;
                }

                if (!TryResolveCellTilesetTile(cell, terrains, out var tileX, out var tileY)
                    || !TilesetTileMetadataResolver.TryFind(_map.TilesetPlan, tileX, tileY, out var metadata)
                    || metadata.CollisionShapes.Count <= 0)
                {
                    continue;
                }

                var rect = CellToScreenRect(cell.X, cell.Y);
                foreach (var shape in metadata.CollisionShapes)
                {
                    DrawTileCollisionShape(g, rect, shape, fill, pen);
                }
            }
        }
    }

    private void DrawTileCollisionShape(Graphics g, RectangleF tileRect, TileCollisionShapeDefinition shape, Brush fill, Pen pen)
    {
        if (string.Equals(shape.ShapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase))
        {
            var points = shape.Points
                .Select(point => new PointF(
                    tileRect.Left + point.X * tileRect.Width,
                    tileRect.Top + point.Y * tileRect.Height))
                .ToArray();
            if (points.Length < 3)
            {
                return;
            }

            g.FillPolygon(fill, points);
            g.DrawPolygon(pen, points);
            return;
        }

        var rect = new RectangleF(
            tileRect.Left + shape.X * tileRect.Width,
            tileRect.Top + shape.Y * tileRect.Height,
            Math.Max(1f, shape.Width * tileRect.Width),
            Math.Max(1f, shape.Height * tileRect.Height));
        if (string.Equals(shape.ShapeType, TileCollisionShapeTypes.Ellipse, StringComparison.OrdinalIgnoreCase))
        {
            g.FillEllipse(fill, rect);
            g.DrawEllipse(pen, rect);
        }
        else
        {
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    private bool TryResolveCellTilesetTile(
        MapTileCell cell,
        IReadOnlyDictionary<string, MapTerrainDefinition> terrains,
        out int tileX,
        out int tileY)
    {
        tileX = cell.TileX;
        tileY = cell.TileY;
        if (tileX >= 0 && tileY >= 0)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(cell.TerrainId)
            && terrains.TryGetValue(cell.TerrainId, out var terrain)
            && terrain.TileX >= 0
            && terrain.TileY >= 0)
        {
            tileX = terrain.TileX;
            tileY = terrain.TileY;
            return true;
        }

        return false;
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

        var hoverRect = GetBrushScreenRect(_hoverCell);
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
        var text = $"{_map.Width}x{_map.Height}  zoom {MathF.Round(_zoom * 100f)}%  layer {layer}  brush {brush}  size {CurrentBrushSize}";
        var size = g.MeasureString(text, font);
        var rect = new RectangleF(10, Height - size.Height - 14, size.Width + 16, size.Height + 8);
        g.FillRectangle(backBrush, rect);
        g.DrawString(text, font, textBrush, rect.Left + 8, rect.Top + 4);
    }

    private void BeginEditStrokeIfNeeded()
    {
        if (_hasActiveEditStroke || _activeTool == MapEditorTool.Eyedropper)
        {
            return;
        }

        _hasActiveEditStroke = true;
        _activeEditStrokeChanged = false;
        EditStrokeStarting?.Invoke(this, EventArgs.Empty);
    }

    private void EndEditStrokeIfNeeded()
    {
        if (!_hasActiveEditStroke)
        {
            return;
        }

        var changed = _activeEditStrokeChanged;
        _hasActiveEditStroke = false;
        _activeEditStrokeChanged = false;
        EditStrokeCompleted?.Invoke(this, new MapEditStrokeCompletedEventArgs(changed));
    }

    private void ApplyTool(Point cell)
    {
        if (_map is null || _activeLayer is null || !IsInside(cell) || _activeLayer.Locked)
        {
            return;
        }

        var changed = false;
        switch (_activeTool)
        {
            case MapEditorTool.Paint:
                changed = SetBrushCells(_activeLayer, cell);
                break;
            case MapEditorTool.Erase:
                changed = RemoveBrushCells(_activeLayer, cell);
                break;
            case MapEditorTool.Fill:
                changed = FloodFill(cell);
                break;
            case MapEditorTool.Eyedropper:
                PickTerrain(cell);
                return;
        }

        if (!changed)
        {
            return;
        }

        _activeEditStrokeChanged = true;
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
        if (TryCreateRepeatingPattern(out var repeatingPattern))
        {
            for (var y = top; y <= bottom; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    _activeEditStrokeChanged |= ApplyRepeatingPatternCell(_activeLayer, x, y, repeatingPattern, left, top);
                }
            }
        }
        else
        {
            for (var y = top; y <= bottom; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    _activeEditStrokeChanged |= SetCells(_activeLayer, x, y);
                }
            }
        }

        if (_activeEditStrokeChanged)
        {
            MapChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool SetBrushCells(MapLayerDefinition layer, Point center)
    {
        var changed = false;
        foreach (var cell in EnumerateBrushCells(center))
        {
            changed |= SetCells(layer, cell.X, cell.Y);
        }

        return changed;
    }

    private bool RemoveBrushCells(MapLayerDefinition layer, Point center)
    {
        var changed = false;
        foreach (var cell in EnumerateBrushCells(center))
        {
            changed |= RemoveCell(layer, cell.X, cell.Y);
        }

        return changed;
    }

    private bool SetCells(MapLayerDefinition layer, int x, int y)
    {
        var changed = false;
        if (_brushSource == MapBrushSource.Terrain && _selectedTerrainPattern is { Count: > 0 })
        {
            foreach (var patternCell in _selectedTerrainPattern)
            {
                changed |= IsSystemLayer(layer)
                    ? SetCell(layer, x + patternCell.OffsetX, y + patternCell.OffsetY)
                    : SetTerrainCell(layer, x + patternCell.OffsetX, y + patternCell.OffsetY, patternCell.TerrainId, patternCell.Tag);
            }

            return changed;
        }

        if (_brushSource == MapBrushSource.Tileset
            && _terrainFillModeEnabled
            && TryBuildTerrainFillPattern(out var terrainFillPattern))
        {
            foreach (var patternCell in terrainFillPattern)
            {
                changed |= IsSystemLayer(layer)
                    ? SetCell(layer, x + patternCell.OffsetX, y + patternCell.OffsetY)
                    : SetTerrainCell(layer, x + patternCell.OffsetX, y + patternCell.OffsetY, patternCell.TerrainId, patternCell.Tag);
            }

            return changed;
        }

        if (_brushSource == MapBrushSource.Tileset && _selectedTilesetPattern is { Count: > 0 })
        {
            foreach (var patternCell in _selectedTilesetPattern)
            {
                changed |= SetCell(layer, x + patternCell.OffsetX, y + patternCell.OffsetY, patternCell.TileX, patternCell.TileY);
            }

            return changed;
        }

        if (_brushSource == MapBrushSource.Tileset && (_selectedTilesetTileSize.Width > 1 || _selectedTilesetTileSize.Height > 1))
        {
            for (var offsetY = 0; offsetY < _selectedTilesetTileSize.Height; offsetY++)
            {
                for (var offsetX = 0; offsetX < _selectedTilesetTileSize.Width; offsetX++)
                {
                    changed |= SetCell(layer, x + offsetX, y + offsetY, _selectedTilesetTile.X + offsetX, _selectedTilesetTile.Y + offsetY);
                }
            }

            return changed;
        }

        return SetCell(layer, x, y);
    }

    private bool TryBuildTerrainFillPattern(out List<TerrainBrushCell> pattern)
    {
        pattern = [];
        if (_map?.TilesetPlan?.Advanced?.WangSets is null)
        {
            return false;
        }

        if (_selectedTilesetPattern is { Count: > 0 })
        {
            foreach (var tile in _selectedTilesetPattern)
            {
                var terrainId = FindPlannedWangTerrainId(tile.TileX, tile.TileY);
                if (string.IsNullOrWhiteSpace(terrainId))
                {
                    pattern.Clear();
                    return false;
                }

                pattern.Add(new TerrainBrushCell(tile.OffsetX, tile.OffsetY, terrainId));
            }

            return pattern.Count > 0;
        }

        if (_selectedTilesetTile.X < 0 || _selectedTilesetTile.Y < 0)
        {
            return false;
        }

        if (_selectedTilesetTileSize.Width > 1 || _selectedTilesetTileSize.Height > 1)
        {
            for (var offsetY = 0; offsetY < _selectedTilesetTileSize.Height; offsetY++)
            {
                for (var offsetX = 0; offsetX < _selectedTilesetTileSize.Width; offsetX++)
                {
                    var terrainId = FindPlannedWangTerrainId(_selectedTilesetTile.X + offsetX, _selectedTilesetTile.Y + offsetY);
                    if (string.IsNullOrWhiteSpace(terrainId))
                    {
                        pattern.Clear();
                        return false;
                    }

                    pattern.Add(new TerrainBrushCell(offsetX, offsetY, terrainId));
                }
            }

            return pattern.Count > 0;
        }

        var singleTerrainId = FindPlannedWangTerrainId(_selectedTilesetTile.X, _selectedTilesetTile.Y);
        if (string.IsNullOrWhiteSpace(singleTerrainId))
        {
            return false;
        }

        pattern.Add(new TerrainBrushCell(0, 0, singleTerrainId));
        return true;
    }

    private string FindPlannedWangTerrainId(int tileX, int tileY)
    {
        if (_map?.TilesetPlan?.Advanced?.WangSets is null || _map.Terrains is null)
        {
            return string.Empty;
        }

        foreach (var set in _map.TilesetPlan.Advanced.WangSets)
        {
            var tile = set.Tiles.FirstOrDefault(entry => entry.TileX == tileX && entry.TileY == tileY);
            if (tile is null)
            {
                continue;
            }

            var colorIndex = MostUsedWangColor(tile.WangId);
            if (colorIndex <= 0)
            {
                continue;
            }

            var terrainId = WangAutoTile.TerrainId(set.Id, colorIndex);
            if (_map.Terrains.Any(terrain => string.Equals(terrain.Id, terrainId, StringComparison.OrdinalIgnoreCase)))
            {
                return terrainId;
            }
        }

        return string.Empty;
    }

    private static int MostUsedWangColor(IReadOnlyList<int> wangId)
    {
        return wangId
            .Where(value => value > 0)
            .GroupBy(value => value)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault();
    }

    private static bool IsSystemLayer(MapLayerDefinition layer)
    {
        return layer.Kind.Equals("Collision", StringComparison.OrdinalIgnoreCase)
            || layer.Kind.Equals("Region", StringComparison.OrdinalIgnoreCase);
    }

    private bool SetTerrainCell(MapLayerDefinition layer, int x, int y, string terrainId, string tag)
    {
        if (_map is null || x < 0 || y < 0 || x >= _map.Width || y >= _map.Height)
        {
            return false;
        }

        var existing = layer.Tiles.FirstOrDefault(v => v.X == x && v.Y == y);
        if (existing is null)
        {
            existing = new MapTileCell { X = x, Y = y };
            layer.Tiles.Add(existing);
        }

        tag ??= string.Empty;
        if (existing.TerrainId == terrainId
            && existing.TileX == -1
            && existing.TileY == -1
            && existing.Tag == tag
            && !existing.Solid)
        {
            return false;
        }

        existing.TerrainId = terrainId;
        existing.TileX = -1;
        existing.TileY = -1;
        existing.Tag = tag;
        existing.Solid = false;
        return true;
    }

    private bool SetCell(MapLayerDefinition layer, int x, int y, int? overrideTileX = null, int? overrideTileY = null)
    {
        if (_map is null || x < 0 || y < 0 || x >= _map.Width || y >= _map.Height)
        {
            return false;
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
            if (existing.Solid
                && existing.TerrainId.Equals("collision", StringComparison.OrdinalIgnoreCase)
                && existing.TileX == -1
                && existing.TileY == -1
                && string.IsNullOrEmpty(existing.Tag))
            {
                return false;
            }

            existing.Solid = true;
            existing.TerrainId = "collision";
            existing.TileX = -1;
            existing.TileY = -1;
            existing.Tag = string.Empty;
            return true;
        }

        if (layer.Kind.Equals("Region", StringComparison.OrdinalIgnoreCase))
        {
            var terrainId = _selectedTerrain?.Id ?? "region";
            var tag = string.IsNullOrWhiteSpace(existing.Tag) ? "region" : existing.Tag;
            if (existing.TerrainId == terrainId
                && existing.TileX == -1
                && existing.TileY == -1
                && existing.Tag == tag
                && !existing.Solid)
            {
                return false;
            }

            existing.TerrainId = terrainId;
            existing.TileX = -1;
            existing.TileY = -1;
            existing.Tag = tag;
            existing.Solid = false;
            return true;
        }

        if (_brushSource == MapBrushSource.Tileset && _selectedTilesetTile.X >= 0 && _selectedTilesetTile.Y >= 0)
        {
            var tileX = overrideTileX ?? _selectedTilesetTile.X;
            var tileY = overrideTileY ?? _selectedTilesetTile.Y;
            if (_shiftAutoTerrainMode && _shiftTerrain is not null)
            {
                if (existing.TerrainId == _shiftTerrain.Id
                    && existing.TileX == -1
                    && existing.TileY == -1
                    && string.IsNullOrEmpty(existing.Tag)
                    && !existing.Solid)
                {
                    return false;
                }

                existing.TerrainId = _shiftTerrain.Id;
                existing.TileX = -1;
                existing.TileY = -1;
                existing.Tag = string.Empty;
                existing.Solid = false;
                return true;
            }

            if (_selectedTilesetTileIsA1Overlay)
            {
                var tag = A1OverlayTag(tileX, tileY, _selectedTilesetTileIsA1OverlayAnimated);
                if (existing.TileX == tileX
                    && existing.TileY == tileY
                    && existing.Tag == tag)
                {
                    return false;
                }

                existing.TileX = tileX;
                existing.TileY = tileY;
                existing.Tag = tag;
                return true;
            }

            if (existing.TileX == tileX
                && existing.TileY == tileY
                && string.IsNullOrEmpty(existing.TerrainId)
                && string.IsNullOrEmpty(existing.Tag)
                && !existing.Solid)
            {
                return false;
            }

            existing.TileX = tileX;
            existing.TileY = tileY;
            existing.TerrainId = string.Empty;
            existing.Tag = string.Empty;
            existing.Solid = false;
            return true;
        }

        if (ShouldPaintSelectedA3FixedShape() || ShouldPaintSelectedA4FixedWallShape())
        {
            var tag = ShouldPaintSelectedA3FixedShape()
                ? RpgMakerAutoTile.A3FixedShapeTag(RpgMakerAutoTile.A3StandaloneShape)
                : RpgMakerAutoTile.A4FixedShapeTag(RpgMakerAutoTile.A4WallInteriorShape);
            if (existing.TerrainId == _selectedTerrain!.Id
                && existing.TileX == -1
                && existing.TileY == -1
                && existing.Tag == tag
                && !existing.Solid)
            {
                return false;
            }

            existing.TerrainId = _selectedTerrain.Id;
            existing.TileX = -1;
            existing.TileY = -1;
            existing.Tag = tag;
            existing.Solid = false;
            return true;
        }

        if (ShouldPaintSelectedTerrainAsTileset())
        {
            var tag = A1OverlayTag(_selectedTilesetTile.X, _selectedTilesetTile.Y, _selectedTilesetTileIsA1OverlayAnimated);
            if (existing.TileX == _selectedTilesetTile.X
                && existing.TileY == _selectedTilesetTile.Y
                && existing.Tag == tag)
            {
                return false;
            }

            existing.TileX = _selectedTilesetTile.X;
            existing.TileY = _selectedTilesetTile.Y;
            existing.Tag = tag;
            return true;
        }

        if (_selectedTerrain is null)
        {
            if (created)
            {
                RemoveCell(layer, x, y);
            }

            return false;
        }

        if (existing.TerrainId == _selectedTerrain.Id
            && existing.TileX == -1
            && existing.TileY == -1
            && string.IsNullOrEmpty(existing.Tag)
            && !existing.Solid)
        {
            return false;
        }

        existing.TerrainId = _selectedTerrain.Id;
        existing.TileX = -1;
        existing.TileY = -1;
        existing.Tag = string.Empty;
        existing.Solid = false;
        return true;
    }

    private static bool RemoveCell(MapLayerDefinition layer, int x, int y)
    {
        for (var index = layer.Tiles.Count - 1; index >= 0; index--)
        {
            if (layer.Tiles[index].X == x && layer.Tiles[index].Y == y)
            {
                layer.Tiles.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    private bool FloodFill(Point origin)
    {
        if (_map is null || _activeLayer is null || !IsInside(origin))
        {
            return false;
        }

        var lookup = BuildLookup(_activeLayer);
        var hasRepeatingPattern = TryCreateRepeatingPattern(out var repeatingPattern);
        var targetKey = GetFillKey(_activeLayer, lookup.TryGetValue((origin.X, origin.Y), out var originCell) ? originCell : null);
        var replacementKey = GetReplacementKey(_activeLayer);
        if ((!hasRepeatingPattern || repeatingPattern.IsSingleCell)
            && string.Equals(targetKey, replacementKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var queue = new Queue<Point>();
        var visited = new HashSet<(int X, int Y)>();
        var fillCells = new List<Point>();
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

            fillCells.Add(current);
            foreach (var next in EnumerateNeighbors(current))
            {
                if (!IsInside(next) || !visited.Add((next.X, next.Y)))
                {
                    continue;
                }

                queue.Enqueue(next);
            }
        }

        var changed = false;
        if (hasRepeatingPattern)
        {
            foreach (var cell in fillCells)
            {
                changed |= ApplyRepeatingPatternCell(_activeLayer, cell.X, cell.Y, repeatingPattern, origin.X, origin.Y);
            }

            return changed;
        }

        foreach (var cell in fillCells)
        {
            changed |= SetCell(_activeLayer, cell.X, cell.Y);
        }

        return changed;
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

        if (_brushSource == MapBrushSource.Tileset
            && _terrainFillModeEnabled
            && TryBuildTerrainFillPattern(out var terrainFillPattern)
            && terrainFillPattern.Count == 1)
        {
            return terrainFillPattern[0].TerrainId;
        }

        return _brushSource == MapBrushSource.Tileset || ShouldPaintSelectedTerrainAsTileset()
            ? $"atlas:{_selectedTilesetTile.X},{_selectedTilesetTile.Y}"
            : ShouldPaintSelectedA3FixedShape()
                ? $"{_selectedTerrain!.Id}|{RpgMakerAutoTile.A3FixedShapeTag(RpgMakerAutoTile.A3StandaloneShape)}"
            : ShouldPaintSelectedA4FixedWallShape()
                ? $"{_selectedTerrain!.Id}|{RpgMakerAutoTile.A4FixedShapeTag(RpgMakerAutoTile.A4WallInteriorShape)}"
            : _selectedTerrain?.Id ?? string.Empty;
    }

    private bool TryCreateRepeatingPattern(out RepeatingBrushPattern pattern)
    {
        pattern = new RepeatingBrushPattern();

        if (_brushSource == MapBrushSource.Terrain && _selectedTerrainPattern is { Count: > 0 })
        {
            pattern.Width = Math.Max(1, _selectedTerrainPattern.Max(cell => cell.OffsetX) + 1);
            pattern.Height = Math.Max(1, _selectedTerrainPattern.Max(cell => cell.OffsetY) + 1);
            pattern.TerrainCells = _selectedTerrainPattern.ToDictionary(cell => (cell.OffsetX, cell.OffsetY));
            return true;
        }

        if (_brushSource == MapBrushSource.Tileset
            && _terrainFillModeEnabled
            && TryBuildTerrainFillPattern(out var terrainFillPattern))
        {
            pattern.Width = Math.Max(1, terrainFillPattern.Max(cell => cell.OffsetX) + 1);
            pattern.Height = Math.Max(1, terrainFillPattern.Max(cell => cell.OffsetY) + 1);
            pattern.TerrainCells = terrainFillPattern.ToDictionary(cell => (cell.OffsetX, cell.OffsetY));
            return true;
        }

        if (_brushSource == MapBrushSource.Tileset && _selectedTilesetPattern is { Count: > 0 })
        {
            pattern.Width = Math.Max(1, _selectedTilesetPattern.Max(cell => cell.OffsetX) + 1);
            pattern.Height = Math.Max(1, _selectedTilesetPattern.Max(cell => cell.OffsetY) + 1);
            pattern.TilesetCells = _selectedTilesetPattern.ToDictionary(cell => (cell.OffsetX, cell.OffsetY));
            return true;
        }

        if (_brushSource == MapBrushSource.Tileset
            && _selectedTilesetTile.X >= 0
            && _selectedTilesetTile.Y >= 0
            && (_selectedTilesetTileSize.Width > 1 || _selectedTilesetTileSize.Height > 1))
        {
            pattern.Width = _selectedTilesetTileSize.Width;
            pattern.Height = _selectedTilesetTileSize.Height;
            pattern.TilesetCells = [];
            for (var offsetY = 0; offsetY < _selectedTilesetTileSize.Height; offsetY++)
            {
                for (var offsetX = 0; offsetX < _selectedTilesetTileSize.Width; offsetX++)
                {
                    pattern.TilesetCells[(offsetX, offsetY)] = new TilesetBrushCell(
                        offsetX,
                        offsetY,
                        _selectedTilesetTile.X + offsetX,
                        _selectedTilesetTile.Y + offsetY);
                }
            }

            return true;
        }

        return false;
    }

    private bool ApplyRepeatingPatternCell(MapLayerDefinition layer, int x, int y, RepeatingBrushPattern pattern, int originX, int originY)
    {
        if (_map is null || x < 0 || y < 0 || x >= _map.Width || y >= _map.Height)
        {
            return false;
        }

        var patternX = PositiveModulo(x - originX, Math.Max(1, pattern.Width));
        var patternY = PositiveModulo(y - originY, Math.Max(1, pattern.Height));
        if (pattern.TerrainCells.Count > 0)
        {
            return pattern.TerrainCells.TryGetValue((patternX, patternY), out var terrainCell)
                && (IsSystemLayer(layer)
                    ? SetCell(layer, x, y)
                    : SetTerrainCell(layer, x, y, terrainCell.TerrainId, terrainCell.Tag));
        }

        return pattern.TilesetCells.Count > 0
            && pattern.TilesetCells.TryGetValue((patternX, patternY), out var tileCell)
            && SetCell(layer, x, y, tileCell.TileX, tileCell.TileY);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
        {
            return 0;
        }

        var remainder = value % modulo;
        return remainder < 0 ? remainder + modulo : remainder;
    }

    private bool ShouldPaintSelectedTerrainAsTileset()
    {
        return _brushSource == MapBrushSource.Terrain
            && _paintSelectedTerrainAsTileset;
    }

    private bool ShouldPaintSelectedA3FixedShape()
    {
        return _brushSource == MapBrushSource.Terrain
            && _shiftAutoTerrainMode
            && RpgMakerAutoTile.IsA3(_selectedTerrain);
    }

    private bool ShouldPaintSelectedA4FixedWallShape()
    {
        return _brushSource == MapBrushSource.Terrain
            && _shiftAutoTerrainMode
            && RpgMakerAutoTile.IsA4Wall(_selectedTerrain);
    }

    private void UpdateSelectedTerrainPaintMode()
    {
        _paintSelectedTerrainAsTileset = _brushSource == MapBrushSource.Terrain
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

        if (cell.TileX >= 0 && cell.TileY >= 0)
        {
            return $"atlas:{cell.TileX},{cell.TileY}";
        }

        return string.IsNullOrWhiteSpace(cell.Tag)
            ? cell.TerrainId ?? ""
            : $"{cell.TerrainId}|{cell.Tag}";
    }

    private static IEnumerable<Point> EnumerateNeighbors(Point cell)
    {
        yield return new Point(cell.X + 1, cell.Y);
        yield return new Point(cell.X - 1, cell.Y);
        yield return new Point(cell.X, cell.Y + 1);
        yield return new Point(cell.X, cell.Y - 1);
    }

    private IEnumerable<Point> EnumerateBrushCells(Point center)
    {
        var brushSize = CurrentBrushSize;
        var leftOffset = -(brushSize / 2);
        var topOffset = -(brushSize / 2);
        for (var offsetY = 0; offsetY < brushSize; offsetY++)
        {
            for (var offsetX = 0; offsetX < brushSize; offsetX++)
            {
                var cell = new Point(center.X + leftOffset + offsetX, center.Y + topOffset + offsetY);
                if (IsInside(cell))
                {
                    yield return cell;
                }
            }
        }
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

    private RectangleF GetBrushScreenRect(Point center)
    {
        var brushSize = CurrentBrushSize;
        var left = center.X - (brushSize / 2);
        var top = center.Y - (brushSize / 2);
        var footprint = GetBrushFootprintSize();
        var topLeft = WorldToScreen(left, top);
        var bottomRight = WorldToScreen(left + brushSize + footprint.Width - 1, top + brushSize + footprint.Height - 1);
        return RectangleF.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
    }

    private Size GetBrushFootprintSize()
    {
        if (_activeTool != MapEditorTool.Paint)
        {
            return new Size(1, 1);
        }

        if (_brushSource == MapBrushSource.Terrain && _selectedTerrainPattern is { Count: > 0 })
        {
            return new Size(
                _selectedTerrainPattern.Max(cell => cell.OffsetX) + 1,
                _selectedTerrainPattern.Max(cell => cell.OffsetY) + 1);
        }

        if (_brushSource == MapBrushSource.Tileset && _selectedTilesetPattern is { Count: > 0 })
        {
            return new Size(
                _selectedTilesetPattern.Max(cell => cell.OffsetX) + 1,
                _selectedTilesetPattern.Max(cell => cell.OffsetY) + 1);
        }

        if (_brushSource == MapBrushSource.Tileset && _selectedTilesetTile.X >= 0 && _selectedTilesetTile.Y >= 0)
        {
            return _selectedTilesetTileSize;
        }

        return new Size(1, 1);
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
            || _map?.Layers.Any(layer => layer.Tiles.Any(tile => IsAnimatedA1OverlayTag(tile.Tag))) == true
            || _map?.TilesetPlan?.Regions?.Any(region =>
                string.Equals(region.Kind, TilesetRegionKinds.Normal, StringComparison.OrdinalIgnoreCase)
                && region.Animated
                && (region.AnimationFrames.Count > 1 || region.Width * region.Height > 1)) == true;
    }

    private static bool IsAnimatedA1OverlayTag(string? tag)
    {
        return TryParseA1OverlayTag(tag, out _, out _, out var animated) && animated;
    }

    private Color AnimateColor(Color color, MapTerrainDefinition terrain)
    {
        var animationTick = EffectiveAnimationTick;
        var frame = animationTick % Math.Max(1, terrain.AnimationFrames);
        var delta = (int)(Math.Sin((animationTick + frame) * 0.65) * 16);
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
            if (terrain is not null && TilesetAnimationResolver.HasAnimation(terrain, _map.TilesetPlan, tileX, tileY))
            {
                if (TilesetAnimationResolver.TryResolveFrame(terrain, _map.TilesetPlan, tileX, tileY, EffectiveAnimationTick * 160, out var frameTile))
                {
                    tileX = frameTile.X;
                    tileY = frameTile.Y;
                }
                else
                {
                    frameOffset = EffectiveAnimationTick % Math.Max(1, terrain.AnimationFrames);
                }
            }
        }
        else if (TilesetAnimationResolver.TryResolveFrame(null, _map.TilesetPlan, tileX, tileY, EffectiveAnimationTick * 160, out var frameTile))
        {
            tileX = frameTile.X;
            tileY = frameTile.Y;
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
            DrawA1AutoOverlay(g, rect, cell, layer, opacity, tileX, tileY);
            return;
        }

        DrawA1WaterfallOverlay(g, rect, cell, layer, opacity, tileX, tileY);
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

    private void DrawA1WaterfallOverlay(Graphics g, RectangleF rect, MapTileCell cell, MapLayerDefinition layer, float opacity, int tileX, int tileY)
    {
        if (_tilesetImage is null || _map is null)
        {
            return;
        }

        var tileSize = _map.TileSize;
        var quarterSize = tileSize / 2f;
        var frame = EffectiveAnimationTick % 3;
        var sourceX = tileX * tileSize;
        var sourceY = (tileY + frame) * tileSize;
        if (sourceX < 0 || sourceY < 0 || sourceX + tileSize * 2 > _tilesetImage.Width || sourceY + tileSize > _tilesetImage.Height)
        {
            return;
        }

        var sameW = IsSameA1Overlay(layer, cell, -1, 0);
        var sameE = IsSameA1Overlay(layer, cell, 1, 0);
        Point[] quarters;
        if (sameW && sameE)
        {
            quarters = [new Point(2, 0), new Point(1, 0), new Point(2, 1), new Point(1, 1)];
        }
        else if (sameE)
        {
            quarters = [new Point(0, 0), new Point(1, 0), new Point(0, 1), new Point(1, 1)];
        }
        else if (sameW)
        {
            quarters = [new Point(2, 0), new Point(3, 0), new Point(2, 1), new Point(3, 1)];
        }
        else
        {
            quarters = [new Point(0, 0), new Point(3, 0), new Point(0, 1), new Point(3, 1)];
        }

        using var attributes = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);

        for (var index = 0; index < quarters.Length; index++)
        {
            var source = quarters[index];
            g.DrawImage(
                _tilesetImage,
                GetDestinationQuarter(rect, index),
                (int)MathF.Round(sourceX + source.X * quarterSize),
                (int)MathF.Round(sourceY + source.Y * quarterSize),
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

public sealed record TilesetBrushCell(int OffsetX, int OffsetY, int TileX, int TileY);

public sealed record TerrainBrushCell(int OffsetX, int OffsetY, string TerrainId, string Tag = "");

internal sealed class RepeatingBrushPattern
{
    public int Width { get; set; } = 1;

    public int Height { get; set; } = 1;

    public Dictionary<(int X, int Y), TilesetBrushCell> TilesetCells { get; set; } = [];

    public Dictionary<(int X, int Y), TerrainBrushCell> TerrainCells { get; set; } = [];

    public bool IsSingleCell => Width <= 1
        && Height <= 1
        && (TilesetCells.Count > 0 || TerrainCells.Count > 0);
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

public sealed class MapEditStrokeCompletedEventArgs : EventArgs
{
    public MapEditStrokeCompletedEventArgs(bool changed)
    {
        Changed = changed;
    }

    public bool Changed { get; }
}

public sealed class BrushSizePopupRequestedEventArgs : EventArgs
{
    public BrushSizePopupRequestedEventArgs(Point location)
    {
        Location = location;
    }

    public Point Location { get; }
}

public sealed class MapTerrainPickedEventArgs : EventArgs
{
    public MapTerrainPickedEventArgs(string terrainId)
    {
        TerrainId = terrainId;
    }

    public string TerrainId { get; }
}
