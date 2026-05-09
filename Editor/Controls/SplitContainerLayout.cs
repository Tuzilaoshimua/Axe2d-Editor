namespace Axe2DEditor.Editor.Controls;

internal static class SplitContainerLayout
{
    private const int CollapseReserve = 80;

    public static void ApplySafe(SplitContainer split, int desiredDistance, int desiredPanel1Min, int desiredPanel2Min)
    {
        if (split.IsDisposed)
        {
            return;
        }

        // WinForms validates min sizes against the current splitter immediately,
        // so constraints must stay loose until the legal distance is known.
        split.Panel1MinSize = 0;
        split.Panel2MinSize = 0;

        var extent = split.Orientation == Orientation.Vertical ? split.Width : split.Height;
        var available = extent - split.SplitterWidth;
        if (extent <= 0 || available <= 0)
        {
            return;
        }

        var panel1Min = Math.Min(Math.Max(0, desiredPanel1Min), Math.Max(0, available / 3));
        var panel2Budget = Math.Max(0, available - panel1Min - CollapseReserve);
        var panel2Min = Math.Min(Math.Max(0, desiredPanel2Min), panel2Budget);
        var minDistance = panel1Min;
        var maxDistance = extent - split.SplitterWidth - panel2Min;

        if (maxDistance < minDistance)
        {
            return;
        }

        var distance = Math.Clamp(desiredDistance, minDistance, maxDistance);
        if (split.SplitterDistance != distance)
        {
            split.SplitterDistance = distance;
        }

        try
        {
            split.Panel1MinSize = Math.Min(panel1Min, split.SplitterDistance);
            split.Panel2MinSize = Math.Min(panel2Min, extent - split.SplitterWidth - split.SplitterDistance);
        }
        catch (InvalidOperationException)
        {
            split.Panel1MinSize = 0;
            split.Panel2MinSize = 0;
        }
    }

    public static void ClampCurrentSafe(SplitContainer split, int desiredPanel1Min, int desiredPanel2Min)
    {
        ApplySafe(split, split.SplitterDistance, desiredPanel1Min, desiredPanel2Min);
    }
}
