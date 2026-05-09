using Axe2DEditor.Core.Graphs;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Axe2DEditor.Editor.Controls;

internal static class NodeGraphRenderService
{
    public static void DrawScene(
        Graphics g,
        Font font,
        float pixelsPerUnit,
        EventGraphDefinition? graph,
        IReadOnlyDictionary<string, GraphNodeDefinition> visibleNodeCache,
        IReadOnlyDictionary<string, NodeLayoutInfo> layoutCache,
        string? selectedNodeId,
        string? linkSourceNodeId,
        NodeFieldHit? hoverField,
        NodeSocketHit? hoverSocket,
        NodeSocketHit? connectingSocket,
        NodeSocketHit? connectionSnapSocket,
        Point connectionCurrentScreen,
        Func<RectangleF, RectangleF> toScreenRect,
        Func<NodeSocketHit, PointF> getSocketScreenCenter,
        Func<string, string, bool, PointF> getPortCenter,
        Func<string, string> getNodeTitle)
    {
        DrawEdges(g, graph, visibleNodeCache, getPortCenter);
        DrawNodes(g, font, pixelsPerUnit, layoutCache, selectedNodeId, linkSourceNodeId, hoverField, hoverSocket, toScreenRect, getNodeTitle);
        DrawConnectionPreview(g, connectingSocket, connectionSnapSocket, connectionCurrentScreen, getSocketScreenCenter);
    }

    private static void DrawEdges(
        Graphics g,
        EventGraphDefinition? graph,
        IReadOnlyDictionary<string, GraphNodeDefinition> visibleNodeCache,
        Func<string, string, bool, PointF> getPortCenter)
    {
        if (graph is null)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(170, 170, 170), 2.2f);
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;

