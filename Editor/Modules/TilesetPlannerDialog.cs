using Axe2DEditor.Core.Maps;
using Axe2DEditor.Editor.Controls;

namespace Axe2DEditor.Editor.Modules;

public sealed class TilesetPlannerDialog : Form
{
    private readonly TilesetPlannerCanvas _canvas = new();
    private readonly ListView _regionList = new();
    private readonly RadioButton _normalButton = new();
    private readonly RadioButton _ignoredButton = new();
    private readonly ComboBox _modeCombo = new();
    private readonly ComboBox _rpgKindCombo = new();
    private readonly ComboBox _a1VariantCombo = new();
    private readonly Button _autoRpgButton = new();
    private readonly Button _clearA2Button = new();
    private readonly Button _addButton = new();
    private readonly Button _deleteButton = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();
    private readonly Label _selectionLabel = new();
    private readonly Label _hintLabel = new();
    private readonly Image _image;
    private readonly int _tileSize;
    private readonly List<TilesetRegionDefinition> _regions;
    private TableLayoutPanel? _planPanel;
    private TableLayoutPanel? _sidePanel;
    private FlowLayoutPanel? _modePanel;
    private string _mode;
    private string _rpgKind;

    public TilesetPlannerDialog(Image image, int tileSize, TilesetPlanDefinition? existingPlan)
    {
        _image = new Bitmap(image);
        _tileSize = Math.Max(8, tileSize);
        _regions = CloneRegions(existingPlan?.Regions ?? []);
        _mode = existingPlan?.Mode ?? TilesetPlanModes.Normal;
        _rpgKind = existingPlan?.RpgMakerKind ?? RpgMakerTilesetKinds.A2;

        Text = "图集规划";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        MinimumSize = new Size(980, 640);
        Size = new Size(1180, 760);
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildUi();
        RefreshRegionList();
        UpdateSelectionText(Rectangle.Empty);
    }

    public TilesetPlanDefinition Plan => new()
    {
        TileSize = _tileSize,
        Mode = _mode,
        RpgMakerKind = _rpgKind,
        RpgMakerLayout = TilesetPlanModes.RpgMaker.Equals(_mode, StringComparison.OrdinalIgnoreCase) ? RpgMakerTilesetLayouts.Standard : RpgMakerTilesetLayouts.Standard,
        Regions = CloneRegions(_regions)
    };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _image.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildUi()
    {
        var root = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        _canvas.Dock = DockStyle.Fill;
        _canvas.SetTileset(_image, _tileSize, _regions);
        _canvas.SelectionChanged += (_, e) => UpdateSelectionText(e.Selection);
        root.Panel1.Controls.Add(_canvas);

        _sidePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            AutoScroll = true,
            RowCount = 9
        };
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            Text = "区域类型",
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var planPanel = BuildPlanPanel();

        _modePanel = BuildModePanel();

        _selectionLabel.Dock = DockStyle.Fill;
        _selectionLabel.TextAlign = ContentAlignment.MiddleLeft;

        _addButton.Text = "添加选区";
        _addButton.Dock = DockStyle.Left;
        _addButton.Width = 108;
        _addButton.Click += (_, _) => AddSelection();

        _regionList.Dock = DockStyle.Fill;
        _regionList.View = View.Details;
        _regionList.FullRowSelect = true;
        _regionList.HideSelection = false;
        _regionList.MultiSelect = false;
        _regionList.Columns.Add("名称", 132);
        _regionList.Columns.Add("类型", 112);
        _regionList.Columns.Add("区域", 104);
        _regionList.SelectedIndexChanged += (_, _) =>
        {
            var region = _regionList.SelectedItems.Count > 0
                ? _regionList.SelectedItems[0].Tag as TilesetRegionDefinition
                : null;
            _canvas.SelectedRegion = region;
            _deleteButton.Enabled = region is not null;
        };

        _deleteButton.Text = "删除区域";
        _deleteButton.Dock = DockStyle.Left;
        _deleteButton.Width = 108;
        _deleteButton.Enabled = false;
        _deleteButton.Click += (_, _) => DeleteSelectedRegion();

