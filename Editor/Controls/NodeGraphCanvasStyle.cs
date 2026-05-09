using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace Axe2DEditor.Editor.Controls;

internal static class NodeGraphCanvasStyle
{
    public const int RulerTopHeight = 22;
    public const int RulerLeftWidth = 40;
    public const float MinZoomPercent = 60f;
    public const float MaxZoomPercent = 180f;
    public const float DefaultZoomPercent = 100f;
    public const float TargetMajorGridPixelSpacing = 90f;
    public const float MinMinorGridPixelSpacing = 9f;
    public const float MinRulerLabelPixelSpacing = 64f;
    public const float ConnectionSnapDistance = 18f;

    public const int NodeMinWidth = 280;
    public const int NodeMaxWidth = 420;
    public const int NodeHeaderHeight = 34;
    public const int NodeRowHeight = 26;
    public const int NodeBodyPadding = 12;
    public const int NodeCornerRadius = 14;
    public const int SocketRadius = 6;
    public const int SocketHitPadding = 10;
    public const int CanvasPadding = 80;

    public static readonly Color CanvasBackColor = Color.FromArgb(38, 38, 38);
    public static readonly Color CanvasGridMinor = Color.FromArgb(58, 58, 58);
    public static readonly Color CanvasGridMajor = Color.FromArgb(96, 96, 96);
    public static readonly Color CanvasTextColor = Color.FromArgb(236, 236, 236);
    public static readonly Color CanvasMutedTextColor = Color.FromArgb(180, 180, 180);
    public static readonly Color NodeFillColor = Color.FromArgb(48, 48, 52);
    public static readonly Color NodeBorderColor = Color.FromArgb(92, 96, 104);
    public static readonly Color NodeSelectedBorderColor = Color.FromArgb(77, 156, 255);
    public static readonly Color NodeLinkBorderColor = Color.FromArgb(180, 137, 255);
    public static readonly Color ShadowColor = Color.FromArgb(80, 0, 0, 0);

    public static float GetDynamicMajorGridStep(float pixelsPerUnit)
    {
        var unitsPerPixel = 1f / Math.Max(0.0001f, pixelsPerUnit);
        var rawStep = Math.Max(1f, unitsPerPixel * TargetMajorGridPixelSpacing);
        return ToNiceStepNearest(rawStep);
    }

    public static float? GetDynamicMinorGridStep(float majorStep, float pixelsPerUnit)
    {
        if (majorStep <= 0.00001f)
        {
            return null;
        }

        var divisors = new[] { 10f, 5f, 4f, 2f };
        foreach (var divisor in divisors)
        {
            var step = majorStep / divisor;
            var px = step * pixelsPerUnit;
            if (px >= MinMinorGridPixelSpacing)
            {
                return step;
            }
        }

        return null;
    }

    public static float GetRulerLabelStep(float majorStep, float pixelsPerUnit)
    {
        if (majorStep <= 0.00001f)
        {
            return 1f;
        }

        var px = majorStep * pixelsPerUnit;
        if (px <= 0.01f)
        {
            return majorStep;
        }

        var multiplier = Math.Max(1f, MinRulerLabelPixelSpacing / px);
        return majorStep * ToNiceStepCeil(multiplier);
    }

    public static float ToNiceStepNearest(float value)
    {
        var exponent = (float)Math.Floor(Math.Log10(value));
        var fraction = value / MathF.Pow(10f, exponent);
        var niceFraction = fraction switch
        {
            < 1.5f => 1f,
            < 3.5f => 2f,
            < 7.5f => 5f,
            _ => 10f
        };
        return niceFraction * MathF.Pow(10f, exponent);
    }

    public static float ToNiceStepCeil(float value)
    {
        var exponent = (float)Math.Floor(Math.Log10(value));
        var fraction = value / MathF.Pow(10f, exponent);
        var niceFraction = fraction <= 1f ? 1f : fraction <= 2f ? 2f : fraction <= 5f ? 5f : 10f;
        return niceFraction * MathF.Pow(10f, exponent);
    }

    public static string FormatWorldValue(float value)
    {
        return Math.Abs(value - MathF.Round(value)) < 0.001f
            ? MathF.Round(value).ToString()
            : value.ToString("0.#");
    }

    public static Font CreateZoomedFont(Font baseFont, float zoomFactor, FontStyle style)
    {
        var baseSize = baseFont.SizeInPoints;
        var scaledSize = Math.Max(4.2f, baseSize * Math.Clamp(zoomFactor, 0.30f, 1.8f));
        return new Font(baseFont.FontFamily, scaledSize, style, GraphicsUnit.Point);
    }

    public static void DrawZoomedText(Graphics g, string text, Font font, RectangleF rect, Color color, StringAlignment horizontalAlignment, StringAlignment verticalAlignment)
    {
        using var brush = new SolidBrush(color);
        using var format = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = horizontalAlignment,
            LineAlignment = verticalAlignment,
            Trimming = StringTrimming.EllipsisCharacter
        };

        var previousRendering = g.TextRenderingHint;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.DrawString(text, font, brush, rect, format);
        g.TextRenderingHint = previousRendering;
    }
}
