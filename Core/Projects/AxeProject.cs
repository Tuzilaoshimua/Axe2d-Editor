using System.Text.Json;
using System.Text.Json.Serialization;
using Axe2DEditor.Core.Assets;
using Axe2DEditor.Core.Graphs;

namespace Axe2DEditor.Core.Projects;

public sealed class AxeProject
{
    public string EngineVersion { get; set; } = "0.1.0";

    public string Name { get; set; } = "Untitled Axe2D Project";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public AssetLibrary AssetLibrary { get; set; } = new();

    public ProjectPaths Paths { get; set; } = new();

    public List<ProjectTreeNode> HierarchyTree { get; set; } = [];

    public List<ProjectTreeNode> ResourceTree { get; set; } = [];

    public List<EventGraphDefinition> EventGraphs { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? LegacyData { get; set; }
}
