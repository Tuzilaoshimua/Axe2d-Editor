# 图集编辑器对齐 Tiled 的后续开发计划

本文档用于指导后续窗口继续开发图集规划、地图编辑器图集面板、自动地形和瓦片元数据功能。目标不是盲目复制 Tiled 的全部功能，而是在保留本项目 RPG Maker A1-A5 兼容层的同时，补齐 Tiled 中对 2D RPG/策略/塔防/生活模拟等项目最关键的图集能力。

## 当前定位

当前项目已经具备三层图集能力：

- 普通模式：直接选择图集格子绘制，支持隐藏瓦片、忽略区域、普通动画帧区域。
- RPG Maker 模式：支持 A1/A2/A3/A4/A5 的官方图集规则和折叠显示，用于兼容 RM 风格自动元件。
- 高级模式：按 Tiled Terrain Set / Wang Set 的方向实现边、角、混合标记，用于非 RM 图集的自动地形规划。

当前实现与 Tiled 的最大差异在于：Tiled 更倾向于“绘制时选择并写入最终 tile”；本项目当前高级模式更倾向于“地图格子存 TerrainId，渲染时根据邻居动态解析最终 tile”。这不是错误，但需要明确记录，因为后续功能设计必须围绕这个数据流保持一致。

## 不可破坏的既有规则

后续开发必须先阅读 `MAP_EDITOR_REGRESSION_RULES.md`，尤其注意：

- 不要改动 A1 C/E 的 Shift 语义。
- 不要让 A2/A3/A4/A5 的绘制逻辑互相污染。
- 不要让 Alt 吸管、Shift 自动地形、黄色选框互相影响。
- 高级模式编辑器预览和运行时必须使用同一套 Wang/自动地形解析链路。
- WinForms UI 必须保证中文文字完整显示，按钮不截断，控件水平和垂直对齐。

## 与 Tiled 的主要差距

### 1. 瓦片属性编辑器

Tiled 支持为 tile 配置自定义属性。本项目目前还缺少“瓦片本身携带游戏语义”的统一入口。

建议新增瓦片元数据模型，例如：

```csharp
public sealed class TilesetTileMetadataDefinition
{
    public int TileX { get; set; }
    public int TileY { get; set; }
    public string DisplayName { get; set; } = "";
    public string Category { get; set; } = "";
    public bool Walkable { get; set; } = true;
    public bool BlocksSight { get; set; }
    public double MoveCost { get; set; } = 1;
    public string MaterialTag { get; set; } = "";
    public string FootstepSoundId { get; set; } = "";
    public Dictionary<string, string> Tags { get; set; } = [];
    public Dictionary<string, string> CustomProperties { get; set; } = [];
}
```

建议 UI：

- 在图集规划窗口或单独“瓦片属性编辑器”中双击瓦片打开。
- 左侧图集选择瓦片，右侧显示属性表。
- 常用字段用明确控件，不要都塞进键值表。
- 自定义属性保留键值表，供高级用户扩展。

优先级：高。

### 2. 瓦片碰撞编辑器

Tiled 有 Tile Collision Editor，可为单个 tile 配置矩形、多边形、椭圆等碰撞对象。本项目目前主要依赖地形和图层规则，缺少 tile 级碰撞。

建议新增模型：

```csharp
public sealed class TilesetTileCollisionDefinition
{
    public int TileX { get; set; }
    public int TileY { get; set; }
    public List<TileCollisionShapeDefinition> Shapes { get; set; } = [];
}

public sealed class TileCollisionShapeDefinition
{
    public string ShapeType { get; set; } = "Rectangle";
    public RectangleF Bounds { get; set; }
    public List<PointF> Points { get; set; } = [];
    public string Tag { get; set; } = "";
}
```

建议 UI：

- 弹出专门碰撞编辑窗口。
- 中间显示单个 tile 放大视图。
- 工具栏提供矩形、多边形、擦除、选择。
- 右侧显示当前碰撞形状列表和属性。

注意：

- 运行时碰撞系统需要能读取这些 tile collision。
- 如果地图上同一 tile 被多处绘制，碰撞应自动复用。

优先级：高。

### 3. 高级模式匹配结果检查和补全提示

高级模式已经有 Wang Set / 地形标签概念，但还需要更接近 Tiled 的可诊断能力。

建议增强“检查图案”：

- 显示当前集合类型：混合、边、角。
- 显示已覆盖组合数、缺失组合数。
- 列出常用缺失组合，例如孤立、上下左右边、内角、外角、十字连接。
- 标注每个组合当前会匹配到哪张 tile 或 fallback 到哪张 tile。
- 对缺失组合提供“跳转到图集标记”或“从当前选区补充”的入口。

优先级：高。

### 4. 概率和变体真正参与绘制

Tiled 的 Wang tile 支持 probability，用于同一匹配结果的随机变体。本项目已有概率字段，但后续需要确认它在地图编辑器和运行时中的行为稳定。

建议规则：

