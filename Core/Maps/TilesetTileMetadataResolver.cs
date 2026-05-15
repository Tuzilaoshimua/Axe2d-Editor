namespace Axe2DEditor.Core.Maps;

public static class TilesetTileMetadataResolver
{
    public static TilesetTileMetadataDefinition? Find(TilesetPlanDefinition? plan, int tileX, int tileY)
    {
        if (plan?.Tiles is null || tileX < 0 || tileY < 0)
        {
            return null;
        }

        return plan.Tiles.FirstOrDefault(tile => tile.TileX == tileX && tile.TileY == tileY);
    }

    public static bool TryFind(TilesetPlanDefinition? plan, int tileX, int tileY, out TilesetTileMetadataDefinition metadata)
    {
        var result = Find(plan, tileX, tileY);
        if (result is null)
        {
            metadata = null!;
            return false;
        }

        metadata = result;
        return true;
    }

    public static string Summary(TilesetTileMetadataDefinition? metadata)
    {
        if (metadata is null)
        {
            return string.Empty;
        }

        var name = string.IsNullOrWhiteSpace(metadata.DisplayName) ? "未命名瓦片" : metadata.DisplayName;
        var pass = metadata.Walkable ? "可通行" : "不可通行";
        var sight = metadata.BlocksSight ? "挡视线" : "不挡视线";
        var category = string.IsNullOrWhiteSpace(metadata.Category) ? "" : $" | {metadata.Category}";
        var tags = metadata.Tags.Count <= 0 ? "" : $" | 标签 {string.Join(",", metadata.Tags.Take(3))}";
        var moveCost = metadata.MoveCost is null ? "规则默认" : $"覆盖 {metadata.MoveCost:0.##}";
        var collision = metadata.CollisionShapes.Count <= 0 ? "" : $" | 碰撞 {metadata.CollisionShapes.Count}";
        return $"{name}{category} | {pass} | {sight} | 移动 {moveCost}{tags}{collision}";
    }
}