        foreach (var edge in graph.Edges.Where(edge => visibleNodeCache.ContainsKey(edge.FromNodeId) && visibleNodeCache.ContainsKey(edge.ToNodeId)))
        {
            var from = getPortCenter(edge.FromNodeId, edge.FromPort, true);
            var to = getPortCenter(edge.ToNodeId, edge.ToPort, false);
            DrawBezierEdge(g, pen, from, to, GetValueTypeColor(edge.ValueType));
        }
    }

    private static void DrawNodes(
        Graphics g,
        Font font,
        float pixelsPerUnit,
        IReadOnlyDictionary<string, NodeLayoutInfo> layoutCache,
        string? selectedNodeId,
        string? linkSourceNodeId,
        NodeFieldHit? hoverField,
        NodeSocketHit? hoverSocket,
        Func<RectangleF, RectangleF> toScreenRect,
        Func<string, string> getNodeTitle)
    {
        foreach (var layout in layoutCache.Values.OrderBy(info => info.Bounds.Top).ThenBy(info => info.Bounds.Left))
        {
            DrawNode(g, font, pixelsPerUnit, layout, selectedNodeId, linkSourceNodeId, hoverField, hoverSocket, toScreenRect, getNodeTitle);
        }
    }

    private static void DrawNode(
        Graphics g,
        Font font,
        float pixelsPerUnit,
        NodeLayoutInfo layout,
        string? selectedNodeId,
        string? linkSourceNodeId,
        NodeFieldHit? hoverField,
        NodeSocketHit? hoverSocket,
        Func<RectangleF, RectangleF> toScreenRect,
        Func<string, string> getNodeTitle)
    {
        var bounds = toScreenRect(layout.Bounds);
        var accent = ParseAccentColor(layout.AccentColor);
        var selected = string.Equals(selectedNodeId, layout.NodeId, StringComparison.OrdinalIgnoreCase);
        var linkSource = string.Equals(linkSourceNodeId, layout.NodeId, StringComparison.OrdinalIgnoreCase);
        var zoomFactor = pixelsPerUnit;
        var headerHeight = Math.Max(24f, NodeGraphCanvasStyle.NodeHeaderHeight * zoomFactor);
        var headerPaddingY = Math.Max(2f, 7f * zoomFactor);
        var headerTextHeight = Math.Max(14f, 20f * zoomFactor);
        var kindWidth = Math.Max(68f, 90f * zoomFactor);
        var headerLeftInset = Math.Max(28f, 42f * zoomFactor);
        var headerRightInset = Math.Max(16f, 24f * zoomFactor);

        using var shadowBrush = new SolidBrush(NodeGraphCanvasStyle.ShadowColor);
        using var fillBrush = new SolidBrush(NodeGraphCanvasStyle.NodeFillColor);
        using var borderPen = new Pen(selected ? NodeGraphCanvasStyle.NodeSelectedBorderColor : linkSource ? NodeGraphCanvasStyle.NodeLinkBorderColor : NodeGraphCanvasStyle.NodeBorderColor, selected ? 2.2f : 1.2f);
        using var accentBrush = new SolidBrush(accent);
        var shadowRect = RectangleF.Inflate(bounds, 2, 3);
        using (var path = CreateRoundedPath(shadowRect, NodeGraphCanvasStyle.NodeCornerRadius))
        {
            g.FillPath(shadowBrush, path);
        }

        using (var path = CreateRoundedPath(bounds, NodeGraphCanvasStyle.NodeCornerRadius))
        {
            g.FillPath(fillBrush, path);
            g.DrawPath(borderPen, path);
        }

        var accentRect = new RectangleF(bounds.X, bounds.Y, bounds.Width, headerHeight);
        using (var path = CreateRoundedTopPath(accentRect, NodeGraphCanvasStyle.NodeCornerRadius))
        {
            g.FillPath(accentBrush, path);
        }

        var title = string.IsNullOrWhiteSpace(layout.NodeId) ? layout.DisplayName : getNodeTitle(layout.NodeId);
        var kindText = layout.DisplayName;
        using var kindFont = NodeGraphCanvasStyle.CreateZoomedFont(font, zoomFactor, FontStyle.Bold);
        using var titleFont = NodeGraphCanvasStyle.CreateZoomedFont(font, zoomFactor, FontStyle.Regular);
        NodeGraphCanvasStyle.DrawZoomedText(g, kindText, kindFont, new RectangleF(bounds.X + headerLeftInset, bounds.Y + headerPaddingY, kindWidth, headerTextHeight), Color.White, StringAlignment.Near, StringAlignment.Center);
        NodeGraphCanvasStyle.DrawZoomedText(g, title, titleFont, new RectangleF(bounds.X + headerLeftInset + kindWidth + 8, bounds.Y + headerPaddingY, Math.Max(20f, bounds.Width - (headerLeftInset + headerRightInset + kindWidth + 8)), headerTextHeight), Color.White, StringAlignment.Near, StringAlignment.Center);

        DrawFlowSocket(g, layout.Inputs.FirstOrDefault(), accent, hoverSocket);
        DrawFlowSocket(g, layout.Outputs.FirstOrDefault(), accent, hoverSocket);

        foreach (var field in layout.Fields)
        {
            DrawField(g, font, pixelsPerUnit, field, hoverField, hoverSocket, toScreenRect);
        }
    }

    private static void DrawField(
        Graphics g,
        Font font,
        float pixelsPerUnit,
        NodeFieldHit field,
        NodeFieldHit? hoverField,
        NodeSocketHit? hoverSocket,
        Func<RectangleF, RectangleF> toScreenRect)
    {
        var rect = toScreenRect(field.Bounds);
        var selected = hoverField?.NodeId == field.NodeId && string.Equals(hoverField.Key, field.Key, StringComparison.OrdinalIgnoreCase);
        using var linePen = new Pen(selected ? Color.FromArgb(110, 110, 120) : Color.FromArgb(72, 72, 78), 1f);
        g.DrawLine(linePen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);

        var labelColor = Color.FromArgb(232, 232, 232);
        var valueColor = Color.FromArgb(210, 210, 210);
        var socketOffset = 0f;
        if (field.CanConnectInput)
        {
            var socketRect = toScreenRect(field.LeftSocketBounds);
            var isHover = hoverField?.NodeId == field.NodeId && string.Equals(hoverField.Key, field.Key, StringComparison.OrdinalIgnoreCase);
            var isSnapped = hoverSocket is not null && string.Equals(hoverSocket.NodeId, field.NodeId, StringComparison.OrdinalIgnoreCase) && string.Equals(hoverSocket.PortName, field.PortName, StringComparison.OrdinalIgnoreCase);
            DrawSocketCircle(g, socketRect, GetValueTypeColor(field.ValueType), isHover || isSnapped);
            socketOffset += 22f;
        }

        if (field.CanConnectOutput)
        {
            var socketRect = toScreenRect(field.RightSocketBounds);
            var isHover = hoverField?.NodeId == field.NodeId && string.Equals(hoverField.Key, field.Key, StringComparison.OrdinalIgnoreCase);
            var isSnapped = hoverSocket is not null && string.Equals(hoverSocket.NodeId, field.NodeId, StringComparison.OrdinalIgnoreCase) && string.Equals(hoverSocket.PortName, field.PortName, StringComparison.OrdinalIgnoreCase);
            DrawSocketCircle(g, socketRect, GetValueTypeColor(field.ValueType), isHover || isSnapped);
        }

        var contentLeft = rect.X + 10 + socketOffset;
        var contentRight = field.CanConnectOutput ? rect.Right - 28 : rect.Right - 10;
        var labelRect = new RectangleF(contentLeft, rect.Y, Math.Max(40, (contentRight - contentLeft) * 0.52f), rect.Height);
        var valueRect = new RectangleF(labelRect.Right + 6, rect.Y, Math.Max(20, contentRight - labelRect.Right - 6), rect.Height);

        using var fieldFont = NodeGraphCanvasStyle.CreateZoomedFont(font, pixelsPerUnit, FontStyle.Regular);
        NodeGraphCanvasStyle.DrawZoomedText(g, field.Label, fieldFont, labelRect, labelColor, StringAlignment.Near, StringAlignment.Center);
        NodeGraphCanvasStyle.DrawZoomedText(g, string.IsNullOrWhiteSpace(field.Value) ? "未设置" : field.Value, fieldFont, valueRect, valueColor, StringAlignment.Far, StringAlignment.Center);
    }

    private static void DrawFlowSocket(Graphics g, NodeSocketHit? socket, Color accentColor, NodeSocketHit? hoverSocket)
    {
        if (socket is null)
        {
            return;
        }

        var isHover = hoverSocket is not null && string.Equals(hoverSocket.NodeId, socket.NodeId, StringComparison.OrdinalIgnoreCase) && string.Equals(hoverSocket.PortName, socket.PortName, StringComparison.OrdinalIgnoreCase);
        DrawSocketCircle(g, socket.Bounds, accentColor, isHover);
    }

    private static void DrawSocketCircle(Graphics g, RectangleF rect, Color color, bool isHover)
    {
        using var fill = new SolidBrush(color);
        using var outline = new Pen(Color.White, isHover ? 2f : 1.2f);
        g.FillEllipse(fill, rect);
        g.DrawEllipse(outline, rect.X, rect.Y, rect.Width, rect.Height);
    }

    private static void DrawConnectionPreview(
        Graphics g,
        NodeSocketHit? connectingSocket,
        NodeSocketHit? connectionSnapSocket,
        Point connectionCurrentScreen,
        Func<NodeSocketHit, PointF> getSocketScreenCenter)
    {
        if (connectingSocket is null)
        {
            return;
        }

        var start = getSocketScreenCenter(connectingSocket);
        var end = connectionSnapSocket is not null ? getSocketScreenCenter(connectionSnapSocket) : new PointF(connectionCurrentScreen.X, connectionCurrentScreen.Y);
        using var pen = new Pen(GetValueTypeColor(connectingSocket.ValueType), 2.4f);
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;
        DrawBezierEdge(g, pen, start, end, GetValueTypeColor(connectingSocket.ValueType));
    }

    private static void DrawBezierEdge(Graphics g, Pen pen, PointF start, PointF end, Color tint)
    {
        var dx = Math.Max(80f, Math.Abs(end.X - start.X) * 0.5f);
        var c1 = new PointF(start.X + dx, start.Y);
        var c2 = new PointF(end.X - dx, end.Y);

        using var outlinePen = new Pen(Color.FromArgb(220, 255, 255, 255), pen.Width + 1.6f);
        outlinePen.StartCap = LineCap.Round;
        outlinePen.EndCap = LineCap.Round;
        using var pathPen = new Pen(Color.FromArgb(180, tint), pen.Width);
        pathPen.StartCap = LineCap.Round;
        pathPen.EndCap = LineCap.Round;
        g.DrawBezier(outlinePen, start, c1, c2, end);
        g.DrawBezier(pathPen, start, c1, c2, end);
    }

    private static Color ParseAccentColor(string accentColor)
    {
        return ColorTranslator.FromHtml($"#{accentColor}");
    }

    private static Color GetValueTypeColor(string valueType)
    {
        return valueType switch
        {
            NodePortValueTypes.Flow => Color.FromArgb(255, 182, 72),
            NodePortValueTypes.Bool => Color.FromArgb(90, 200, 120),
            NodePortValueTypes.Int => Color.FromArgb(120, 150, 255),
            NodePortValueTypes.Float => Color.FromArgb(186, 125, 255),
            NodePortValueTypes.String => Color.FromArgb(110, 205, 255),
            NodePortValueTypes.Entity => Color.FromArgb(255, 110, 155),
            NodePortValueTypes.Player => Color.FromArgb(255, 141, 110),
            NodePortValueTypes.Skill => Color.FromArgb(255, 190, 110),
            NodePortValueTypes.Item => Color.FromArgb(145, 220, 122),
            NodePortValueTypes.Area => Color.FromArgb(140, 210, 190),
            NodePortValueTypes.Vector2 => Color.FromArgb(120, 200, 255),
            NodePortValueTypes.AssetRef => Color.FromArgb(170, 170, 180),
            _ => Color.FromArgb(170, 170, 180)
        };
    }

    private static GraphicsPath CreateRoundedPath(RectangleF rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;
        var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.X;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static GraphicsPath CreateRoundedTopPath(RectangleF rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2f;
        var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
        path.CloseFigure();
        return path;
    }
}
