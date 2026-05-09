namespace Axe2DEditor.Editor.Selection;

public sealed class AssetTreeSelection
{
    public AssetTreeSelection(string kind, string id)
    {
        Kind = kind;
        Id = id;
    }

    public string Kind { get; }

    public string Id { get; }
}
