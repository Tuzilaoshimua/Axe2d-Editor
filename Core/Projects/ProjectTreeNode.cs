namespace Axe2DEditor.Core.Projects;

public sealed class ProjectTreeNode
{
    public string Name { get; set; } = "";

    public string Kind { get; set; } = "item";

    public string Type { get; set; } = "";

    public float? PositionX { get; set; }

    public float? PositionY { get; set; }

    public float? Rotation { get; set; }

    public float? Scale { get; set; }

    public string CameraMode { get; set; } = "";

    public string CameraTarget { get; set; } = "";

    public float? CameraSmooth { get; set; }

    public float? CameraZoom { get; set; }

    public List<ProjectTreeNode> Children { get; set; } = [];
}
