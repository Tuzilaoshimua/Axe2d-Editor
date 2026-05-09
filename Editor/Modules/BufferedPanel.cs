namespace Axe2DEditor.Editor.Modules;

internal sealed class BufferedPanel : Panel
{
    public BufferedPanel()
    {
        DoubleBuffered = true;
        ResizeRedraw = true;
    }
}
