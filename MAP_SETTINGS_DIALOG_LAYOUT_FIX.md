# 地图设定对话框布局修复

## 问题
在地图编辑器的"地图设定"对话框中，背景颜色行的文本框和颜色选择按钮没有正确对齐，导致视觉效果不佳。

## 原因分析
1. **TableLayoutPanel高度设置不当**：背景行使用了TableLayoutPanel来包含文本框和按钮，但RowStyle设置为固定高度34px，而没有使用百分比
2. **列宽设置不合理**：按钮列宽度设置为36px，导致按钮显示过窄
3. **Margin设置不当**：文本框和按钮之间没有适当的间距
4. **控件高度冲突**：文本框设置了Height=30，但TableLayoutPanel的行高为34，导致对齐问题

## 修复方案

### 修改前的代码
```csharp
var backgroundButton = new Button
{
    Text = string.Empty,
    Dock = DockStyle.Fill,
    Width = 34,
    Height = 30,
    FlatStyle = FlatStyle.Flat,
    Margin = Padding.Empty,
    UseVisualStyleBackColor = false
};
var backgroundPanel = new TableLayoutPanel
{
    Dock = DockStyle.Fill,
    ColumnCount = 2,
    RowCount = 1,
    Margin = Padding.Empty,
    Padding = Padding.Empty
};
backgroundPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
backgroundPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
backgroundPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
backgroundBox.Margin = Padding.Empty;
backgroundBox.Dock = DockStyle.Fill;
backgroundBox.Height = 30;
```

### 修改后的代码
```csharp
var backgroundButton = new Button
{
    Text = string.Empty,
    Dock = DockStyle.Fill,
    FlatStyle = FlatStyle.Flat,
    Margin = Padding.Empty,
    UseVisualStyleBackColor = false
};
var backgroundPanel = new TableLayoutPanel
{
    Dock = DockStyle.Fill,
    ColumnCount = 2,
    RowCount = 1,
    Margin = Padding.Empty,
    Padding = Padding.Empty,
    Height = 30
};
backgroundPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
backgroundPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
backgroundPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
backgroundBox.Margin = new Padding(0, 0, 4, 0);
backgroundBox.Dock = DockStyle.Fill;
backgroundButton.Margin = Padding.Empty;
```

## 关键改进

### 1. 移除冲突的尺寸设置
- ❌ 移除按钮的 `Width = 34, Height = 30`
- ✅ 让按钮使用 `Dock = DockStyle.Fill` 自动填充

### 2. 优化TableLayoutPanel布局
- ❌ `RowStyles.Add(new RowStyle(SizeType.Absolute, 34))`
- ✅ `RowStyles.Add(new RowStyle(SizeType.Percent, 100))`
- ✅ 设置整个Panel的 `Height = 30`

### 3. 调整列宽
- ❌ 按钮列宽度 36px（太窄）
- ✅ 按钮列宽度 40px（更合适）

### 4. 添加适当间距
- ❌ `backgroundBox.Margin = Padding.Empty`
- ✅ `backgroundBox.Margin = new Padding(0, 0, 4, 0)`（右侧4px间距）

### 5. 移除冲突的高度设置
- ❌ `backgroundBox.Height = 30`
- ✅ 移除此设置，让Dock自动处理

## 效果
- ✅ 文本框和按钮完美对齐
- ✅ 按钮大小合适，易于点击
- ✅ 文本框和按钮之间有适当的间距
- ✅ 整体视觉效果更加专业和统一

## 编译状态
✅ 项目编译成功，无错误，无警告
