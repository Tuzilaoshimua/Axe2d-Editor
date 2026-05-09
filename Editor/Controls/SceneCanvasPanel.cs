namespace Axe2DEditor.Editor.Controls;

public sealed class SceneCanvasPanel : Panel
{
    private const int RulerTopHeight = 22;
    private const int RulerLeftWidth = 40;
    private const float MinZoomPercent = 10f;
    private const float MaxZoomPercent = 10000f;
    private const float DefaultZoomPercent = 100f;
    private const float BasePixelsPerUnit = 0.8f;
    private const float TargetMajorGridPixelSpacing = 90f;
    private const float MinMinorGridPixelSpacing = 9f;
    private const float MinRulerLabelPixelSpacing = 64f;

    private IReadOnlyList<SceneObjectPoint> _objects = [];
    private bool _showGrid = true;
    private int _selectedIndex = -1;
    private int _draggingIndex = -1;
    private bool _panning;
    private Point _panStart;
    private float _cameraX;
    private float _cameraY;
    private float _zoomPercent = DefaultZoomPercent;
    private SceneToolMode _activeTool = SceneToolMode.Select;
    private SceneGizmoHitTarget _hoverTarget = SceneGizmoHitTarget.None;
    private SceneGizmoHitTarget _draggingTarget = SceneGizmoHitTarget.None;

    public event EventHandler<ScenePointEventArgs>? CanvasClicked;
    public event EventHandler<ScenePointDragEventArgs>? PointDragged;
    public event EventHandler<ScenePointDragEventArgs>? PointDragFinished;
    public event EventHandler? ViewportChanged;
    public event EventHandler<SceneGizmoHoverChangedEventArgs>? GizmoHoverChanged;

    public SceneToolMode ActiveTool
    {
        get => _activeTool;
        set
        {
            _activeTool = value;
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

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = value;
            Invalidate();
        }
    }

    public SceneCanvasPanel()
    {
        SetStyle(ControlStyles.Selectable, true);
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
        Cursor = Cursors.Cross;
    }

