using System.Drawing.Drawing2D;
using Axe2DEditor.Core.Maps;

namespace Axe2DEditor.Runtime;

internal sealed class RuntimeRenderer
{
    private const int WorldPadding = 28;

    public void Render(Graphics graphics, Rectangle viewportRect, RuntimeSession session)
    {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.FromArgb(16, 18, 22));

        if (viewportRect.Width <= 0 || viewportRect.Height <= 0)
        {
            return;
        }

        using var backBrush = new LinearGradientBrush(
            viewportRect,
            Color.FromArgb(34, 40, 58),
            Color.FromArgb(18, 20, 28),
            LinearGradientMode.Vertical);
        graphics.FillRectangle(backBrush, viewportRect);

        DrawBackgroundGrid(graphics, viewportRect);
        DrawWorld(graphics, viewportRect, session);
    }

    public PointF ScreenToWorld(Point location, Rectangle viewportRect, RuntimeSession session)
    {
        var worldRect = Rectangle.Inflate(viewportRect, -WorldPadding, -WorldPadding);
        if (worldRect.Width <= 0 || worldRect.Height <= 0)
        {
            return session.CameraFocus;
        }

        var projection = CalculateProjection(worldRect, session.GetWorldBounds(), session.CameraFocus, session.CameraZoom);
        var worldX = ((location.X - projection.OffsetX) / projection.TileSize) + projection.ViewOriginX;
        var worldY = ((location.Y - projection.OffsetY) / projection.TileSize) + projection.ViewOriginY;
        return new PointF(worldX, worldY);
    }

    private static void DrawBackgroundGrid(Graphics graphics, Rectangle rect)
    {
        using var pen = new Pen(Color.FromArgb(42, 48, 62), 1f);
        const int spacing = 48;
        for (var x = rect.Left; x <= rect.Right; x += spacing)
        {
            graphics.DrawLine(pen, x, rect.Top, x, rect.Bottom);
        }

        for (var y = rect.Top; y <= rect.Bottom; y += spacing)
        {
            graphics.DrawLine(pen, rect.Left, y, rect.Right, y);
        }
    }

    private static void DrawWorld(Graphics graphics, Rectangle rect, RuntimeSession session)
    {
        var worldRect = Rectangle.Inflate(rect, -WorldPadding, -WorldPadding);
        if (worldRect.Width <= 0 || worldRect.Height <= 0)
        {
            return;
        }

        using var backdropBrush = new SolidBrush(Color.FromArgb(20, 24, 34));
        using var worldBorderPen = new Pen(Color.FromArgb(90, 160, 220), 2f);
        using (var worldPath = CreateRoundedPath(worldRect, 20))
        {
            graphics.FillPath(backdropBrush, worldPath);
            graphics.DrawPath(worldBorderPen, worldPath);
        }

        if (session.ActiveMap is null)
        {
            using var titleFont = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            using var bodyFont = new Font("Microsoft YaHei UI", 11F);
            using var titleBrush = new SolidBrush(Color.White);
            using var bodyBrush = new SolidBrush(Color.FromArgb(220, 220, 220));
            var center = new PointF(worldRect.Left + worldRect.Width / 2f, worldRect.Top + worldRect.Height / 2f);
            graphics.DrawString("暂无可运行地图", titleFont, titleBrush, center.X - 110, center.Y - 26);
            graphics.DrawString("请先在项目中配置地图，然后重新运行。", bodyFont, bodyBrush, center.X - 152, center.Y + 10);
            return;
        }

        var projection = CalculateProjection(worldRect, session.GetWorldBounds(), session.CameraFocus, session.CameraZoom);

        DrawMapLayers(graphics, session.ActiveMap, projection);
        DrawMapGrid(graphics, projection);

        using var outerPen = new Pen(Color.FromArgb(120, 200, 255), 2f);
        graphics.DrawRectangle(outerPen, projection.OffsetX, projection.OffsetY, projection.VisibleMapPixelWidth, projection.VisibleMapPixelHeight);
        DrawCameraFrame(graphics, projection, session);

        DrawSceneObjects(graphics, session, projection);
        var playerRadius = Math.Max(8f, projection.TileSize * 0.32f);
        var playerScreenX = projection.OffsetX + (session.Player.X - projection.ViewOriginX) * projection.TileSize;
        var playerScreenY = projection.OffsetY + (session.Player.Y - projection.ViewOriginY) * projection.TileSize;
        using var playerGlow = new SolidBrush(Color.FromArgb(70, 120, 210, 255));
        using var playerFill = new SolidBrush(Color.FromArgb(255, 125, 220, 255));
        using var playerStroke = new Pen(Color.White, 1.5f);
        graphics.FillEllipse(playerGlow, playerScreenX - playerRadius * 1.5f, playerScreenY - playerRadius * 1.5f, playerRadius * 3f, playerRadius * 3f);
        graphics.FillEllipse(playerFill, playerScreenX - playerRadius, playerScreenY - playerRadius, playerRadius * 2f, playerRadius * 2f);
        graphics.DrawEllipse(playerStroke, playerScreenX - playerRadius, playerScreenY - playerRadius, playerRadius * 2f, playerRadius * 2f);

        using var labelBrush = new SolidBrush(Color.White);
        using var labelFont = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        using var smallFont = new Font("Consolas", 9.5F);
        graphics.DrawString($"地图预览: {session.ActiveMap.DisplayName}", labelFont, labelBrush, worldRect.Left + 12, worldRect.Top + 12);
        graphics.DrawString($"网格: {session.ActiveMap.Width} x {session.ActiveMap.Height}", smallFont, labelBrush, worldRect.Left + 12, worldRect.Top + 40);
        graphics.DrawString($"角色位置: {session.Player.X:0.0}, {session.Player.Y:0.0}", smallFont, labelBrush, worldRect.Left + 12, worldRect.Top + 58);
        graphics.DrawString($"相机: {session.CameraLabel}", smallFont, labelBrush, worldRect.Left + 12, worldRect.Top + 76);
        graphics.DrawString($"相机焦点: {session.CameraFocus.X:0.0}, {session.CameraFocus.Y:0.0}", smallFont, labelBrush, worldRect.Left + 12, worldRect.Top + 94);
        graphics.DrawString($"对象数: {session.SceneObjects.Count}", smallFont, labelBrush, worldRect.Left + 12, worldRect.Top + 112);
    }

    private static void DrawMapLayers(Graphics graphics, MapDefinition map, RuntimeProjection projection)
    {
        MapDefaults.Normalize(map);
        var terrains = map.Terrains.ToDictionary(v => v.Id, StringComparer.OrdinalIgnoreCase);
        using var tilesetImage = LoadTilesetImage(map.TilesetImagePath);
        foreach (var layer in map.Layers.Where(v => v.Visible))
        {
            if (layer.Kind.Equals("Tile", StringComparison.OrdinalIgnoreCase))
            {
                DrawRuntimeTileLayer(graphics, layer, terrains, tilesetImage, map.TileSize, projection);
            }
            else if (layer.Kind.Equals("Collision", StringComparison.OrdinalIgnoreCase))
            {
                DrawRuntimeOverlayLayer(graphics, layer, projection, Color.FromArgb(88, 230, 70, 80), Color.FromArgb(120, 255, 120, 120));
            }
            else if (layer.Kind.Equals("Region", StringComparison.OrdinalIgnoreCase))
            {
                DrawRuntimeOverlayLayer(graphics, layer, projection, Color.FromArgb(70, 250, 190, 70), Color.FromArgb(130, 255, 218, 110));
            }
        }
    }

    private static void DrawRuntimeTileLayer(
        Graphics graphics,
        MapLayerDefinition layer,
        Dictionary<string, MapTerrainDefinition> terrains,
        Image? tilesetImage,
        int tileSize,
        RuntimeProjection projection)
    {
        var lookup = layer.Tiles.ToDictionary(v => (v.X, v.Y), v => v);
        foreach (var tile in layer.Tiles)
        {
            if (!IsVisible(tile.X, tile.Y, projection))
            {
                continue;
            }

            terrains.TryGetValue(tile.TerrainId, out var terrain);

            var rect = TileRect(tile.X, tile.Y, projection);
            var usesRpgMakerAutoTile = RpgMakerAutoTile.IsA1(terrain) || RpgMakerAutoTile.IsA2(terrain);
            var animationFrame = Environment.TickCount / 160;
            var drawn = RpgMakerAutoTile.IsA1(terrain)
                && terrain is not null
                && RpgMakerAutoTile.DrawA1(graphics, tilesetImage, tileSize, rect, tile, terrain, lookup, layer.Opacity, animationFrame);
            drawn = drawn || (RpgMakerAutoTile.IsA2(terrain)
                && terrain is not null
                && RpgMakerAutoTile.DrawA2(graphics, tilesetImage, tileSize, rect, tile, terrain, lookup, layer.Opacity, animationFrame));
            if (!drawn && !usesRpgMakerAutoTile)
            {
                drawn = DrawRuntimeTilesetTile(graphics, rect, tile, terrain, tilesetImage, tileSize, layer.Opacity);
            }

            if (!drawn && terrain is not null)
            {
                var color = WithOpacity(MapDefaults.ParseColor(terrain.ColorHex, Color.Gray), layer.Opacity);
                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, rect);
                DrawRuntimeAutoEdges(graphics, rect, tile, terrain, lookup, layer.Opacity);
            }
        }
    }

    private static void DrawRuntimeAutoEdges(
        Graphics graphics,
        RectangleF rect,
        MapTileCell tile,
        MapTerrainDefinition terrain,
        Dictionary<(int X, int Y), MapTileCell> lookup,
        float opacity)
    {
        if (!terrain.Rule.StartsWith("Auto", StringComparison.OrdinalIgnoreCase) || rect.Width < 6f)
        {
            return;
        }

        var edge = Math.Max(1f, rect.Width * 0.13f);
        var color = WithOpacity(MapDefaults.ParseColor(terrain.EdgeColorHex, Color.Black), Math.Min(1f, opacity * 0.75f));
        using var brush = new SolidBrush(color);
        if (!IsSameTerrain(lookup, tile.X, tile.Y - 1, tile.TerrainId))
        {
            graphics.FillRectangle(brush, rect.Left, rect.Top, rect.Width, edge);
        }

        if (!IsSameTerrain(lookup, tile.X, tile.Y + 1, tile.TerrainId))
        {
            graphics.FillRectangle(brush, rect.Left, rect.Bottom - edge, rect.Width, edge);
        }

        if (!IsSameTerrain(lookup, tile.X - 1, tile.Y, tile.TerrainId))
        {
            graphics.FillRectangle(brush, rect.Left, rect.Top, edge, rect.Height);
        }

        if (!IsSameTerrain(lookup, tile.X + 1, tile.Y, tile.TerrainId))
        {
            graphics.FillRectangle(brush, rect.Right - edge, rect.Top, edge, rect.Height);
        }
    }

    private static bool DrawRuntimeTilesetTile(
        Graphics graphics,
        RectangleF rect,
        MapTileCell tile,
        MapTerrainDefinition? terrain,
        Image? tilesetImage,
        int tileSize,
        float opacity)
    {
        if (tilesetImage is null || tileSize <= 0)
        {
            return false;
        }

        var tileX = tile.TileX >= 0 ? tile.TileX : terrain?.TileX ?? -1;
        var tileY = tile.TileY >= 0 ? tile.TileY : terrain?.TileY ?? -1;
        if (tileX < 0 || tileY < 0)
        {
            return false;
        }

        var sourceX = tileX * tileSize;
        var sourceY = tileY * tileSize;
        if (sourceX + tileSize > tilesetImage.Width || sourceY + tileSize > tilesetImage.Height)
        {
            return false;
        }

        using var attributes = new System.Drawing.Imaging.ImageAttributes();
        var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = opacity };
        attributes.SetColorMatrix(matrix);
        graphics.DrawImage(
            tilesetImage,
            Rectangle.Round(rect),
            sourceX,
            sourceY,
            tileSize,
            tileSize,
            GraphicsUnit.Pixel,
            attributes);
        return true;
    }

    private static void DrawRuntimeOverlayLayer(Graphics graphics, MapLayerDefinition layer, RuntimeProjection projection, Color fillColor, Color strokeColor)
    {
        using var fill = new SolidBrush(WithOpacity(fillColor, layer.Opacity));
        using var stroke = new Pen(WithOpacity(strokeColor, layer.Opacity), 1f);
        foreach (var tile in layer.Tiles)
        {
            if (!IsVisible(tile.X, tile.Y, projection))
            {
                continue;
            }

            var rect = TileRect(tile.X, tile.Y, projection);
            graphics.FillRectangle(fill, rect);
            graphics.DrawRectangle(stroke, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    private static void DrawMapGrid(Graphics graphics, RuntimeProjection projection)
    {
        using var gridPen = new Pen(Color.FromArgb(42, 56, 74), 1f);
        using var majorPen = new Pen(Color.FromArgb(72, 100, 140), 1.5f);
        var startX = Math.Max(0, (int)MathF.Floor(projection.ViewOriginX));
        var endX = Math.Min((int)projection.WorldBounds.Width, (int)MathF.Ceiling(projection.ViewOriginX + projection.VisibleWorldWidth));
        var startY = Math.Max(0, (int)MathF.Floor(projection.ViewOriginY));
        var endY = Math.Min((int)projection.WorldBounds.Height, (int)MathF.Ceiling(projection.ViewOriginY + projection.VisibleWorldHeight));

        for (var x = startX; x <= endX; x++)
        {
            var screenX = projection.OffsetX + (x - projection.ViewOriginX) * projection.TileSize;
            graphics.DrawLine(x % 8 == 0 ? majorPen : gridPen, screenX, projection.OffsetY, screenX, projection.OffsetY + projection.MapPixelHeight);
        }

        for (var y = startY; y <= endY; y++)
        {
            var screenY = projection.OffsetY + (y - projection.ViewOriginY) * projection.TileSize;
            graphics.DrawLine(y % 8 == 0 ? majorPen : gridPen, projection.OffsetX, screenY, projection.OffsetX + projection.MapPixelWidth, screenY);
        }
    }

    private static void DrawSceneObjects(Graphics graphics, RuntimeSession session, RuntimeProjection projection)
    {
        using var objectTextBrush = new SolidBrush(Color.FromArgb(220, 230, 235));
        using var objectFont = new Font("Microsoft YaHei UI", 8.5F);

        foreach (var obj in session.SceneObjects)
        {
            var px = projection.OffsetX + (obj.X - projection.ViewOriginX) * projection.TileSize;
            var py = projection.OffsetY + (obj.Y - projection.ViewOriginY) * projection.TileSize;
            if (px < projection.OffsetX - 40 || py < projection.OffsetY - 40 || px > projection.OffsetX + projection.VisibleMapPixelWidth + 40 || py > projection.OffsetY + projection.VisibleMapPixelHeight + 40)
            {
                continue;
            }

            var size = Math.Max(8f, projection.TileSize * Math.Max(0.22f, Math.Min(obj.Scale, 1.2f) * 0.18f));
            var rect = new RectangleF(px - size, py - size, size * 2f, size * 2f);
            using var fill = new SolidBrush(GetObjectColor(obj.Category));
            using var stroke = new Pen(Color.White, 1.2f);
            if (string.Equals(obj.Category, "camera", StringComparison.OrdinalIgnoreCase))
            {
                graphics.FillEllipse(fill, rect);
                graphics.DrawEllipse(stroke, rect);
            }
            else if (string.Equals(obj.Category, "ui", StringComparison.OrdinalIgnoreCase))
            {
                graphics.FillRectangle(fill, rect.X, rect.Y, rect.Width, rect.Height);
                graphics.DrawRectangle(stroke, rect.X, rect.Y, rect.Width, rect.Height);
            }
            else
            {
                var diamond = new[]
                {
                    new PointF(px, py - size),
                    new PointF(px + size, py),
                    new PointF(px, py + size),
                    new PointF(px - size, py)
                };
                graphics.FillPolygon(fill, diamond);
                graphics.DrawPolygon(stroke, diamond);
            }
            graphics.DrawString(obj.Name, objectFont, objectTextBrush, px + size + 4f, py - 7f);
        }
    }

    private static Color GetObjectColor(string category)
    {
        return category switch
        {
            "camera" => Color.FromArgb(255, 244, 143, 177),
            "ui" => Color.FromArgb(255, 121, 214, 249),
            _ => Color.FromArgb(255, 255, 198, 109)
        };
    }

    private static bool IsVisible(int x, int y, RuntimeProjection projection)
    {
        return x + 1 >= projection.ViewOriginX
            && y + 1 >= projection.ViewOriginY
            && x <= projection.ViewOriginX + projection.VisibleWorldWidth
            && y <= projection.ViewOriginY + projection.VisibleWorldHeight;
    }

    private static RectangleF TileRect(int x, int y, RuntimeProjection projection)
    {
        return new RectangleF(
            projection.OffsetX + (x - projection.ViewOriginX) * projection.TileSize,
            projection.OffsetY + (y - projection.ViewOriginY) * projection.TileSize,
            projection.TileSize,
            projection.TileSize);
    }

    private static bool IsSameTerrain(Dictionary<(int X, int Y), MapTileCell> lookup, int x, int y, string terrainId)
    {
        return lookup.TryGetValue((x, y), out var other)
            && string.Equals(other.TerrainId, terrainId, StringComparison.OrdinalIgnoreCase);
    }

    private static Color WithOpacity(Color color, float opacity)
    {
        return Color.FromArgb((int)Math.Clamp(color.A * opacity, 0, 255), color.R, color.G, color.B);
    }

    private static Image? LoadTilesetImage(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        using var stream = File.OpenRead(path);
        using var image = Image.FromStream(stream);
        return new Bitmap(image);
    }

    private static RuntimeProjection CalculateProjection(Rectangle worldRect, RectangleF worldBounds)
    {
        return CalculateProjection(worldRect, worldBounds, new PointF(0f, 0f), 1f);
    }

    private static RuntimeProjection CalculateProjection(Rectangle worldRect, RectangleF worldBounds, PointF cameraFocus, float cameraZoom)
    {
        var zoom = Math.Clamp(cameraZoom, 0.25f, 4f);
        var tileWidth = worldRect.Width / Math.Max(1f, worldBounds.Width / zoom);
        var tileHeight = worldRect.Height / Math.Max(1f, worldBounds.Height / zoom);
        var tileSize = Math.Max(8f, Math.Min(tileWidth, tileHeight));
        var visibleWorldWidth = worldRect.Width / tileSize;
        var visibleWorldHeight = worldRect.Height / tileSize;
        var viewOriginX = Math.Clamp(cameraFocus.X - visibleWorldWidth / 2f, 0f, Math.Max(0f, worldBounds.Width - visibleWorldWidth));
        var viewOriginY = Math.Clamp(cameraFocus.Y - visibleWorldHeight / 2f, 0f, Math.Max(0f, worldBounds.Height - visibleWorldHeight));
        var mapPixelWidth = Math.Min(worldBounds.Width, visibleWorldWidth) * tileSize;
        var mapPixelHeight = Math.Min(worldBounds.Height, visibleWorldHeight) * tileSize;
        var offsetX = worldRect.Left + (worldRect.Width - mapPixelWidth) / 2f;
        var offsetY = worldRect.Top + (worldRect.Height - mapPixelHeight) / 2f;
        return new RuntimeProjection(worldBounds, tileSize, mapPixelWidth, mapPixelHeight, mapPixelWidth, mapPixelHeight, offsetX, offsetY, viewOriginX, viewOriginY, visibleWorldWidth, visibleWorldHeight);
    }

    private static void DrawCameraFrame(Graphics graphics, RuntimeProjection projection, RuntimeSession session)
    {
        if (!session.HasExplicitCamera)
        {
            return;
        }

        var frameRect = RectangleF.Inflate(
            new RectangleF(projection.OffsetX, projection.OffsetY, projection.VisibleMapPixelWidth, projection.VisibleMapPixelHeight),
            -1f,
            -1f);
        using var framePen = new Pen(Color.FromArgb(255, 255, 255, 255), 2f) { DashStyle = DashStyle.Dash };
        graphics.DrawRectangle(framePen, frameRect.X, frameRect.Y, frameRect.Width, frameRect.Height);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.Left, rect.Top, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Top, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.Left, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal readonly record struct RuntimeProjection(
    RectangleF WorldBounds,
    float TileSize,
    float MapPixelWidth,
    float MapPixelHeight,
    float VisibleMapPixelWidth,
    float VisibleMapPixelHeight,
    float OffsetX,
    float OffsetY,
    float ViewOriginX,
    float ViewOriginY,
    float VisibleWorldWidth,
    float VisibleWorldHeight);