- 相同匹配结果可以有多个候选 tile。
- 按 `Probability` 加权选择。
- 同一地图格子的选择应稳定，不应每帧随机跳变。
- 可以用地图坐标、terrain id、图层 id 组成稳定 seed。
- 如果用户重新随机化，可以提供“刷新变体”命令。

优先级：中高。

### 5. 动画帧升级为瓦片级数据

当前动画帧已经可以在普通区域中使用，但更接近 Tiled 的设计是“每个 tile 可以绑定动画帧”。

建议改造方向：

- 保留现有区域动画作为兼容入口。
- 新增 tile metadata 中的 `AnimationFrames`。
- 图集规划中选择某个 tile 后，可打开动画帧列表。
- 帧引用同一图集内其他 tile，且每帧有 DurationMs。
- 地图编辑器和运行时统一使用同一个动画解析器。

优先级：中。

### 6. 多图集支持

Tiled 一张地图可以引用多个 tileset。本项目当前更接近一张地图绑定一张 tileset image。

建议长期模型：

```csharp
public sealed class MapTilesetReferenceDefinition
{
    public string TilesetId { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public TilesetPlanDefinition Plan { get; set; } = new();
}
```

注意：

- 地图格子需要知道来自哪个 tileset。
- UI 要有图集切换栏。
- 这是大改，不建议优先做。

优先级：中低。

### 7. Tile Object / 对象层打通

Tiled 支持 tile object 和 object layer。本项目有事件触发器和场景对象体系，但图集编辑器还没有明确把“这个 tile 可作为对象放置”打通。

建议：

- 在瓦片属性中增加 `PlaceAsObject`、`ObjectTypeId`、`DefaultTriggerId` 等字段。
- 地图编辑器增加“对象刷”模式，选择 tile 后创建地图对象而不是 tile layer cell。
- 事件触发器可以读取对象类型、标签、属性。

优先级：中。

## 推荐开发阶段

### 第一阶段：补齐游戏语义

目标：让瓦片能被游戏逻辑读取，而不只是显示。

任务：

- 新增瓦片属性模型。
- 新增瓦片属性编辑 UI。
- 让地图编辑器状态栏或属性面板能显示当前 tile 的属性摘要。
- 运行时提供查询 API：按 tile 坐标或地图格子获取 tile metadata。

### 第二阶段：补齐碰撞

目标：让 tile 自身携带碰撞形状。

任务：

- 新增 tile collision 模型。
- 新增碰撞编辑窗口。
- 地图编辑器中提供碰撞预览开关。
- 运行时渲染/碰撞系统读取 tile collision。

### 第三阶段：完善高级自动地形

目标：让高级模式真正接近 Tiled Terrain/Wang Set 的用户体验。

任务：

- 增强检查图案窗口。
- 列出缺失组合和 fallback 结果。
- 让概率变体稳定参与绘制。
- 提供“刷新变体”功能。

### 第四阶段：动画和对象扩展

目标：让瓦片动画和对象放置更通用。

任务：

- 把动画帧迁移/扩展为 tile 级数据。
- 普通区域动画作为兼容层保留。
- 增加 Tile Object / 对象刷模式。

### 第五阶段：多图集

目标：支持大型项目的多素材包管理。

任务：

- 地图支持多个 tileset reference。
- 地图格子记录 tileset id。
- 图集面板支持切换图集。

## 绘制逻辑取舍建议

不要盲目改成 Tiled 的“写死最终 tile”模式。当前项目的动态 TerrainId 解析有明显优势：

- 修改图集规划后，地图可以自动更新。
- 事件触发器和运行时可以按语义读取地形，而不是只读 tile 坐标。
- 更适合作为游戏引擎的核心数据模型。

但需要补一个稳定结果层：

- 编辑器显示和运行时必须共用同一套解析器。
- 对概率变体，必须用稳定 seed 计算。
- 保存地图时可以选择缓存最终 tile，但不要让缓存成为唯一真相。

建议最终模型：

- 地图主数据存语义：TerrainId、TileMetadataId、ObjectId。
- 渲染层解析最终 tile。
- 可选缓存最终 tile，用于性能优化和调试。

## UI 规则

后续所有 UI 改动必须遵守：

- 保持 WinForms 原生风格，不要使用自定义花哨颜色按钮。
- 所有中文文字必须完整显示，不能截断。
- 行高至少保证 CheckBox、RadioButton、Button 完整显示。
- 标签和控件必须水平对齐。
- 复杂参数不要塞进单个文本框，应使用表格、弹窗或专门编辑器。
- 图集规划窗口里可以显示隐藏/忽略区域；地图编辑器图集面板中隐藏区域必须自动补齐，不留空洞。

## 验证清单

每次修改图集或地图绘制相关功能后至少验证：

- `dotnet build Axe2DEditor.csproj -p:UseAppHost=false -p:OutputPath=artifacts\codex-build\`
- 普通模式单格选择和拖选。
- 隐藏瓦片自动补齐。
- 忽略区域不可选。
- A1 C/E Shift 语义。
- A2/A3/A4/A5 折叠显示和绘制。
- 高级模式 Wang 标记、拖动标记、检查图案。
- 地图编辑器和运行时显示是否一致。
