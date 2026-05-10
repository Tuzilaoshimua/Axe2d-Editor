# 地图编辑器 A1 自动元件维护规则

更广泛的地图编辑器回归守则见 [MAP_EDITOR_REGRESSION_RULES.md](./MAP_EDITOR_REGRESSION_RULES.md)。

## 必须遵守

- A1 C 海洋装饰和 E 瀑布有两套绘制语义：默认不按 Shift 是覆盖模式，按住 Shift 是自动地形模式。
- 不按 Shift 时，C/E 写入 `TileX`、`TileY` 和 `a1Overlay:*` 标签；按住 Shift 时写入 `TerrainId`，走 `RpgMakerAutoTile.DrawA1`。
- C 装饰和 E 瀑布不能共用同一套补齐表。C 是 2x3 地面自动元件 quarter 表；E 是 2x1 瀑布左右邻接 quarter 表。
- E 瀑布默认覆盖模式不能整格动画绘制，必须按左右邻接拆成四个半格 quarter 组合，否则横向连续瀑布会重复整块图案。
- ALT 临时吸管只能临时取样，不允许修改 `ActiveTool`、Shift 状态、图集黄色选框状态或当前绘制模式。

## 已犯过的错误

- 把 C/E 默认模式和 Shift 模式反复写反，导致用户按住 Shift 和不按 Shift 的结果互换。
- 只修 C 装饰补齐表，漏掉 E 瀑布的专用瀑布 quarter 表。
- 把 E 瀑布当成普通 animated overlay 整格绘制，导致连续瀑布重复整块纹理。
- 为快捷键或吸管功能改动公共选择状态，间接破坏 A1 自动元件绘制模式。

## 修改前检查

- 修改 A1 绘制前，先确认目标是默认覆盖模式还是 Shift 自动地形模式。
- 修改 C 装饰时，对照 `DrawA1AutoOverlay`；修改 E 瀑布时，对照 `DrawA1WaterfallOverlay`。
- 修改 Shift/ALT 快捷键时，不要改 `SelectTilesetTile`、`ShouldPaintSelectedTerrainAsTileset` 和 A1 overlay 绘制路径，除非当前任务明确要求。
- 修改后必须至少构建一次：`dotnet build Axe2DEditor.csproj`。