        _hintLabel.Dock = DockStyle.Fill;
        _hintLabel.TextAlign = ContentAlignment.MiddleLeft;
        _hintLabel.ForeColor = SystemColors.GrayText;
        _hintLabel.AutoSize = false;
        _hintLabel.Text = "拖拽框选格子区域。A2 区域会按 2x3 块生成自动地形。";

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0)
        };
        _okButton.Text = "确定";
        _okButton.Width = 92;
        _okButton.Height = 32;
        _okButton.DialogResult = DialogResult.OK;
        _cancelButton.Text = "取消";
        _cancelButton.Width = 92;
        _cancelButton.Height = 32;
        _cancelButton.DialogResult = DialogResult.Cancel;
        buttonPanel.Controls.Add(_okButton);
        buttonPanel.Controls.Add(_cancelButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        _sidePanel.Controls.Add(title, 0, 0);
        _sidePanel.Controls.Add(planPanel, 0, 1);
        _sidePanel.Controls.Add(_modePanel, 0, 2);
        _sidePanel.Controls.Add(_selectionLabel, 0, 3);
        _sidePanel.Controls.Add(_addButton, 0, 4);
        _sidePanel.Controls.Add(_regionList, 0, 5);
        _sidePanel.Controls.Add(_deleteButton, 0, 6);
        _sidePanel.Controls.Add(_hintLabel, 0, 7);
        _sidePanel.Controls.Add(buttonPanel, 0, 8);
        root.Panel2.Controls.Add(_sidePanel);
        Controls.Add(root);

        root.Resize += (_, _) => SplitContainerLayout.ClampCurrentSafe(root, 360, 260);
        Shown += (_, _) => BeginInvoke(new Action(() => SplitContainerLayout.ApplySafe(root, 760, 360, 260)));
        UpdatePlanControls();
    }

    private Control BuildPlanPanel()
    {
        _planPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty
        };
        _planPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        _planPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _planPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _planPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _planPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        _planPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        ConfigureCombo(_modeCombo, [
            new PlannerComboItem(TilesetPlanModes.Normal, "普通模式"),
            new PlannerComboItem(TilesetPlanModes.RpgMaker, "RPG Maker"),
            new PlannerComboItem(TilesetPlanModes.Advanced, "高级模式")
        ], _mode);
        ConfigureCombo(_rpgKindCombo, [
            new PlannerComboItem(RpgMakerTilesetKinds.A1, "A1 官方动画"),
            new PlannerComboItem(RpgMakerTilesetKinds.A2, "A2 地面"),
            new PlannerComboItem(RpgMakerTilesetKinds.A3, "A3 建筑外墙"),
            new PlannerComboItem(RpgMakerTilesetKinds.A4, "A4 墙壁/屋顶"),
            new PlannerComboItem(RpgMakerTilesetKinds.A5, "A5 普通瓦片")
        ], _rpgKind);
        ConfigureCombo(_a1VariantCombo, [
            new PlannerComboItem(RpgMakerA1RegionVariants.Ocean, "A 海洋 6x3"),
            new PlannerComboItem(RpgMakerA1RegionVariants.DeepSea, "B 深海 6x3"),
            new PlannerComboItem(RpgMakerA1RegionVariants.OceanDecor, "C 海洋装饰 2x3"),
            new PlannerComboItem(RpgMakerA1RegionVariants.Water, "D 水面 6x3"),
            new PlannerComboItem(RpgMakerA1RegionVariants.Waterfall, "E 瀑布 2x3")
        ], RpgMakerA1RegionVariants.Ocean);
        _modeCombo.SelectedIndexChanged += (_, _) =>
        {
            _mode = SelectedComboValue(_modeCombo, TilesetPlanModes.Normal);
            UpdatePlanControls();
        };
        _rpgKindCombo.SelectedIndexChanged += (_, _) =>
        {
            _rpgKind = SelectedComboValue(_rpgKindCombo, RpgMakerTilesetKinds.A2);
            UpdatePlanControls();
        };
        _autoRpgButton.Text = "自动生成";
        _autoRpgButton.Dock = DockStyle.Fill;
        _autoRpgButton.Margin = new Padding(0, 4, 6, 0);
        _autoRpgButton.Click += (_, _) => GenerateStandardRpgRegions();
        _clearA2Button.Text = "清空选区";
        _clearA2Button.Dock = DockStyle.Fill;
        _clearA2Button.Margin = new Padding(0, 4, 0, 0);
        _clearA2Button.Click += (_, _) => ClearA2Regions();

        var actionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actionPanel.Controls.Add(_autoRpgButton);
        actionPanel.Controls.Add(_clearA2Button);

        AddPlanRow(_planPanel, 0, "类型", _modeCombo);
        AddPlanRow(_planPanel, 1, "RM类型", _rpgKindCombo);
        AddPlanRow(_planPanel, 2, "A1类型", _a1VariantCombo);
        AddPlanRow(_planPanel, 3, "", actionPanel);
        return _planPanel;
    }

    private FlowLayoutPanel BuildModePanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        ConfigureModeButton(_normalButton, "普通瓦片区域", true);
        ConfigureModeButton(_ignoredButton, "忽略区域");

        _normalButton.Margin = new Padding(0, 0, 12, 6);
        _ignoredButton.Margin = new Padding(0, 0, 0, 6);

        panel.Controls.Add(_normalButton);
        panel.Controls.Add(_ignoredButton);
        return panel;
    }

    private static void ConfigureCombo(ComboBox combo, PlannerComboItem[] values, string selected)
    {
        combo.Dock = DockStyle.Fill;
        combo.Margin = Padding.Empty;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.Items.AddRange(values);
        combo.SelectedItem = values.FirstOrDefault(v => string.Equals(v.Value, selected, StringComparison.OrdinalIgnoreCase));
        if (combo.SelectedIndex < 0 && combo.Items.Count > 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    private static string SelectedComboValue(ComboBox combo, string fallback)
    {
        return combo.SelectedItem is PlannerComboItem item ? item.Value : fallback;
    }

    private static void AddPlanRow(TableLayoutPanel panel, int row, string label, Control editor)
    {
        panel.Controls.Add(new Label { Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        panel.Controls.Add(editor, 1, row);
    }

    private void UpdatePlanControls()
    {
        var isRpgMaker = string.Equals(_mode, TilesetPlanModes.RpgMaker, StringComparison.OrdinalIgnoreCase);
        var showRpgControls = isRpgMaker;
        var isA1Kind = isRpgMaker && string.Equals(_rpgKind, RpgMakerTilesetKinds.A1, StringComparison.OrdinalIgnoreCase);
        var isA3Kind = isRpgMaker && string.Equals(_rpgKind, RpgMakerTilesetKinds.A3, StringComparison.OrdinalIgnoreCase);
        var isA4Kind = isRpgMaker && string.Equals(_rpgKind, RpgMakerTilesetKinds.A4, StringComparison.OrdinalIgnoreCase);

        _rpgKindCombo.Enabled = isRpgMaker;
        SetPlanRowVisible(1, showRpgControls);
        SetPlanRowVisible(2, showRpgControls && isA1Kind);
        SetPlanRowVisible(3, showRpgControls);
        _a1VariantCombo.Enabled = isA1Kind;
        _autoRpgButton.Enabled = isRpgMaker
            && (isA1Kind
                || string.Equals(_rpgKind, RpgMakerTilesetKinds.A2, StringComparison.OrdinalIgnoreCase)
                || isA3Kind
                || isA4Kind
                || string.Equals(_rpgKind, RpgMakerTilesetKinds.A5, StringComparison.OrdinalIgnoreCase));
        _clearA2Button.Enabled = _regions.Any(IsRpgAutoRegion);

        _hintLabel.Text = isRpgMaker && isA1Kind
            ? "RM 标准 A1: A 海洋、B 深海、C 海洋装饰、D 水面、E 瀑布，按官方 16x12 格布局生成。"
            : isRpgMaker && string.Equals(_rpgKind, RpgMakerTilesetKinds.A2, StringComparison.OrdinalIgnoreCase)
            ? "RM 标准 A2: 每 4x3 为一组，自动生成左右两个 2x3 自动元件块；左侧面板会折叠显示可绘制代表瓦片。"
            : isA3Kind
            ? "RM 标准 A3: 建筑外墙按官方 16x8 格布局生成，每个自动元件为 2x2。"
            : isA4Kind
            ? "RM 标准 A4: 墙壁/屋顶按官方 16x15 格布局生成，屋顶为 2x3，墙面为 2x2。"
            : isRpgMaker && string.Equals(_rpgKind, RpgMakerTilesetKinds.A5, StringComparison.OrdinalIgnoreCase)
            ? "RM 标准 A5: 普通瓦片按 8x16 单格区域生成。"
            : isRpgMaker
                ? "RM 自定义布局: 可手动框选普通区域、A1/A2/A3/A4 自动元件区域或忽略区域。"
                : "普通模式: 直接按单格图块绘制；需要自动边缘时可切到 RPG Maker 或高级模式。";
    }

    private void SetPlanRowVisible(int row, bool visible)
    {
        if (_planPanel is null)
        {
            return;
        }

        if (row < 0 || row >= _planPanel.RowStyles.Count)
        {
            return;
        }

        if (_planPanel.GetControlFromPosition(0, row) is Control label)
        {
            label.Visible = visible;
        }

        if (_planPanel.GetControlFromPosition(1, row) is Control editor)
        {
            editor.Visible = visible;
        }

        _planPanel.RowStyles[row].SizeType = SizeType.Absolute;
        _planPanel.RowStyles[row].Height = visible ? (row == 3 ? 36 : 32) : 0;
    }

    private void SetSideRowHeight(int row, int height)
    {
        if (_sidePanel is null || row < 0 || row >= _sidePanel.RowStyles.Count)
        {
            return;
        }

        _sidePanel.RowStyles[row].SizeType = SizeType.Absolute;
        _sidePanel.RowStyles[row].Height = height;
    }

    private void GenerateStandardRpgRegions()
    {
        if (!string.Equals(_mode, TilesetPlanModes.RpgMaker, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "目前支持 RPG Maker 标准 A1/A2/A3/A4/A5 的自动生成。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var columns = _image.Width / _tileSize;
        var rows = _image.Height / _tileSize;
        if (string.Equals(_rpgKind, RpgMakerTilesetKinds.A1, StringComparison.OrdinalIgnoreCase))
        {
            GenerateStandardA1Regions(columns, rows);
            return;
        }

        if (string.Equals(_rpgKind, RpgMakerTilesetKinds.A3, StringComparison.OrdinalIgnoreCase))
        {
            GenerateStandardA3Regions(columns, rows);
            return;
        }

        if (string.Equals(_rpgKind, RpgMakerTilesetKinds.A4, StringComparison.OrdinalIgnoreCase))
        {
            GenerateStandardA4Regions(columns, rows);
            return;
        }

        if (string.Equals(_rpgKind, RpgMakerTilesetKinds.A5, StringComparison.OrdinalIgnoreCase))
        {
            GenerateStandardA5Regions(columns, rows);
            return;
        }

        if (!string.Equals(_rpgKind, RpgMakerTilesetKinds.A2, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(this, "目前只支持 RPG Maker 标准 A1/A2/A3/A4/A5 的自动生成。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (columns < 4 || rows < 3)
        {
            MessageBox.Show(this, "标准 A2 至少需要 4x3 个瓦片格。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _regions.RemoveAll(v => string.Equals(v.Kind, TilesetRegionKinds.RpgMakerA2, StringComparison.OrdinalIgnoreCase));
        for (var y = 0; y + 3 <= rows; y += 3)
        {
            for (var x = 0; x + 4 <= columns; x += 4)
            {
                AddGeneratedA2Region(x, y);
                AddGeneratedA2Region(x + 2, y);
            }
        }

        RefreshRegionList();
        _canvas.Invalidate();
        UpdatePlanControls();
    }

    private void GenerateStandardA3Regions(int columns, int rows)
    {
        if (columns < 16 || rows < 8)
        {
            MessageBox.Show(this, "标准 A3 至少需要 16x8 个瓦片格。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _regions.RemoveAll(v => string.Equals(v.Kind, TilesetRegionKinds.RpgMakerA3, StringComparison.OrdinalIgnoreCase));
        for (var y = 0; y + 2 <= Math.Min(rows, 8); y += 2)
        {
            for (var x = 0; x + 2 <= Math.Min(columns, 16); x += 2)
            {
                AddGeneratedA3Region(x, y);
            }
        }

        RefreshRegionList();
        _canvas.Invalidate();
        UpdatePlanControls();
    }

    private void GenerateStandardA4Regions(int columns, int rows)
    {
        if (columns < 16 || rows < 15)
        {
            MessageBox.Show(this, "标准 A4 至少需要 16x15 个瓦片格。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _regions.RemoveAll(v => string.Equals(v.Kind, TilesetRegionKinds.RpgMakerA4, StringComparison.OrdinalIgnoreCase));
        int[] originRows = [0, 3, 5, 8, 10, 13];
        foreach (var y in originRows)
        {
            var isWall = IsOfficialA4WallRow(y);
            var height = isWall ? 2 : 3;
            if (y + height > Math.Min(rows, 15))
            {
                continue;
            }

            for (var x = 0; x + 2 <= Math.Min(columns, 16); x += 2)
            {
                AddGeneratedA4Region(x, y, height, isWall ? RpgMakerA4RegionVariants.Wall : RpgMakerA4RegionVariants.Roof);
            }
        }

        RefreshRegionList();
        _canvas.Invalidate();
        UpdatePlanControls();
    }

    private void GenerateStandardA5Regions(int columns, int rows)
    {
        if (columns < 8 || rows < 16)
        {
            MessageBox.Show(this, "标准 A5 至少需要 8x16 个瓦片格。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _regions.RemoveAll(v => string.Equals(v.Kind, TilesetRegionKinds.Normal, StringComparison.OrdinalIgnoreCase));
        _regions.Add(new TilesetRegionDefinition
        {
            Id = $"tileset.region.a5.0.0.{Guid.NewGuid():N}",
            Name = "RM A5 普通瓦片",
            Kind = TilesetRegionKinds.Normal,
            X = 0,
            Y = 0,
            Width = Math.Min(columns, 8),
            Height = Math.Min(rows, 16)
        });

        RefreshRegionList();
        _canvas.Invalidate();
        UpdatePlanControls();
    }

    private void GenerateStandardA1Regions(int columns, int rows)
    {
        if (columns < 16 || rows < 12)
        {
            MessageBox.Show(this, "标准 A1 至少需要 16x12 个瓦片格。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _regions.RemoveAll(v => string.Equals(v.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase));
        AddGeneratedA1Region(0, 0, 6, 3, "RM A1 A 海洋", RpgMakerA1RegionVariants.Ocean);
        AddGeneratedA1Region(0, 3, 6, 3, "RM A1 B 深海", RpgMakerA1RegionVariants.DeepSea);
        AddGeneratedA1Region(6, 0, 2, 3, "RM A1 C 海洋装饰 1", RpgMakerA1RegionVariants.OceanDecor);
        AddGeneratedA1Region(6, 3, 2, 3, "RM A1 C 海洋装饰 2", RpgMakerA1RegionVariants.OceanDecor);
        AddGeneratedA1Region(8, 0, 6, 3, "RM A1 D 水面 1", RpgMakerA1RegionVariants.Water);
        AddGeneratedA1Region(8, 3, 6, 3, "RM A1 D 水面 2", RpgMakerA1RegionVariants.Water);
        AddGeneratedA1Region(0, 6, 6, 3, "RM A1 D 水面 3", RpgMakerA1RegionVariants.Water);
        AddGeneratedA1Region(0, 9, 6, 3, "RM A1 D 水面 4", RpgMakerA1RegionVariants.Water);
        AddGeneratedA1Region(8, 6, 6, 3, "RM A1 D 水面 5", RpgMakerA1RegionVariants.Water);
        AddGeneratedA1Region(8, 9, 6, 3, "RM A1 D 水面 6", RpgMakerA1RegionVariants.Water);
        AddGeneratedA1Region(14, 0, 2, 3, "RM A1 E 瀑布 1", RpgMakerA1RegionVariants.Waterfall);
        AddGeneratedA1Region(14, 3, 2, 3, "RM A1 E 瀑布 2", RpgMakerA1RegionVariants.Waterfall);
        AddGeneratedA1Region(6, 6, 2, 3, "RM A1 E 瀑布 3", RpgMakerA1RegionVariants.Waterfall);
        AddGeneratedA1Region(6, 9, 2, 3, "RM A1 E 瀑布 4", RpgMakerA1RegionVariants.Waterfall);
        AddGeneratedA1Region(14, 6, 2, 3, "RM A1 E 瀑布 5", RpgMakerA1RegionVariants.Waterfall);
        AddGeneratedA1Region(14, 9, 2, 3, "RM A1 E 瀑布 6", RpgMakerA1RegionVariants.Waterfall);

        RefreshRegionList();
        _canvas.Invalidate();
        UpdatePlanControls();
    }

    private void ClearA2Regions()
    {
        var removed = _regions.RemoveAll(IsRpgAutoRegion);
        if (removed <= 0)
        {
            return;
        }

        RefreshRegionList();
        _canvas.SelectedRegion = null;
        _canvas.Invalidate();
        UpdatePlanControls();
    }

    private void AddGeneratedA2Region(int x, int y)
    {
        _regions.Add(new TilesetRegionDefinition
        {
            Id = $"tileset.region.a2.{x}.{y}.{Guid.NewGuid():N}",
            Name = $"RM A2 {x},{y}",
            Kind = TilesetRegionKinds.RpgMakerA2,
            X = x,
            Y = y,
            Width = 2,
            Height = 3
        });
    }

    private void AddGeneratedA3Region(int x, int y)
    {
        _regions.Add(new TilesetRegionDefinition
        {
            Id = $"tileset.region.a3.{x}.{y}.{Guid.NewGuid():N}",
            Name = $"RM A3 {x},{y}",
            Kind = TilesetRegionKinds.RpgMakerA3,
            X = x,
            Y = y,
            Width = 2,
            Height = 2
        });
    }

    private void AddGeneratedA4Region(int x, int y, int height, string variant)
    {
        _regions.Add(new TilesetRegionDefinition
        {
            Id = $"tileset.region.a4.{x}.{y}.{Guid.NewGuid():N}",
            Name = variant == RpgMakerA4RegionVariants.Wall ? $"RM A4 墙面 {x},{y}" : $"RM A4 屋顶 {x},{y}",
            Kind = TilesetRegionKinds.RpgMakerA4,
            Variant = variant,
            X = x,
            Y = y,
            Width = 2,
            Height = height
        });
    }

    private void AddGeneratedA1Region(int x, int y)
    {
        _regions.Add(new TilesetRegionDefinition
        {
            Id = $"tileset.region.a1.{x}.{y}.{Guid.NewGuid():N}",
            Name = $"RM A1 {x},{y}",
            Kind = TilesetRegionKinds.RpgMakerA1,
            Variant = RpgMakerA1RegionVariants.Water,
            X = x,
            Y = y,
            Width = 6,
            Height = 3
        });
    }

    private void AddGeneratedA1Region(int x, int y, int width, int height, string name, string variant)
    {
        _regions.Add(new TilesetRegionDefinition
        {
            Id = $"tileset.region.a1.{x}.{y}.{Guid.NewGuid():N}",
            Name = name,
            Kind = TilesetRegionKinds.RpgMakerA1,
            Variant = variant,
            X = x,
            Y = y,
            Width = width,
            Height = height
        });
    }

    private static void ConfigureModeButton(RadioButton button, string text, bool isChecked = false)
    {
        button.Text = text;
        button.AutoSize = true;
        button.Checked = isChecked;
        button.Margin = new Padding(0, 4, 0, 4);
    }

    private void AddSelection()
    {
        var selection = _canvas.Selection;
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            MessageBox.Show(this, "请先在图集中拖拽框选一个区域。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var kind = SelectedKind();
        if (kind == TilesetRegionKinds.RpgMakerA1 && !IsValidA1Selection(selection, SelectedA1Variant()))
        {
            MessageBox.Show(this, "RPG Maker A1 区域必须匹配所选官方块：A/B/D 为 6x3，C/E 为 2x3。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (kind == TilesetRegionKinds.RpgMakerA2 && (selection.Width % 2 != 0 || selection.Height % 3 != 0))
        {
            MessageBox.Show(this, "RPG Maker A2 区域必须由完整的 2x3 自动元件块组成。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (kind == TilesetRegionKinds.RpgMakerA3 && (selection.Width % 2 != 0 || selection.Height % 2 != 0))
        {
            MessageBox.Show(this, "RPG Maker A3 区域必须由完整的 2x2 建筑外墙自动元件块组成。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (kind == TilesetRegionKinds.RpgMakerA4 && !IsValidA4Selection(selection))
        {
            MessageBox.Show(this, "RPG Maker A4 选区必须按官方块对齐：屋顶 2x3，墙面 2x2。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (kind == TilesetRegionKinds.RpgMakerA4)
        {
            AddA4SelectionRegions(selection);
            RefreshRegionList();
            _canvas.Invalidate();
            UpdatePlanControls();
            return;
        }

        var region = new TilesetRegionDefinition
        {
            Id = $"tileset.region.{Guid.NewGuid():N}",
            Name = CreateRegionName(kind, selection),
            Kind = kind,
            Variant = CreateRegionVariant(kind, selection, SelectedA1Variant()),
            X = selection.X,
            Y = selection.Y,
            Width = selection.Width,
            Height = selection.Height
        };
        _regions.Add(region);
        RefreshRegionList(region);
        _canvas.Invalidate();
        UpdatePlanControls();
    }

    private void DeleteSelectedRegion()
    {
        if (_regionList.SelectedItems.Count <= 0 || _regionList.SelectedItems[0].Tag is not TilesetRegionDefinition region)
        {
            return;
        }

        _regions.Remove(region);
        RefreshRegionList();
        _canvas.SelectedRegion = null;
        _canvas.Invalidate();
        UpdatePlanControls();
    }

    private void RefreshRegionList(TilesetRegionDefinition? selected = null)
    {
        _regionList.Items.Clear();
        foreach (var region in _regions)
        {
            var item = new ListViewItem([region.Name, KindLabel(region.Kind), $"{region.X},{region.Y} {region.Width}x{region.Height}"])
            {
                Tag = region
            };
            _regionList.Items.Add(item);
            item.Selected = ReferenceEquals(region, selected);
        }

        _deleteButton.Enabled = _regionList.SelectedItems.Count > 0;
    }

    private void UpdateSelectionText(Rectangle selection)
    {
        _selectionLabel.Text = selection.Width <= 0 || selection.Height <= 0
            ? "选区: -"
            : $"选区: {selection.X},{selection.Y}  {selection.Width}x{selection.Height}";
    }

    private string SelectedKind()
    {
        if (_ignoredButton.Checked)
        {
            return TilesetRegionKinds.Ignored;
        }

        if (string.Equals(_mode, TilesetPlanModes.RpgMaker, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(_rpgKind, RpgMakerTilesetKinds.A1, StringComparison.OrdinalIgnoreCase))
            {
                return TilesetRegionKinds.RpgMakerA1;
            }

            if (string.Equals(_rpgKind, RpgMakerTilesetKinds.A2, StringComparison.OrdinalIgnoreCase))
            {
                return TilesetRegionKinds.RpgMakerA2;
            }

            if (string.Equals(_rpgKind, RpgMakerTilesetKinds.A3, StringComparison.OrdinalIgnoreCase))
            {
                return TilesetRegionKinds.RpgMakerA3;
            }

            if (string.Equals(_rpgKind, RpgMakerTilesetKinds.A4, StringComparison.OrdinalIgnoreCase))
            {
                return TilesetRegionKinds.RpgMakerA4;
            }
        }

        return TilesetRegionKinds.Normal;
    }

    private static string CreateRegionName(string kind, Rectangle selection)
    {
        var label = KindLabel(kind);
        return $"{label} {selection.X},{selection.Y}";
    }

    private string SelectedA1Variant()
    {
        return SelectedComboValue(_a1VariantCombo, RpgMakerA1RegionVariants.Ocean);
    }

    private static bool IsValidA1Selection(Rectangle selection, string variant)
    {
        return variant switch
        {
            RpgMakerA1RegionVariants.Ocean => selection.Width % 6 == 0 && selection.Height % 3 == 0,
            RpgMakerA1RegionVariants.DeepSea => selection.Width % 6 == 0 && selection.Height % 3 == 0,
            RpgMakerA1RegionVariants.OceanDecor => selection.Width % 2 == 0 && selection.Height % 3 == 0,
            RpgMakerA1RegionVariants.Water => selection.Width % 6 == 0 && selection.Height % 3 == 0,
            RpgMakerA1RegionVariants.Waterfall => selection.Width % 2 == 0 && selection.Height % 3 == 0,
            _ => false
        };
    }

    private static string CreateRegionVariant(string kind, Rectangle selection, string selectedVariant)
    {
        if (string.Equals(kind, TilesetRegionKinds.RpgMakerA4, StringComparison.OrdinalIgnoreCase))
        {
            return IsOfficialA4WallRow(selection.Y) ? RpgMakerA4RegionVariants.Wall : RpgMakerA4RegionVariants.Roof;
        }

        if (!string.Equals(kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return selectedVariant;
    }

    private static string KindLabel(string kind)
    {
        return kind switch
        {
            TilesetRegionKinds.RpgMakerA1 => "RPG A1",
            TilesetRegionKinds.RpgMakerA2 => "RPG A2",
            TilesetRegionKinds.RpgMakerA3 => "RPG A3",
            TilesetRegionKinds.RpgMakerA4 => "RPG A4",
            TilesetRegionKinds.Ignored => "忽略",
            _ => "普通"
        };
    }

    private static bool IsOfficialA4WallRow(int y)
    {
        return y is 3 or 8 or 13;
    }

    private static bool IsValidA4Selection(Rectangle selection)
    {
        if (selection.Width % 2 != 0)
        {
            return false;
        }

        var rows = EnumerateA4Rows(selection).ToList();
        if (rows.Count <= 0)
        {
            return false;
        }

        return rows[0].Y == selection.Y && rows[^1].Y + rows[^1].Height == selection.Bottom;
    }

    private void AddA4SelectionRegions(Rectangle selection)
    {
        foreach (var row in EnumerateA4Rows(selection))
        {
            for (var x = selection.X; x < selection.Right; x += 2)
            {
                AddGeneratedA4Region(x, row.Y, row.Height, row.Variant);
            }
        }
    }

    private static IEnumerable<A4Row> EnumerateA4Rows(Rectangle selection)
    {
        int[] originRows = [0, 3, 5, 8, 10, 13];
        foreach (var y in originRows)
        {
            var isWall = IsOfficialA4WallRow(y);
            var height = isWall ? 2 : 3;
            if (y >= selection.Y && y + height <= selection.Bottom)
            {
                yield return new A4Row(y, height, isWall ? RpgMakerA4RegionVariants.Wall : RpgMakerA4RegionVariants.Roof);
            }
        }
    }

    private static bool IsRpgAutoRegion(TilesetRegionDefinition region)
    {
        return string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase)
            || string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA2, StringComparison.OrdinalIgnoreCase)
            || string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA3, StringComparison.OrdinalIgnoreCase)
            || string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA4, StringComparison.OrdinalIgnoreCase);
    }

    private static List<TilesetRegionDefinition> CloneRegions(IEnumerable<TilesetRegionDefinition> regions)
    {
        return regions
            .Select(v => new TilesetRegionDefinition
            {
                Id = v.Id,
                Name = v.Name,
                Kind = v.Kind,
                Variant = v.Variant,
                X = v.X,
                Y = v.Y,
                Width = v.Width,
                Height = v.Height
            })
            .ToList();
    }
}

internal sealed record A4Row(int Y, int Height, string Variant);

internal sealed class TilesetPlannerCanvas : Panel
{
    private Image? _image;
    private int _tileSize = 32;
    private List<TilesetRegionDefinition> _regions = [];
    private Rectangle _selection = Rectangle.Empty;
    private TilesetRegionDefinition? _selectedRegion;
    private bool _dragging;
    private Point _dragStart;

    public event EventHandler<TilesetPlanSelectionChangedEventArgs>? SelectionChanged;

    public Rectangle Selection => _selection;

    public TilesetRegionDefinition? SelectedRegion
    {
        get => _selectedRegion;
        set
        {
            _selectedRegion = value;
            Invalidate();
        }
    }

    public TilesetPlannerCanvas()
    {
        DoubleBuffered = true;
        AutoScroll = true;
        ResizeRedraw = true;
        BackColor = Color.FromArgb(31, 31, 31);
    }

    public void SetTileset(Image image, int tileSize, List<TilesetRegionDefinition> regions)
    {
        _image = image;
        _tileSize = Math.Max(8, tileSize);
        _regions = regions;
        AutoScrollMinSize = new Size(_image.Width + 1, _image.Height + 1);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (_image is null || e.Button != MouseButtons.Left)
        {
            return;
        }

        var tile = HitTile(e.Location);
        if (tile.X < 0 || tile.Y < 0)
        {
            return;
        }

        _dragging = true;
        _dragStart = tile;
        _selection = new Rectangle(tile.X, tile.Y, 1, 1);
        SelectionChanged?.Invoke(this, new TilesetPlanSelectionChangedEventArgs(_selection));
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging || _image is null)
        {
            return;
        }

        var tile = HitTile(e.Location);
        if (tile.X < 0 || tile.Y < 0)
        {
            return;
        }

        var left = Math.Min(_dragStart.X, tile.X);
        var top = Math.Min(_dragStart.Y, tile.Y);
        var right = Math.Max(_dragStart.X, tile.X);
        var bottom = Math.Max(_dragStart.Y, tile.Y);
        _selection = Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        SelectionChanged?.Invoke(this, new TilesetPlanSelectionChangedEventArgs(_selection));
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _dragging = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.Clear(BackColor);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        if (_image is null)
        {
            return;
        }

        var offset = AutoScrollPosition;
        g.DrawImage(_image, offset.X, offset.Y, _image.Width, _image.Height);
        DrawRegions(g, offset);
        DrawGrid(g, offset);
        DrawSelection(g, offset);
    }

    private Point HitTile(Point point)
    {
        if (_image is null)
        {
            return new Point(-1, -1);
        }

        var x = point.X - AutoScrollPosition.X;
        var y = point.Y - AutoScrollPosition.Y;
        if (x < 0 || y < 0 || x >= _image.Width || y >= _image.Height)
        {
            return new Point(-1, -1);
        }

        return new Point(x / _tileSize, y / _tileSize);
    }

    private void DrawGrid(Graphics g, Point offset)
    {
        if (_image is null)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(95, 255, 255, 255));
        for (var x = 0; x <= _image.Width; x += _tileSize)
        {
            g.DrawLine(pen, offset.X + x, offset.Y, offset.X + x, offset.Y + _image.Height);
        }

        for (var y = 0; y <= _image.Height; y += _tileSize)
        {
            g.DrawLine(pen, offset.X, offset.Y + y, offset.X + _image.Width, offset.Y + y);
        }
    }

    private void DrawRegions(Graphics g, Point offset)
    {
        foreach (var region in _regions)
        {
            var rect = RegionToPixels(region, offset);
            var color = RegionColor(region.Kind);
            var selected = ReferenceEquals(region, _selectedRegion);
            var borderColor = selected ? Color.FromArgb(255, 0, 190, 220) : color;
            using var fill = new SolidBrush(Color.FromArgb(selected ? 72 : 44, selected ? borderColor : color));
            using var pen = new Pen(borderColor, selected ? 4f : 2f);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect);
            DrawRegionLabel(g, rect, region);
        }
    }

    private void DrawSelection(Graphics g, Point offset)
    {
        if (_selection.Width <= 0 || _selection.Height <= 0)
        {
            return;
        }

        var rect = new Rectangle(
            offset.X + _selection.X * _tileSize,
            offset.Y + _selection.Y * _tileSize,
            _selection.Width * _tileSize,
            _selection.Height * _tileSize);
        using var fill = new SolidBrush(Color.FromArgb(36, 0, 120, 215));
        using var pen = new Pen(Color.FromArgb(255, 0, 120, 215), 3f);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(pen, rect);
    }

    private Rectangle RegionToPixels(TilesetRegionDefinition region, Point offset)
    {
        return new Rectangle(
            offset.X + region.X * _tileSize,
            offset.Y + region.Y * _tileSize,
            region.Width * _tileSize,
            region.Height * _tileSize);
    }

    private static Color RegionColor(string kind)
    {
        return kind switch
        {
            TilesetRegionKinds.RpgMakerA1 => Color.FromArgb(255, 78, 172, 255),
            TilesetRegionKinds.RpgMakerA2 => Color.FromArgb(255, 229, 126, 32),
            TilesetRegionKinds.RpgMakerA3 => Color.FromArgb(255, 186, 104, 200),
            TilesetRegionKinds.RpgMakerA4 => Color.FromArgb(255, 121, 134, 203),
            TilesetRegionKinds.Ignored => Color.FromArgb(255, 145, 145, 145),
            _ => Color.FromArgb(255, 38, 166, 91)
        };
    }

    private static void DrawRegionLabel(Graphics g, Rectangle rect, TilesetRegionDefinition region)
    {
        if (rect.Width < 34 || rect.Height < 20)
        {
            return;
        }

        using var font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold);
        using var backBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
        using var textBrush = new SolidBrush(Color.White);
        var text = region.Kind == TilesetRegionKinds.RpgMakerA1
            ? A1RegionLabel(region)
            : region.Kind == TilesetRegionKinds.RpgMakerA2
                ? "A2"
                : region.Kind == TilesetRegionKinds.RpgMakerA3
                    ? "A3"
                    : region.Kind == TilesetRegionKinds.RpgMakerA4
                        ? A4RegionLabel(region)
                        : region.Kind == TilesetRegionKinds.Ignored ? "-" : "普通";
        var size = g.MeasureString(text, font);
        var labelRect = new RectangleF(rect.Left + 4, rect.Top + 4, size.Width + 8, size.Height + 4);
        g.FillRectangle(backBrush, labelRect);
        g.DrawString(text, font, textBrush, labelRect.Left + 4, labelRect.Top + 2);
    }

    private static string A1RegionLabel(TilesetRegionDefinition region)
    {
        return region.Variant switch
        {
            RpgMakerA1RegionVariants.Ocean => "A海洋",
            RpgMakerA1RegionVariants.DeepSea => "B深海",
            RpgMakerA1RegionVariants.OceanDecor => "C装饰",
            RpgMakerA1RegionVariants.Waterfall => "E瀑布",
            _ => "D水面"
        };
    }

    private static string A4RegionLabel(TilesetRegionDefinition region)
    {
        return string.Equals(region.Variant, RpgMakerA4RegionVariants.Wall, StringComparison.OrdinalIgnoreCase)
            ? "A4墙"
            : "A4顶";
    }
}

internal sealed class TilesetPlanSelectionChangedEventArgs : EventArgs
{
    public TilesetPlanSelectionChangedEventArgs(Rectangle selection)
    {
        Selection = selection;
    }

    public Rectangle Selection { get; }
}

internal sealed class PlannerComboItem
{
    public PlannerComboItem(string value, string label)
    {
        Value = value;
        Label = label;
    }

    public string Value { get; }

    public string Label { get; }

    public override string ToString()
    {
        return Label;
    }
}