    public void SetObjects(IReadOnlyList<SceneObjectPoint> objects)
    {
        _objects = objects;
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

        if (e.Button == MouseButtons.Right)
        {
            _panning = true;
            _panStart = e.Location;
            Cursor = Cursors.SizeAll;
            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var worldX = ScreenToWorldX(e.X);
        var worldY = ScreenToWorldY(e.Y);
        _draggingIndex = HitTestWorld(worldX, worldY);
        CanvasClicked?.Invoke(this, new ScenePointEventArgs(worldX, worldY, e.Button));
        _draggingTarget = SceneGizmoHitTarget.None;

        if (_draggingIndex >= 0 &&
            _draggingIndex == _selectedIndex &&
            RequiresGizmoHit(_activeTool) &&
            !HitTestActiveGizmo(_objects[_selectedIndex], worldX, worldY))
        {
            _draggingIndex = -1;
            _draggingTarget = SceneGizmoHitTarget.None;
        }
        else if (_draggingIndex >= 0 && _draggingIndex == _selectedIndex && RequiresGizmoHit(_activeTool))
        {
            _draggingTarget = GetGizmoHitTarget(_objects[_selectedIndex], worldX, worldY);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var worldX = ScreenToWorldX(e.X);
        var worldY = ScreenToWorldY(e.Y);
        UpdateHoverTarget(worldX, worldY);

        if (_panning)
        {
            var dx = e.X - _panStart.X;
            var dy = e.Y - _panStart.Y;
            _cameraX -= dx / PixelsPerUnit;
            _cameraY -= dy / PixelsPerUnit;
            _panStart = e.Location;
            Invalidate();
            ViewportChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (_draggingIndex < 0 || e.Button != MouseButtons.Left)
        {
            return;
        }

        PointDragged?.Invoke(this, new ScenePointDragEventArgs(_draggingIndex, worldX, worldY, _draggingTarget));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.Button == MouseButtons.Right)
        {
            _panning = false;
            Cursor = Cursors.Cross;
            return;
        }

        if (_draggingIndex < 0)
        {
            return;
        }

        var worldX = ScreenToWorldX(e.X);
        var worldY = ScreenToWorldY(e.Y);
        PointDragFinished?.Invoke(this, new ScenePointDragEventArgs(_draggingIndex, worldX, worldY, _draggingTarget));
        _draggingIndex = -1;
        _draggingTarget = SceneGizmoHitTarget.None;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        SetHoverTarget(SceneGizmoHitTarget.None);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        var bounds = ClientRectangle;
        if (!bounds.Contains(e.Location))
        {
            return;
        }

        var center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
        var beforeX = ScreenToWorldX(center.X);
        var beforeY = ScreenToWorldY(center.Y);

        _zoomPercent = e.Delta > 0 ? _zoomPercent * 1.1f : _zoomPercent / 1.1f;
        _zoomPercent = Math.Clamp(_zoomPercent, MinZoomPercent, MaxZoomPercent);

        var afterX = ScreenToWorldX(center.X);
        var afterY = ScreenToWorldY(center.Y);
        _cameraX += beforeX - afterX;
        _cameraY += beforeY - afterY;

        Invalidate();
        ViewportChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(38, 38, 38));

        var content = ClientRectangle;
        if (_showGrid)
        {
            DrawGrid(g, content);
        }

        DrawAxes(g, content);
        DrawObjects(g);
        DrawSelectedGizmo(g);
        DrawRulerBackground(g);
        DrawRulers(g, content);
    }

    private void DrawGrid(Graphics g, Rectangle content)
    {
        var majorStep = GetDynamicMajorGridStep();
        var minorStep = GetDynamicMinorGridStep(majorStep);

        if (minorStep.HasValue)
        {
            using var minorPen = new Pen(Color.FromArgb(58, 58, 58));
            foreach (var line in EnumerateVerticalGridLines(content, minorStep.Value))
            {
                if (IsMultiple(line.World, majorStep))
                {
                    continue;
                }

                g.DrawLine(minorPen, line.Screen, content.Top, line.Screen, content.Bottom);
            }

            foreach (var line in EnumerateHorizontalGridLines(content, minorStep.Value))
            {
                if (IsMultiple(line.World, majorStep))
                {
                    continue;
                }

                g.DrawLine(minorPen, content.Left, line.Screen, content.Right, line.Screen);
            }
        }

        using var majorPen = new Pen(Color.FromArgb(96, 96, 96));
        foreach (var line in EnumerateVerticalGridLines(content, majorStep))
        {
            g.DrawLine(majorPen, line.Screen, content.Top, line.Screen, content.Bottom);
        }

        foreach (var line in EnumerateHorizontalGridLines(content, majorStep))
        {
            g.DrawLine(majorPen, content.Left, line.Screen, content.Right, line.Screen);
        }
    }

    private void DrawAxes(Graphics g, Rectangle content)
    {
        using var xAxisPen = new Pen(Color.FromArgb(190, 36, 36), 2);
        using var yAxisPen = new Pen(Color.FromArgb(38, 160, 54), 2);

        var axisY = WorldToScreenY(0f);
        if (axisY >= content.Top && axisY <= content.Bottom)
        {
            g.DrawLine(xAxisPen, content.Left, axisY, content.Right, axisY);
        }

        var axisX = WorldToScreenX(0f);
        if (axisX >= content.Left && axisX <= content.Right)
        {
            g.DrawLine(yAxisPen, axisX, content.Top, axisX, content.Bottom);
        }
    }

    private void DrawObjects(Graphics g)
    {
        using var normalBrush = new SolidBrush(Color.FromArgb(238, 238, 238));
        using var selectedBrush = new SolidBrush(Color.FromArgb(0, 145, 220));
        using var outlinePen = new Pen(Color.FromArgb(18, 18, 18));

        for (var i = 0; i < _objects.Count; i++)
        {
            var item = _objects[i];
            var sx = WorldToScreenX(item.X);
            var sy = WorldToScreenY(item.Y);
            var brush = i == _selectedIndex ? selectedBrush : normalBrush;
            var radius = i == _selectedIndex ? 6 : 5;
            var rect = new Rectangle(sx - radius, sy - radius, radius * 2, radius * 2);
            g.FillEllipse(brush, rect);
            g.DrawEllipse(outlinePen, rect);
        }
    }

    private void DrawSelectedGizmo(Graphics g)
    {
        if (_selectedIndex < 0 || _selectedIndex >= _objects.Count)
        {
            return;
        }

        var selected = _objects[_selectedIndex];
        var cx = WorldToScreenX(selected.X);
        var cy = WorldToScreenY(selected.Y);

        switch (_activeTool)
        {
            case SceneToolMode.Move:
                DrawMoveGizmo(g, cx, cy, _hoverTarget);
                break;
            case SceneToolMode.Rotate:
                DrawRotateGizmo(g, cx, cy, _hoverTarget == SceneGizmoHitTarget.RotateRing);
                break;
            case SceneToolMode.Scale:
                DrawScaleGizmo(g, cx, cy, selected.Scale, _hoverTarget == SceneGizmoHitTarget.ScaleHandle);
                break;
        }
    }

    private static void DrawMoveGizmo(Graphics g, int cx, int cy, SceneGizmoHitTarget hover)
    {
        var xColor = hover is SceneGizmoHitTarget.MoveX or SceneGizmoHitTarget.MoveCenter ? Color.OrangeRed : Color.Firebrick;
        var yColor = hover is SceneGizmoHitTarget.MoveY or SceneGizmoHitTarget.MoveCenter ? Color.LimeGreen : Color.ForestGreen;
        using var penX = new Pen(xColor, 2f);
        using var penY = new Pen(yColor, 2f);
        g.DrawLine(penX, cx - 22, cy, cx + 22, cy);
        g.DrawLine(penY, cx, cy - 22, cx, cy + 22);

        using var centerBrush = new SolidBrush(hover == SceneGizmoHitTarget.MoveCenter ? Color.White : Color.Gainsboro);
        g.FillEllipse(centerBrush, cx - 3, cy - 3, 6, 6);
    }

    private static void DrawRotateGizmo(Graphics g, int cx, int cy, bool hover)
    {
        using var ringPen = new Pen(hover ? Color.Khaki : Color.Goldenrod, 2f);
        g.DrawEllipse(ringPen, cx - 26, cy - 26, 52, 52);

        using var markerBrush = new SolidBrush(hover ? Color.Khaki : Color.Goldenrod);
        g.FillEllipse(markerBrush, cx + 20, cy - 3, 6, 6);
    }

    private static void DrawScaleGizmo(Graphics g, int cx, int cy, float scale, bool hover)
    {
        using var pen = new Pen(hover ? Color.DeepSkyBlue : Color.DodgerBlue, 2f);
        var arm = (int)Math.Clamp(18f * Math.Max(0.5f, scale), 12f, 42f);
        g.DrawLine(pen, cx, cy, cx + arm, cy + arm);

        using var handleBrush = new SolidBrush(hover ? Color.DeepSkyBlue : Color.DodgerBlue);
        g.FillRectangle(handleBrush, cx + arm - 4, cy + arm - 4, 8, 8);
    }

    private void DrawRulerBackground(Graphics g)
    {
        // Keep rulers transparent so numbers look over the scene, no framed-strip effect.
    }

    private void DrawRulers(Graphics g, Rectangle content)
    {
        using var tickPen = new Pen(Color.FromArgb(115, 115, 115));
        var textColor = Color.FromArgb(170, 170, 170);
        var majorStep = GetDynamicMajorGridStep();
        var labelStep = GetRulerLabelStep(majorStep);

        var topClip = g.Save();
        g.SetClip(new Rectangle(RulerLeftWidth, 0, Math.Max(1, Width - RulerLeftWidth), RulerTopHeight));
        var lastRight = int.MinValue;
        foreach (var line in EnumerateVerticalGridLines(content, majorStep))
        {
            g.DrawLine(tickPen, line.Screen, 0, line.Screen, RulerTopHeight - 1);
            if (!IsMultiple(line.World, labelStep))
            {
                continue;
            }

            var label = FormatWorldValue(line.World);
            var size = TextRenderer.MeasureText(label, Font);
            var x = line.Screen + 3;
            if (x <= lastRight + 6)
            {
                continue;
            }

            TextRenderer.DrawText(g, label, Font, new Point(x, 2), textColor, TextFormatFlags.NoPadding);
            lastRight = x + size.Width;
        }
        g.Restore(topClip);

        var leftClip = g.Save();
        g.SetClip(new Rectangle(0, RulerTopHeight, RulerLeftWidth, Math.Max(1, Height - RulerTopHeight)));
        var lastBottom = int.MinValue;
        foreach (var line in EnumerateHorizontalGridLines(content, majorStep))
        {
            g.DrawLine(tickPen, 0, line.Screen, RulerLeftWidth - 1, line.Screen);
            if (!IsMultiple(line.World, labelStep))
            {
                continue;
            }

            var label = FormatWorldValue(line.World);
            var size = TextRenderer.MeasureText(label, Font);
            var y = line.Screen + 2;
            if (y <= lastBottom + 4)
            {
                continue;
            }

            TextRenderer.DrawText(g, label, Font, new Rectangle(2, y, RulerLeftWidth - 4, size.Height), textColor, TextFormatFlags.NoPadding | TextFormatFlags.Left);
            lastBottom = y + size.Height;
        }
        g.Restore(leftClip);
    }

    private IEnumerable<(float World, int Screen)> EnumerateVerticalGridLines(Rectangle content, float step)
    {
        if (step <= 0.00001f)
        {
            yield break;
        }

        var leftWorld = ScreenToWorldX(content.Left);
        var rightWorld = ScreenToWorldX(content.Right);
        var start = (int)MathF.Floor(leftWorld / step) - 1;
        var end = (int)MathF.Ceiling(rightWorld / step) + 1;
        for (var index = start; index <= end; index++)
        {
            var world = index * step;
            var screen = WorldToScreenX(world);
            if (screen < content.Left || screen > content.Right)
            {
                continue;
            }

            yield return (world, screen);
        }
    }

    private IEnumerable<(float World, int Screen)> EnumerateHorizontalGridLines(Rectangle content, float step)
    {
        if (step <= 0.00001f)
        {
            yield break;
        }

        var topWorld = ScreenToWorldY(content.Top);
        var bottomWorld = ScreenToWorldY(content.Bottom);
        var start = (int)MathF.Floor(topWorld / step) - 1;
        var end = (int)MathF.Ceiling(bottomWorld / step) + 1;
        for (var index = start; index <= end; index++)
        {
            var world = index * step;
            var screen = WorldToScreenY(world);
            if (screen < content.Top || screen > content.Bottom)
            {
                continue;
            }

            yield return (world, screen);
        }
    }

    private float GetDynamicMajorGridStep()
    {
        var unitsPerPixel = 1f / Math.Max(0.0001f, PixelsPerUnit);
        var rawStep = Math.Max(1f, unitsPerPixel * TargetMajorGridPixelSpacing);
        return ToNiceStepNearest(rawStep);
    }

    private float? GetDynamicMinorGridStep(float majorStep)
    {
        if (majorStep <= 0.00001f)
        {
            return null;
        }

        var divisors = new[] { 10f, 5f, 4f, 2f };
        foreach (var divisor in divisors)
        {
            var step = majorStep / divisor;
            var px = step * PixelsPerUnit;
            if (px >= MinMinorGridPixelSpacing)
            {
                return step;
            }
        }

        return null;
    }

    private float GetRulerLabelStep(float majorStep)
    {
        if (majorStep <= 0.00001f)
        {
            return 1f;
        }

        var px = majorStep * PixelsPerUnit;
        if (px <= 0.01f)
        {
            return majorStep;
        }

        var multiplier = Math.Max(1f, MinRulerLabelPixelSpacing / px);
        return majorStep * ToNiceStepCeil(multiplier);
    }

    private int HitTestWorld(float worldX, float worldY)
    {
        var worldRadius = 8f / PixelsPerUnit;
        var threshold = worldRadius * worldRadius;
        for (var i = 0; i < _objects.Count; i++)
        {
            var dx = _objects[i].X - worldX;
            var dy = _objects[i].Y - worldY;
            if (dx * dx + dy * dy <= threshold)
            {
                return i;
            }
        }

        return -1;
    }

    private bool RequiresGizmoHit(SceneToolMode mode)
    {
        return mode is SceneToolMode.Move or SceneToolMode.Rotate or SceneToolMode.Scale;
    }

    private bool HitTestActiveGizmo(SceneObjectPoint item, float worldX, float worldY)
    {
        return GetGizmoHitTarget(item, worldX, worldY) != SceneGizmoHitTarget.None;
    }

    private SceneGizmoHitTarget GetGizmoHitTarget(SceneObjectPoint item, float worldX, float worldY)
    {
        var dx = worldX - item.X;
        var dy = worldY - item.Y;
        var pxToWorld = 1f / Math.Max(0.0001f, PixelsPerUnit);

        if (_activeTool == SceneToolMode.Move)
        {
            var arm = 22f * pxToWorld;
            var axisTolerance = 6f * pxToWorld;
            var centerRadius = 9f * pxToWorld;
            var hitCenter = dx * dx + dy * dy <= centerRadius * centerRadius;
            if (hitCenter) return SceneGizmoHitTarget.MoveCenter;

            var hitX = MathF.Abs(dy) <= axisTolerance && MathF.Abs(dx) <= arm;
            if (hitX) return SceneGizmoHitTarget.MoveX;
            var hitY = MathF.Abs(dx) <= axisTolerance && MathF.Abs(dy) <= arm;
            if (hitY) return SceneGizmoHitTarget.MoveY;
            return SceneGizmoHitTarget.None;
        }

        if (_activeTool == SceneToolMode.Rotate)
        {
            var radius = 26f * pxToWorld;
            var tolerance = 6f * pxToWorld;
            var distance = MathF.Sqrt(dx * dx + dy * dy);
            return MathF.Abs(distance - radius) <= tolerance ? SceneGizmoHitTarget.RotateRing : SceneGizmoHitTarget.None;
        }

        if (_activeTool == SceneToolMode.Scale)
        {
            var armPx = Math.Clamp(18f * Math.Max(0.5f, item.Scale), 12f, 42f);
            var arm = armPx * pxToWorld;
            var handleHalf = 6f * pxToWorld;
            return MathF.Abs(dx - arm) <= handleHalf && MathF.Abs(dy - arm) <= handleHalf ? SceneGizmoHitTarget.ScaleHandle : SceneGizmoHitTarget.None;
        }

        return SceneGizmoHitTarget.None;
    }

    private void UpdateHoverTarget(float worldX, float worldY)
    {
        if (_selectedIndex < 0 || _selectedIndex >= _objects.Count || !RequiresGizmoHit(_activeTool))
        {
            SetHoverTarget(SceneGizmoHitTarget.None);
            return;
        }

        var target = GetGizmoHitTarget(_objects[_selectedIndex], worldX, worldY);
        SetHoverTarget(target);
    }

    private void SetHoverTarget(SceneGizmoHitTarget target)
    {
        if (_hoverTarget == target)
        {
            return;
        }

        _hoverTarget = target;
        Cursor = _hoverTarget == SceneGizmoHitTarget.None ? Cursors.Cross : Cursors.Hand;
        Invalidate();
        GizmoHoverChanged?.Invoke(this, new SceneGizmoHoverChangedEventArgs(_hoverTarget));
    }

    private static float ToNiceStepNearest(float value)
    {
        if (value <= 1f)
        {
            return 1f;
        }

        var exponent = (int)MathF.Floor(MathF.Log10(value));
        var baseValue = MathF.Pow(10, exponent);
        var fraction = value / baseValue;
        var options = new[] { 1f, 2f, 5f, 10f };

        var best = options[0];
        var bestDiff = MathF.Abs(fraction - best);
        for (var i = 1; i < options.Length; i++)
        {
            var diff = MathF.Abs(fraction - options[i]);
            if (diff < bestDiff)
            {
                best = options[i];
                bestDiff = diff;
            }
        }

        return best * baseValue;
    }

    private static float ToNiceStepCeil(float value)
    {
        if (value <= 1f)
        {
            return 1f;
        }

        var exponent = (int)MathF.Floor(MathF.Log10(value));
        var baseValue = MathF.Pow(10, exponent);
        var fraction = value / baseValue;
        if (fraction <= 1f) return 1f * baseValue;
        if (fraction <= 2f) return 2f * baseValue;
        if (fraction <= 5f) return 5f * baseValue;
        return 10f * baseValue;
    }

    private static bool IsMultiple(float value, float step)
    {
        if (step <= 0.00001f)
        {
            return false;
        }

        var ratio = value / step;
        return MathF.Abs(ratio - MathF.Round(ratio)) <= 0.0005f;
    }

    private static string FormatWorldValue(float value)
    {
        var rounded = MathF.Round(value);
        if (MathF.Abs(value - rounded) <= 0.0005f)
        {
            return rounded.ToString("0");
        }

        return value.ToString("0.##");
    }

    private float PixelsPerUnit => BasePixelsPerUnit * (_zoomPercent / 100f);

    private int WorldToScreenX(float worldX)
    {
        var centerX = Width / 2f;
        return (int)MathF.Round(centerX + (worldX - _cameraX) * PixelsPerUnit);
    }

    private int WorldToScreenY(float worldY)
    {
        var centerY = Height / 2f;
        return (int)MathF.Round(centerY + (worldY - _cameraY) * PixelsPerUnit);
    }

    private float ScreenToWorldX(float screenX)
    {
        var centerX = Width / 2f;
        return _cameraX + (screenX - centerX) / PixelsPerUnit;
    }

    private float ScreenToWorldY(float screenY)
    {
        var centerY = Height / 2f;
        return _cameraY + (screenY - centerY) / PixelsPerUnit;
    }
}

public enum SceneToolMode
{
    Select,
    Move,
    Rotate,
    Scale,
    Rect
}

public enum SceneGizmoHitTarget
{
    None,
    MoveX,
    MoveY,
    MoveCenter,
    RotateRing,
    ScaleHandle
}

public sealed record SceneObjectPoint(string Name, float X, float Y, float Rotation = 0f, float Scale = 1f);

public sealed class ScenePointEventArgs : EventArgs
{
    public ScenePointEventArgs(float x, float y, MouseButtons button)
    {
        X = x;
        Y = y;
        Button = button;
    }

    public float X { get; }

    public float Y { get; }

    public MouseButtons Button { get; }
}

public sealed class ScenePointDragEventArgs : EventArgs
{
    public ScenePointDragEventArgs(int index, float x, float y, SceneGizmoHitTarget target)
    {
        Index = index;
        X = x;
        Y = y;
        Target = target;
    }

    public int Index { get; }

    public float X { get; }

    public float Y { get; }

    public SceneGizmoHitTarget Target { get; }
}

public sealed class SceneGizmoHoverChangedEventArgs : EventArgs
{
    public SceneGizmoHoverChangedEventArgs(SceneGizmoHitTarget target)
    {
        Target = target;
    }

    public SceneGizmoHitTarget Target { get; }
}
