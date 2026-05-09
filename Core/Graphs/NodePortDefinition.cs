namespace Axe2DEditor.Core.Graphs;

public sealed class NodePortDefinition
{
    public string Name { get; set; } = "flow";

    public string Direction { get; set; } = NodePortDirections.Output;

    public string ValueType { get; set; } = NodePortValueTypes.Flow;
}

public static class NodePortDirections
{
    public const string Input = "input";
    public const string Output = "output";
}

public static class NodePortValueTypes
{
    public const string Flow = "Flow";
    public const string Bool = "Bool";
    public const string Int = "Int";
    public const string Float = "Float";
    public const string String = "String";
    public const string Entity = "Entity";
    public const string Player = "Player";
    public const string Skill = "Skill";
    public const string Item = "Item";
    public const string Area = "Area";
    public const string Vector2 = "Vector2";
    public const string AssetRef = "AssetRef";
}
