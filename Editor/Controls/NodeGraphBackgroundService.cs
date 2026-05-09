using System.Drawing.Drawing2D;

namespace Axe2DEditor.Editor.Controls;

internal static class NodeGraphBackgroundService
{
    public static void DrawEmptyState(Graphics g, Font font, Size clientSize)
    {
        var text = "请选择一个触发器进入节点画布";
        var size = TextRenderer.MeasureText(text, font);
        TextRenderer.DrawText(
            g,
            text,
            font,
            new Point((clientSize.Width - size.Width) / 2, (clientSize.Height - size.Height) / 2),
            NodeGraphCanvasStyle.CanvasMutedTextColor,
            TextFormatFlags.NoPadding);
    }

    public static void DrawGrid(
        Graphics g,
        Rectangle content,
        float pixelsPerUnit,
        Func<PointF, PointF> toScreen,
        Func<PointF, PointF> screenToWorld)
    {
        var majorStep = NodeGraphCanvasStyle.GetDynamicMajorGridStep(pixelsPerUnit);
        var minorStep = NodeGraphCanvasStyle.GetDynamicMinorGridStep(majorStep, pixelsPerUnit);

        if (minorStep.HasValue)
        {
            using var minorPen = new Pen(NodeGraphCanvasStyle.CanvasGridMinor);
            foreach (var line in EnumerateVerticalGridLines(content, minorStep.Value, toScreen, screenToWorld))
            {
                if (IsMultiple(line.World, majorStep))
                {
                    continue;
                }

                g.DrawLine(minorPen, line.Screen, content.Top, line.Screen, content.Bottom);
            }

            foreach (var line in EnumerateHorizontalGridLines(content, minorStep.Value, toScreen, screenToWorld))
            {
                if (IsMultiple(line.World, majorStep))
                {
                    continue;
                }

                g.DrawLine(minorPen, content.Left, line.Screen, content.Right, line.Screen);
            }
        }

        using var majorPen = new Pen(NodeGraphCanvasStyle.CanvasGridMajor);
        foreach (var line in EnumerateVerticalGridLines(content, majorStep, toScreen, screenToWorld))
        {
            g.DrawLine(majorPen, line.Screen, content.Top, line.Screen, content.Bottom);
        }

        foreach (var line in EnumerateHorizontalGridLines(content, majorStep, toScreen, screenToWorld))
        {
            g.DrawLine(majorPen, content.Left, line.Screen, content.Right, line.Screen);
        }
    }

    public static void DrawRulerBackground(Graphics g)
    {
        // Intentionally empty: keep the canvas open and unframed.
    }

    public static void DrawRulers(
        Graphics g,
        Rectangle content,
        Font font,
        float pixelsPerUnit,
        int width,
        int height,
        Func<PointF, PointF> toScreen,
        Func<PointF, PointF> screenToWorld)
    {
        using var tickPen = new Pen(Color.FromArgb(115, 115, 115));
        var textColor = NodeGraphCanvasStyle.CanvasMutedTextColor;
        var majorStep = NodeGraphCanvasStyle.GetDynamicMajorGridStep(pixelsPerUnit);
        var labelStep = NodeGraphCanvasStyle.GetRulerLabelStep(majorStep, pixelsPerUnit);

        var topClip = g.Save();
        g.SetClip(new Rectangle(NodeGraphCanvasStyle.RulerLeftWidth, 0, Math.Max(1, width - NodeGraphCanvasStyle.RulerLeftWidth), NodeGraphCanvasStyle.RulerTopHeight));
        var lastRight = int.MinValue;
        foreach (var line in EnumerateVerticalGridLines(content, majorStep, toScreen, screenToWorld))
        {
            g.DrawLine(tickPen, line.Screen, 0, line.Screen, NodeGraphCanvasStyle.RulerTopHeight - 1);
            if (!IsMultiple(line.World, labelStep))
            {
                continue;
            }

            var label = NodeGraphCanvasStyle.FormatWorldValue(line.World);
            var size = TextRenderer.MeasureText(label, font);
            var x = line.Screen + 3;
            if (x <= lastRight + 6)
            {
                continue;
            }

            TextRenderer.DrawText(g, label, font, new Point(x, 2), textColor, TextFormatFlags.NoPadding);
            lastRight = x + size.Width;
        }
        g.Restore(topClip);

        var leftClip = g.Save();
        g.SetClip(new Rectangle(0, NodeGraphCanvasStyle.RulerTopHeight, NodeGraphCanvasStyle.RulerLeftWidth, Math.Max(1, height - NodeGraphCanvasStyle.RulerTopHeight)));
        var lastBottom = int.MinValue;
        foreach (var line in EnumerateHorizontalGridLines(content, majorStep, toScreen, screenToWorld))
        {
            g.DrawLine(tickPen, 0, line.Screen, NodeGraphCanvasStyle.RulerLeftWidth - 1, line.Screen);
            if (!IsMultiple(line.World, labelStep))
            {
                continue;
            }

            var label = NodeGraphCanvasStyle.FormatWorldValue(line.World);
            var size = TextRenderer.MeasureText(label, font);
            var y = line.Screen + 2;
            if (y <= lastBottom + 4)
            {
                continue;
            }

            TextRenderer.DrawText(g, label, font, new Rectangle(2, y, NodeGraphCanvasStyle.RulerLeftWidth - 4, size.Height), textColor, TextFormatFlags.NoPadding | TextFormatFlags.Left);
            lastBottom = y + size.Height;
        }
        g.Restore(leftClip);
    }

    private static IEnumerable<(float World, int Screen)> EnumerateVerticalGridLines(
        Rectangle content,
        float step,
        Func<PointF, PointF> toScreen,
        Func<PointF, PointF> screenToWorld)
    {
        if (step <= 0.00001f)
        {
            yield break;
        }

        var leftWorld = screenToWorld(new PointF(content.Left, 0)).X;
        var rightWorld = screenToWorld(new PointF(content.Right, 0)).X;
        var start = (int)MathF.Floor(leftWorld / step) - 1;
        var end = (int)MathF.Ceiling(rightWorld / step) + 1;
        for (var index = start; index <= end; index++)
        {
            var world = index * step;
            var screen = toScreen(new PointF(world, 0)).X;
            if (screen < content.Left || screen > content.Right)
            {
                continue;
            }

            yield return (world, (int)MathF.Round(screen));
        }
    }

    private static IEnumerable<(float World, int Screen)> EnumerateHorizontalGridLines(
        Rectangle content,
        float step,
        Func<PointF, PointF> toScreen,
        Func<PointF, PointF> screenToWorld)
    {
        if (step <= 0.00001f)
        {
            yield break;
        }

        var topWorld = screenToWorld(new PointF(0, content.Top)).Y;
        var bottomWorld = screenToWorld(new PointF(0, content.Bottom)).Y;
        var start = (int)MathF.Floor(topWorld / step) - 1;
        var end = (int)MathF.Ceiling(bottomWorld / step) + 1;
        for (var index = start; index <= end; index++)
        {
            var world = index * step;
            var screen = toScreen(new PointF(0, world)).Y;
            if (screen < content.Top || screen > content.Bottom)
            {
                continue;
            }

            yield return (world, (int)MathF.Round(screen));
        }
    }

    private static bool IsMultiple(float value, float step)
    {
        if (step <= 0.00001f)
        {
            return true;
        }

        var ratio = value / step;
        return Math.Abs(ratio - MathF.Round(ratio)) < 0.0005f;
    }
}
