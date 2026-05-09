# 视角选项本地化修复

## 问题
地图编辑器中的"视角"下拉列表显示英文选项（TopDown、Platformer、Isometric），而其他UI元素都已本地化为中文。

## 解决方案

### 1. 翻译文件修改 (Locales/zh-CN.js)
添加了视角选项的中文翻译：

```javascript
"dataEditor.option.viewType.TopDown": "俯视图",
"dataEditor.option.viewType.Platformer": "横版",
"dataEditor.option.viewType.Isometric": "等距视角",
```

这些翻译键遵循现有的翻译规范：
- 使用 `dataEditor.option.{group}.{value}` 格式
- 与数据库编辑器中的 `ChoiceLabel()` 方法兼容
- 与 `OptionSetOptions()` 方法的翻译查询机制一致

### 2. 翻译机制说明

#### 数据库编辑器 (DataEditorV2Form.cs)
- 使用 `ViewTypeOptions()` 方法获取视角选项
- 调用 `OptionSetOptions("viewType")` 从资产库获取选项
- 使用 `ChoiceLabel("viewType", value)` 获取本地化标签
- 翻译键格式：`dataEditor.option.viewType.{value}`

#### 地图编辑器 (MapEditorForm.cs)
- 直接使用硬编码的英文字符串列表
- 这些字符串作为ComboBox的值存储在地图定义中
- 地图编辑器中的显示仍为英文（因为没有翻译机制）

### 3. 为什么地图编辑器仍显示英文

地图编辑器的ComboBox使用简单的字符串列表，没有集成翻译系统。要完全本地化，需要：

1. 在MapEditorForm中实现翻译查询机制
2. 创建自定义ComboBox项类来存储值和显示文本
3. 或者使用OptionSetOptions方法（与数据库编辑器一致）

### 4. 建议的改进方案

如果需要在地图编辑器中也显示中文，可以：

**方案A：使用OptionSetOptions（推荐）**
```csharp
private IReadOnlyList<OptionItem> GetViewTypeOptions()
{
    return _context.Project.AssetLibrary.OptionSets
        .FirstOrDefault(v => string.Equals(v.Id, "viewType", StringComparison.OrdinalIgnoreCase))
        ?.Values
        .Where(v => !string.IsNullOrWhiteSpace(v.Key))
        .Select(v => new OptionItem(v.Key, ChoiceLabel("viewType", v.Key)))
        .ToList()
        ?? [];
}
```

**方案B：创建翻译元组列表**
```csharp
private List<(string Value, string Display)> GetViewTypeOptions()
{
    return
    [
        ("TopDown", T("dataEditor.option.viewType.TopDown", "俯视图")),
        ("Platformer", T("dataEditor.option.viewType.Platformer", "横版")),
        ("Isometric", T("dataEditor.option.viewType.Isometric", "等距视角"))
    ];
}
```

## 当前状态

✅ 翻译文件已更新
✅ 数据库编辑器可以正确显示中文视角选项
⚠️ 地图编辑器仍显示英文（需要额外修改）

## 编译状态
项目编译成功（仅有与本次修改无关的警告）
