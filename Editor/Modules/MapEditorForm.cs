using System.Globalization;
using System.Drawing.Imaging;
using Axe2DEditor.Core.Maps;
using Axe2DEditor.Core.Projects;
using Axe2DEditor.Editor.Controls;
using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Editor.Modules;

public sealed class MapEditorForm : Form
{
    private const int LeftWidth = 340;
    private const int RightWidth = 300;
    private const int LeftMapHeight = 180;
    private const int RightLayerHeight = 260;

    private enum TilesetImportMode
    {
        Cancel,
        Replace,
        Append
    }

    private readonly ProjectContext _context;
    private readonly ProjectService _projectService;
    private readonly LocalizationService _localization;
    private readonly EditorSettings _settings;
    private readonly EditorSettingsService _settingsService;

    private readonly ToolTip _toolTip = new();
    private readonly ToolStrip _toolStrip = new();
    private readonly ToolStripButton _paintButton = new();
    private readonly ToolStripButton _eraseButton = new();
    private readonly ToolStripButton _fillButton = new();
    private readonly ToolStripButton _rectButton = new();
    private readonly ToolStripButton _pickButton = new();
    private readonly ToolStripButton _gridButton = new();
    private readonly ToolStripButton _autoEdgeButton = new();
    private readonly ToolStripButton _resetViewButton = new();
    private readonly ToolStripButton _saveButton = new();
    private readonly ToolStripButton _importTilesetButton = new();
    private readonly ToolStripButton _planTilesetButton = new();

    private readonly ListBox _mapList = new();
    private readonly ListView _layerList = new();
    private readonly FlowLayoutPanel _terrainPalette = new();
    private readonly TilesetPalettePanel _tilesetPalette = new();
    private readonly MapCanvasPanel _canvas = new();
    private readonly Label _statusLabel = new();
    private readonly Label _tilesetInfoLabel = new();

    private readonly TextBox _idBox = new();
    private readonly TextBox _nameBox = new();
    private readonly TextBox _descriptionBox = new();
    private readonly TextBox _tilesetBox = new();
    private readonly TextBox _backgroundBox = new();
    private readonly NumericUpDown _widthBox = new();
    private readonly NumericUpDown _heightBox = new();
    private readonly NumericUpDown _tileSizeBox = new();
    private readonly ComboBox _viewTypeCombo = new();
    private readonly Button _applyMapButton = new();
    private readonly Button _newMapButton = new();
    private readonly Button _deleteMapButton = new();
    private readonly Button _addLayerButton = new();
    private readonly Button _deleteLayerButton = new();
    private readonly Button _moveLayerUpButton = new();
    private readonly Button _moveLayerDownButton = new();
    private readonly Button _useTerrainBrushButton = new();
    private readonly Button _useTilesetBrushButton = new();
    private readonly Button _bindA2PaletteButton = new();
    private readonly CheckBox _layerVisibleBox = new();
    private readonly CheckBox _layerLockedBox = new();
    private readonly NumericUpDown _layerOpacityBox = new();

    private MapDefinition? _selectedMap;
    private MapLayerDefinition? _selectedLayer;
    private MapTerrainDefinition? _selectedTerrain;
    private MapTerrainDefinition? _shiftTerrain;
    private Point _selectedTilesetTile = new(-1, -1);
    private bool _shiftAutoTerrainMode;
    private bool _suppressUiEvents;

    public MapEditorForm(
        ProjectContext context,
        ProjectService projectService,
        LocalizationService localization,
        EditorSettings settings,
        EditorSettingsService settingsService)
    {
        _context = context;
        _projectService = projectService;
        _localization = localization;
        _settings = settings;
        _settingsService = settingsService;

        NormalizeMaps();
        BuildUi();
        RefreshMapList();
        SelectFirstMap();
    }

    private void BuildUi()
    {
        Text = T("module.mapEditor", "地图编辑器");
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 760);
        Size = new Size(1500, 920);
        Font = new Font("Microsoft YaHei UI", 9F);
        KeyPreview = true;
        TabSelectAllBehavior.InstallRecursive(this);

        BuildToolStrip();
        BuildStatusBar();
        Controls.Add(_toolStrip);

        var rootSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        var leftSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        var workSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        var inspectorSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        rootSplit.Panel1.Controls.Add(leftSplit);
        rootSplit.Panel2.Controls.Add(workSplit);
        leftSplit.Panel1.Controls.Add(BuildTerrainPanel());
        leftSplit.Panel2.Controls.Add(BuildMapPanel());
        workSplit.Panel1.Controls.Add(BuildCanvasPanel());
        workSplit.Panel2.Controls.Add(inspectorSplit);
        inspectorSplit.Panel1.Controls.Add(BuildInspectorPanel());
        inspectorSplit.Panel2.Controls.Add(BuildLayerPanel());

        Controls.Add(rootSplit);
        Controls.Add(_statusLabel);
        Controls.SetChildIndex(_toolStrip, 0);

        Shown += (_, _) =>
        {
            BeginInvoke(new Action(() =>
            {
                SplitContainerLayout.ApplySafe(rootSplit, LeftWidth, 220, 720);
                SplitContainerLayout.ApplySafe(leftSplit, Math.Max(320, Height - LeftMapHeight - 110), 360, LeftMapHeight);
                SplitContainerLayout.ApplySafe(workSplit, Math.Max(520, Width - LeftWidth - RightWidth - 40), 420, RightWidth);
                SplitContainerLayout.ApplySafe(inspectorSplit, Math.Max(300, Height - RightLayerHeight - 110), 260, RightLayerHeight);
            }));
        };
        KeyDown += MapEditorForm_KeyDown;
        KeyUp += MapEditorForm_KeyUp;
    }

    private void BuildStatusBar()
    {
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 26;
        _statusLabel.Padding = new Padding(12, 0, 12, 0);
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.BorderStyle = BorderStyle.Fixed3D;
    }

    private void MapEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.ShiftKey)
        {
            SetShiftAutoTerrainMode(true);
        }
    }

    private void MapEditorForm_KeyUp(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.ShiftKey)
        {
            SetShiftAutoTerrainMode(false);
        }
    }

    private void SetShiftAutoTerrainMode(bool enabled)
    {
        if (_shiftAutoTerrainMode == enabled)
        {
            return;
        }

        _shiftAutoTerrainMode = enabled;
        if (enabled && _selectedTilesetTile.X >= 0 && IsA1OverlayTile(_selectedTilesetTile.X, _selectedTilesetTile.Y))
        {
            _shiftTerrain = FindPlannedAutoTerrain(_selectedTilesetTile.X, _selectedTilesetTile.Y);
            _canvas.SetShiftTerrain(_shiftTerrain);
        }

        _tilesetPalette.ShowShiftAutoTerrainMode = enabled;
        _canvas.SetShiftAutoTerrainMode(enabled);
        UpdateStatusText();
    }

    private void BuildToolStrip()
    {
        _toolStrip.Dock = DockStyle.Top;
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.Padding = new Padding(8, 5, 8, 5);
        _toolStrip.ImageScalingSize = new Size(20, 20);

        ConfigureToolButton(_paintButton, "🖌", "画笔", MapEditorTool.Paint);
        ConfigureToolButton(_eraseButton, "⌫", "橡皮", MapEditorTool.Erase);
        ConfigureToolButton(_fillButton, "▣", "填充", MapEditorTool.Fill);
        ConfigureToolButton(_rectButton, "▤", "矩形", MapEditorTool.Rectangle);
        ConfigureToolButton(_pickButton, "⌖", "吸管", MapEditorTool.Eyedropper);

        _gridButton.Text = "#";
        _gridButton.ToolTipText = "显示网格";
        _gridButton.CheckOnClick = true;
        _gridButton.Checked = true;
        _gridButton.Click += (_, _) => _canvas.ShowGrid = _gridButton.Checked;

        _autoEdgeButton.Text = "◫";
        _autoEdgeButton.ToolTipText = "自动边缘预览";
        _autoEdgeButton.CheckOnClick = true;
        _autoEdgeButton.Checked = true;
        _autoEdgeButton.Click += (_, _) => _canvas.ShowAutoEdges = _autoEdgeButton.Checked;

        _resetViewButton.Text = "◎";
        _resetViewButton.ToolTipText = "重置视图";
        _resetViewButton.Click += (_, _) => _canvas.ResetView();

        _importTilesetButton.Text = "🖼";
        _importTilesetButton.ToolTipText = "导入图集";
        _importTilesetButton.Click += (_, _) => ImportTileset();

        _planTilesetButton.Text = "▦";
        _planTilesetButton.ToolTipText = "规划图集区域";
        _planTilesetButton.Click += (_, _) => PlanTileset();

        _saveButton.Text = "💾";
        _saveButton.ToolTipText = T("inspector.save", "保存");
        _saveButton.Alignment = ToolStripItemAlignment.Right;
        _saveButton.Click += (_, _) => SaveProject();

        _toolStrip.Items.AddRange(
        [
            _importTilesetButton,
            _planTilesetButton,
            new ToolStripSeparator(),
            _paintButton,
            _eraseButton,
            _fillButton,
            _rectButton,
            _pickButton,
            new ToolStripSeparator(),
            _gridButton,
            _autoEdgeButton,
            _resetViewButton,
            _saveButton
        ]);
        SelectTool(MapEditorTool.Paint);
    }

    private Control BuildMapPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var mapHeader = new Panel { Dock = DockStyle.Fill };
        var mapLabel = HeaderLabel("地图");
        mapLabel.Dock = DockStyle.Fill;
        var mapTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0)
        };
        ConfigureSmallButton(_deleteMapButton, "删", DeleteMap);
        ConfigureSmallButton(_newMapButton, "+", CreateMap);
        _newMapButton.Width = 30;
        _deleteMapButton.Width = 38;
        _toolTip.SetToolTip(_newMapButton, "新建地图");
        _toolTip.SetToolTip(_deleteMapButton, "删除当前地图");
        mapTools.Controls.Add(_newMapButton);
        mapTools.Controls.Add(_deleteMapButton);
        mapHeader.Controls.Add(mapLabel);
        mapHeader.Controls.Add(mapTools);

        _mapList.Dock = DockStyle.Fill;
        _mapList.IntegralHeight = false;
        _mapList.DisplayMember = nameof(MapDefinition.DisplayName);
        _mapList.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            SelectMap(_mapList.SelectedItem as MapDefinition);
        };
        _mapList.DoubleClick += (_, _) => ShowMapSettingsDialog();
        panel.Controls.Add(mapHeader, 0, 0);
        panel.Controls.Add(_mapList, 0, 1);
        return panel;
    }

    private Control BuildLayerPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var layerHeader = new Panel { Dock = DockStyle.Fill };
        var layerLabel = HeaderLabel("图层");
        layerLabel.Dock = DockStyle.Fill;
        var layerTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0)
        };
        ConfigureSmallButton(_addLayerButton, "+", CreateLayer);
        ConfigureSmallButton(_deleteLayerButton, "-", DeleteLayer);
        ConfigureSmallButton(_moveLayerUpButton, "↑", () => MoveLayer(-1));
        ConfigureSmallButton(_moveLayerDownButton, "↓", () => MoveLayer(1));
        layerTools.Controls.AddRange([_addLayerButton, _deleteLayerButton, _moveLayerUpButton, _moveLayerDownButton]);
        layerHeader.Controls.Add(layerLabel);
        layerHeader.Controls.Add(layerTools);

        _layerList.Dock = DockStyle.Fill;
        _layerList.View = View.Details;
        _layerList.FullRowSelect = true;
        _layerList.HideSelection = false;
        _layerList.MultiSelect = false;
        _layerList.Columns.Add("名称", 132);
        _layerList.Columns.Add("类型", 76);
        _layerList.Columns.Add("状态", 64);
        _layerList.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressUiEvents)
            {
                return;
            }

            var layer = _layerList.SelectedItems.Count > 0 ? _layerList.SelectedItems[0].Tag as MapLayerDefinition : null;
            SelectLayer(layer);
        };

        panel.Controls.Add(layerHeader, 0, 0);
        panel.Controls.Add(_layerList, 0, 1);
        return panel;
    }

    private Control BuildCanvasPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(0)
        };
        _canvas.Dock = DockStyle.Fill;
        _canvas.MapChanged += (_, _) =>
        {
            UpdateLayerListText();
            UpdateStatusText();
        };
        _canvas.TileHovered += (_, e) =>
        {
            _statusLabel.Text = e.Inside
                ? $"x {e.X}, y {e.Y} | {_selectedLayer?.Name ?? "-"} | {_selectedTerrain?.DisplayName ?? _selectedTerrain?.Id ?? "-"}{ShiftStatusSuffix()}"
                : "地图画布";
        };
        _canvas.TerrainPicked += (_, e) => SelectTerrain(e.TerrainId);
        _canvas.TilesetTilePicked += (_, e) => SelectTilesetTile(e.TileX, e.TileY, e.Width, e.Height, useAsBrush: true);
        panel.Controls.Add(_canvas);
        return panel;
    }

    private Control BuildTerrainPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 8, 12, 8),
            ColumnCount = 1,
            RowCount = 2
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var tilesetHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0)
        };
        tilesetHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        tilesetHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        tilesetHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
        tilesetHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));

        _tilesetInfoLabel.Dock = DockStyle.Fill;
        _tilesetInfoLabel.TextAlign = ContentAlignment.MiddleLeft;
        _tilesetInfoLabel.Text = "图集: 未导入";
        ConfigurePaletteButton(_useTerrainBrushButton, "地形", UseTerrainBrush);
        ConfigurePaletteButton(_useTilesetBrushButton, "图块", UseTilesetBrush);
        ConfigurePaletteButton(_bindA2PaletteButton, "A2", BindSelectedTerrainToA2);
        _useTerrainBrushButton.Tag = "使用当前地形或自动元件绘制";
        _useTilesetBrushButton.Tag = "直接使用当前选中的图集单格绘制";
        _bindA2PaletteButton.Tag = "把当前地形绑定到选中的 RPG Maker A2 自动元件块";
        _toolTip.SetToolTip(_useTerrainBrushButton, "使用当前地形或自动元件绘制");
        _toolTip.SetToolTip(_useTilesetBrushButton, "直接使用当前选中的图集单格绘制");
        _toolTip.SetToolTip(_bindA2PaletteButton, "把当前地形绑定到选中的 RPG Maker A2 自动元件块");
        tilesetHeader.Controls.Add(_tilesetInfoLabel, 0, 0);
        tilesetHeader.Controls.Add(_useTerrainBrushButton, 1, 0);
        tilesetHeader.Controls.Add(_useTilesetBrushButton, 2, 0);
        tilesetHeader.Controls.Add(_bindA2PaletteButton, 3, 0);
        panel.Controls.Add(tilesetHeader, 0, 0);

        _tilesetPalette.Dock = DockStyle.Fill;
        _tilesetPalette.TileSelected += (_, e) => SelectTilesetTile(e.TileX, e.TileY, e.Width, e.Height, useAsBrush: true);
        panel.Controls.Add(_tilesetPalette, 0, 1);
        return panel;
    }

    private Control BuildInspectorPanel()
    {
        return new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = SystemColors.Control
        };
    }

    private TabPage BuildMapPage()
    {
        var page = new TabPage("地图");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12),
            ColumnCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 10; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 2 ? 70 : 34));
        }

        ConfigureTextBox(_idBox);
        ConfigureTextBox(_nameBox);
        ConfigureTextBox(_descriptionBox, multiline: true);
        ConfigureTextBox(_tilesetBox);
        ConfigureTextBox(_backgroundBox);
        ConfigureNumber(_widthBox, 8, 512, 64);
        ConfigureNumber(_heightBox, 8, 512, 64);
        ConfigureNumber(_tileSizeBox, 8, 128, 32);
        _viewTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _viewTypeCombo.Items.AddRange(["TopDown", "Platformer", "Isometric"]);

        AddRow(panel, 0, "ID", _idBox);
        AddRow(panel, 1, "名称", _nameBox);
        AddRow(panel, 2, "描述", _descriptionBox);
        AddRow(panel, 3, "视角", _viewTypeCombo);
        AddRow(panel, 4, "宽度", _widthBox);
        AddRow(panel, 5, "高度", _heightBox);
        AddRow(panel, 6, "瓦片", _tileSizeBox);
        AddRow(panel, 7, "图块集", _tilesetBox);
        AddRow(panel, 8, "背景", _backgroundBox);

        _applyMapButton.Text = "应用";
        _applyMapButton.Dock = DockStyle.Right;
        _applyMapButton.Width = 96;
        _applyMapButton.Click += (_, _) => ApplyMapProperties();
        panel.Controls.Add(_applyMapButton, 1, 9);

        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildLayerPage()
    {
        var page = new TabPage("图层");
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12),
            ColumnCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 82));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 4; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        }

        _layerVisibleBox.Text = "可见";
        _layerVisibleBox.CheckedChanged += (_, _) => ApplyLayerProperties();
        _layerLockedBox.Text = "锁定";
        _layerLockedBox.CheckedChanged += (_, _) => ApplyLayerProperties();
        ConfigureNumber(_layerOpacityBox, 5, 100, 100);
        _layerOpacityBox.ValueChanged += (_, _) => ApplyLayerProperties();

        AddRow(panel, 0, "显示", _layerVisibleBox);
        AddRow(panel, 1, "锁定", _layerLockedBox);
        AddRow(panel, 2, "透明", _layerOpacityBox);
        page.Controls.Add(panel);
        return page;
    }

    private void ConfigureToolButton(ToolStripButton button, string text, string tooltip, MapEditorTool tool)
    {
        button.Text = text;
        button.ToolTipText = tooltip;
        button.DisplayStyle = ToolStripItemDisplayStyle.Text;
        button.CheckOnClick = true;
        button.Click += (_, _) => SelectTool(tool);
    }

    private static void ConfigureSmallButton(Button button, string text, Action action)
    {
        button.Text = text;
        button.Width = 30;
        button.Height = 26;
        button.Margin = new Padding(2, 0, 0, 0);
        button.Click += (_, _) => action();
    }

    private static void ConfigurePaletteButton(Button button, string text, Action action)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(4, 5, 0, 5);
        button.Click += (_, _) => action();
    }

    private static void ConfigureTextBox(TextBox box, bool multiline = false)
    {
        box.Dock = DockStyle.Fill;
        box.Multiline = multiline;
        TabSelectAllBehavior.Attach(box);
        if (multiline)
        {
            box.ScrollBars = ScrollBars.Vertical;
        }
    }

    private static void ConfigureNumber(NumericUpDown box, int min, int max, int value)
    {
        box.Dock = DockStyle.Left;
        box.Minimum = min;
        box.Maximum = max;
        box.Value = value;
        box.Width = 96;
        TabSelectAllBehavior.Attach(box);
    }

    private static NumericUpDown CreateDialogNumber(int min, int max, int value)
    {
        var box = new NumericUpDown();
        ConfigureNumber(box, min, max, Math.Clamp(value, min, max));
        return box;
    }

    private static Label HeaderLabel(string text)
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = text,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static Label FieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private static void AddRow(TableLayoutPanel panel, int row, string label, Control editor)
    {
        panel.Controls.Add(FieldLabel(label), 0, row);
        panel.Controls.Add(editor, 1, row);
    }

    private void NormalizeMaps()
    {
        foreach (var map in _context.Project.AssetLibrary.Maps)
        {
            MapDefaults.Normalize(map);
            if (map.TilesetPlan.Regions.Count > 0)
            {
                ApplyTilesetPlanToTerrains(map);
            }
        }
    }

    private void RefreshMapList()
    {
        _suppressUiEvents = true;
        _mapList.Items.Clear();
        foreach (var map in _context.Project.AssetLibrary.Maps)
        {
            _mapList.Items.Add(map);
        }

        _suppressUiEvents = false;
    }

    private void SelectFirstMap()
    {
        if (_mapList.Items.Count > 0)
        {
            _mapList.SelectedIndex = 0;
        }
    }

    private void SelectMap(MapDefinition? map)
    {
        _selectedMap = map;
        if (_selectedMap is not null)
        {
            MapDefaults.Normalize(_selectedMap);
        }

        _canvas.SetMap(_selectedMap);
        LoadTilesetPreview();
        RefreshLayerList();
        RefreshTerrainPalette();
        UpdateTilesetButtons();
        SelectLayer(_selectedMap?.Layers.FirstOrDefault(v => v.Kind.Equals("Tile", StringComparison.OrdinalIgnoreCase)) ?? _selectedMap?.Layers.FirstOrDefault());
        SelectTerrain(_selectedMap?.Terrains.FirstOrDefault()?.Id);
        UpdateStatusText();
    }

    private void ShowMapSettingsDialog()
    {
        if (_selectedMap is null)
        {
            return;
        }

        using var dialog = new Form
        {
            Text = "地图设定",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(440, 510),
            Font = Font,
            ShowInTaskbar = false
        };
        TabSelectAllBehavior.InstallRecursive(dialog);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            RowCount = 2
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 9; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 2 ? 72 : 34));
        }

        var idBox = new TextBox { Dock = DockStyle.Fill, Text = _selectedMap.Id };
        var nameBox = new TextBox { Dock = DockStyle.Fill, Text = _selectedMap.DisplayName };
        var descriptionBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, Text = _selectedMap.Description };
        var viewTypeCombo = new ComboBox { Dock = DockStyle.Left, Width = 136, DropDownStyle = ComboBoxStyle.DropDownList };
        viewTypeCombo.Items.AddRange(["TopDown", "Platformer", "Isometric"]);
        viewTypeCombo.SelectedItem = _selectedMap.ViewType;
        if (viewTypeCombo.SelectedIndex < 0)
        {
            viewTypeCombo.SelectedIndex = 0;
        }

        TabSelectAllBehavior.Attach(idBox);
        TabSelectAllBehavior.Attach(nameBox);
        TabSelectAllBehavior.Attach(descriptionBox);
        var widthBox = CreateDialogNumber(8, 512, _selectedMap.Width);
        var heightBox = CreateDialogNumber(8, 512, _selectedMap.Height);
        var tileSizeBox = CreateDialogNumber(8, 128, _selectedMap.TileSize);
        var tilesetBox = new TextBox { Dock = DockStyle.Fill, Text = _selectedMap.Tileset };
        var backgroundBox = new TextBox { Dock = DockStyle.Fill, Text = _selectedMap.BackgroundColor };
        TabSelectAllBehavior.Attach(tilesetBox);
        TabSelectAllBehavior.Attach(backgroundBox);

        AddRow(panel, 0, "ID", idBox);
        AddRow(panel, 1, "名称", nameBox);
        AddRow(panel, 2, "描述", descriptionBox);
        AddRow(panel, 3, "视角", viewTypeCombo);
        AddRow(panel, 4, "宽度", widthBox);
        AddRow(panel, 5, "高度", heightBox);
        AddRow(panel, 6, "瓦片", tileSizeBox);
        AddRow(panel, 7, "图块集", tilesetBox);
        AddRow(panel, 8, "背景", backgroundBox);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(10, 10, 10, 6)
        };
        var okButton = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 88, Height = 30 };
        var cancelButton = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 88, Height = 30 };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        root.Controls.Add(panel, 0, 0);
        root.Controls.Add(buttons, 0, 1);
        dialog.Controls.Add(root);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ApplyMapSettings(
            idBox.Text,
            nameBox.Text,
            descriptionBox.Text,
            viewTypeCombo.SelectedItem?.ToString() ?? "TopDown",
            (int)widthBox.Value,
            (int)heightBox.Value,
            (int)tileSizeBox.Value,
            tilesetBox.Text,
            backgroundBox.Text);
    }

    private void ApplyMapSettings(string id, string name, string description, string viewType, int width, int height, int tileSize, string tileset, string background)
    {
        if (_selectedMap is null)
        {
            return;
        }

        var oldWidth = _selectedMap.Width;
        var oldHeight = _selectedMap.Height;
        var oldTileSize = _selectedMap.TileSize;
        _selectedMap.Id = string.IsNullOrWhiteSpace(id) ? _selectedMap.Id : id.Trim();
        _selectedMap.DisplayName = string.IsNullOrWhiteSpace(name) ? _selectedMap.Id : name.Trim();
        _selectedMap.Description = description.Trim();
        _selectedMap.ViewType = string.IsNullOrWhiteSpace(viewType) ? "TopDown" : viewType;
        _selectedMap.Width = width;
        _selectedMap.Height = height;
        _selectedMap.TileSize = tileSize;
        _selectedMap.Tileset = string.IsNullOrWhiteSpace(tileset) ? "tileset.default" : tileset.Trim();
        _selectedMap.BackgroundColor = string.IsNullOrWhiteSpace(background) ? "#1f2937" : background.Trim();
        MapDefaults.Normalize(_selectedMap);
        if (_selectedMap.TileSize != oldTileSize)
        {
            UpdateTilesetPlanForTileSize(_selectedMap, oldTileSize, _selectedMap.TileSize);
            ApplyTilesetPlanToTerrains(_selectedMap);
        }

        if (_selectedMap.Width != oldWidth || _selectedMap.Height != oldHeight)
        {
            _canvas.SetMap(_selectedMap);
        }

        LoadTilesetPreview();
        RefreshMapListSelection(_selectedMap);
        RefreshLayerList();
        SelectLayer(_selectedLayer ?? _selectedMap.Layers.FirstOrDefault());
        _canvas.Invalidate();
        SaveProject();
    }

    private void PopulateMapProperties()
    {
        _suppressUiEvents = true;
        if (_selectedMap is null)
        {
            _idBox.Text = "";
            _nameBox.Text = "";
            _descriptionBox.Text = "";
            _viewTypeCombo.SelectedIndex = -1;
            _widthBox.Value = 64;
            _heightBox.Value = 64;
            _tileSizeBox.Value = 32;
            _tilesetBox.Text = "";
            _backgroundBox.Text = "";
        }
        else
        {
            _idBox.Text = _selectedMap.Id;
            _nameBox.Text = _selectedMap.DisplayName;
            _descriptionBox.Text = _selectedMap.Description;
            _viewTypeCombo.SelectedItem = _selectedMap.ViewType;
            if (_viewTypeCombo.SelectedIndex < 0)
            {
                _viewTypeCombo.SelectedIndex = 0;
            }

            _widthBox.Value = Math.Clamp(_selectedMap.Width, (int)_widthBox.Minimum, (int)_widthBox.Maximum);
            _heightBox.Value = Math.Clamp(_selectedMap.Height, (int)_heightBox.Minimum, (int)_heightBox.Maximum);
            _tileSizeBox.Value = Math.Clamp(_selectedMap.TileSize, (int)_tileSizeBox.Minimum, (int)_tileSizeBox.Maximum);
            _tilesetBox.Text = _selectedMap.Tileset;
            _backgroundBox.Text = _selectedMap.BackgroundColor;
        }

        _suppressUiEvents = false;
    }

    private void RefreshLayerList()
    {
        _suppressUiEvents = true;
        _layerList.Items.Clear();
        if (_selectedMap is not null)
        {
            foreach (var layer in _selectedMap.Layers.AsEnumerable().Reverse())
            {
                _layerList.Items.Add(CreateLayerItem(layer));
            }
        }

        _suppressUiEvents = false;
        UpdateLayerButtons();
    }

    private ListViewItem CreateLayerItem(MapLayerDefinition layer)
    {
        var state = $"{(layer.Visible ? "👁" : "-")} {(layer.Locked ? "🔒" : "")}".Trim();
        return new ListViewItem([layer.Name, LayerKindLabel(layer.Kind), state]) { Tag = layer };
    }

    private void UpdateLayerListText()
    {
        foreach (ListViewItem item in _layerList.Items)
        {
            if (item.Tag is not MapLayerDefinition layer)
            {
                continue;
            }

            item.SubItems[0].Text = layer.Name;
            item.SubItems[1].Text = LayerKindLabel(layer.Kind);
            item.SubItems[2].Text = $"{(layer.Visible ? "👁" : "-")} {(layer.Locked ? "🔒" : "")}".Trim();
        }
    }

    private void SelectLayer(MapLayerDefinition? layer)
    {
        _selectedLayer = layer;
        _canvas.SetActiveLayer(_selectedLayer);

        _suppressUiEvents = true;
        foreach (ListViewItem item in _layerList.Items)
        {
            item.Selected = ReferenceEquals(item.Tag, layer);
        }

        _layerVisibleBox.Checked = layer?.Visible ?? false;
        _layerLockedBox.Checked = layer?.Locked ?? false;
        _layerOpacityBox.Value = layer is null ? 100 : Math.Clamp((int)Math.Round(layer.Opacity * 100), 5, 100);
        _suppressUiEvents = false;
        UpdateLayerButtons();
        UpdateStatusText();
    }

    private void RefreshTerrainPalette()
    {
        _terrainPalette.SuspendLayout();
        _terrainPalette.Controls.Clear();
        if (_selectedMap is not null)
        {
            foreach (var terrain in _selectedMap.Terrains)
            {
                _terrainPalette.Controls.Add(CreateTerrainButton(terrain));
            }
        }

        _terrainPalette.ResumeLayout();
    }

    private Control CreateTerrainButton(MapTerrainDefinition terrain)
    {
        var button = new Button
        {
            Width = 138,
            Height = 72,
            Margin = new Padding(0, 0, 8, 0),
            Text = TerrainButtonText(terrain),
            TextAlign = ContentAlignment.BottomCenter,
            Tag = terrain,
            FlatStyle = FlatStyle.Standard
        };
        button.Paint += (_, e) =>
        {
            var color = MapDefaults.ParseColor(terrain.ColorHex, Color.Gray);
            var edge = MapDefaults.ParseColor(terrain.EdgeColorHex, Color.DarkGray);
            using var brush = new SolidBrush(color);
            using var edgeBrush = new SolidBrush(edge);
            var swatch = new Rectangle(12, 10, button.Width - 24, 28);
            e.Graphics.FillRectangle(brush, swatch);
            e.Graphics.FillRectangle(edgeBrush, swatch.Left, swatch.Top, swatch.Width, 5);
            e.Graphics.FillRectangle(edgeBrush, swatch.Left, swatch.Bottom - 5, swatch.Width, 5);
            e.Graphics.DrawRectangle(Pens.White, swatch);
            if (ReferenceEquals(_selectedTerrain, terrain))
            {
                using var pen = new Pen(Color.FromArgb(0, 120, 215), 3);
                e.Graphics.DrawRectangle(pen, 2, 2, button.Width - 5, button.Height - 5);
            }
        };
        button.Click += (_, _) => SelectTerrain(terrain.Id);
        return button;
    }

    private void SelectTerrain(string? terrainId)
    {
        _selectedTerrain = _selectedMap?.Terrains.FirstOrDefault(v => string.Equals(v.Id, terrainId, StringComparison.OrdinalIgnoreCase))
            ?? _selectedMap?.Terrains.FirstOrDefault();
        _canvas.SetSelectedTerrain(_selectedTerrain);
        foreach (Control control in _terrainPalette.Controls)
        {
            control.Invalidate();
        }

        UpdateStatusText();
        UpdateTilesetInfo();
        UpdateA2Highlight();
        UpdateBrushButtons();
    }

    private void BindSelectedTerrainToA2()
    {
        if (_selectedMap is null || _selectedTerrain is null)
        {
            return;
        }

        if (_selectedTilesetTile.X < 0 || _selectedTilesetTile.Y < 0)
        {
            MessageBox.Show(this, "请先在图集中选择一个 RPG Maker A2 自动元件块。", "绑定 A2 自动元件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var origin = RpgMakerAutoTile.SnapA2Origin(_selectedTilesetTile.X, _selectedTilesetTile.Y);
        _selectedTerrain.Rule = MapDefaults.RuleRpgMakerA2;
        _selectedTerrain.TileX = origin.X;
        _selectedTerrain.TileY = origin.Y;
        RefreshTerrainPalette();
        SelectTerrain(_selectedTerrain.Id);
        _canvas.Invalidate();
        UpdateTilesetInfo();
        SaveProject();
    }

    private void SelectTilesetTile(int tileX, int tileY, int width, int height, bool useAsBrush)
    {
        _selectedTilesetTile = new Point(tileX, tileY);
        _tilesetPalette.SelectedTile = _selectedTilesetTile;
        var isA1OverlayTile = IsA1OverlayTile(tileX, tileY);
        var isA1OverlayAnimated = IsA1AnimatedOverlayTile(tileX, tileY);
        if (useAsBrush)
        {
            if (isA1OverlayTile && !_shiftAutoTerrainMode)
            {
                _shiftTerrain = FindPlannedAutoTerrain(tileX, tileY);
                _canvas.SetShiftTerrain(_shiftTerrain);
                _canvas.SetTilesetSelection(tileX, tileY, 1, 1, isA1Overlay: true, isA1OverlayAnimated: isA1OverlayAnimated);
                _canvas.UseTilesetBrush();
                _selectedTerrain = null;
                _canvas.SetSelectedTerrain(null);
            }
            else if (TrySelectPlannedAutoTerrain(tileX, tileY))
            {
                _shiftTerrain = _selectedTerrain;
                _canvas.SetShiftTerrain(_shiftTerrain);
                _canvas.SetTilesetSelection(tileX, tileY, width, height, isA1OverlayTile, isA1OverlayAnimated);
                _canvas.UseTerrainBrush();
            }
            else
            {
                _shiftTerrain = null;
                _canvas.SetShiftTerrain(_shiftTerrain);
                _canvas.SetTilesetSelection(tileX, tileY, width, height, isA1OverlayTile, isA1OverlayAnimated);
                _canvas.UseTilesetBrush();
            }
        }
        else
        {
            _canvas.SetTilesetSelection(tileX, tileY, width, height, isA1OverlayTile, isA1OverlayAnimated);
        }

        UpdateTilesetInfo();
        UpdateA2Highlight();
        UpdateBrushButtons();
    }

    private bool IsA1OverlayTile(int tileX, int tileY)
    {
        return FindPlannedRegion(tileX, tileY) is { } region
            && string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(region.Variant, RpgMakerA1RegionVariants.OceanDecor, StringComparison.OrdinalIgnoreCase)
                || string.Equals(region.Variant, RpgMakerA1RegionVariants.Waterfall, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsA1AnimatedOverlayTile(int tileX, int tileY)
    {
        return FindPlannedRegion(tileX, tileY) is { } region
            && string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase)
            && string.Equals(region.Variant, RpgMakerA1RegionVariants.Waterfall, StringComparison.OrdinalIgnoreCase);
    }

    private bool TrySelectPlannedAutoTerrain(int tileX, int tileY)
    {
        var terrain = FindPlannedAutoTerrain(tileX, tileY);
        if (terrain is null)
        {
            return false;
        }

        SelectTerrain(terrain.Id);
        return true;
    }

    private MapTerrainDefinition? FindPlannedAutoTerrain(int tileX, int tileY)
    {
        if (_selectedMap?.TilesetPlan?.Regions is null)
        {
            return null;
        }

        var region = FindPlannedRegion(tileX, tileY);
        if (region is null)
        {
            return null;
        }

        string terrainId;
        if (string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase))
        {
            var blockWidth = GetA1RegionBlockWidth(region);
            var blockHeight = GetA1RegionBlockHeight(region);
            var blockX = region.X + (tileX - region.X) / blockWidth * blockWidth;
            var blockY = region.Y + (tileY - region.Y) / blockHeight * blockHeight;
            terrainId = CreateA1TerrainId(blockX, blockY, region);
        }
        else if (string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA2, StringComparison.OrdinalIgnoreCase))
        {
            var blockX = region.X + (tileX - region.X) / 2 * 2;
            var blockY = region.Y + (tileY - region.Y) / 3 * 3;
            terrainId = CreateA2TerrainId(blockX, blockY);
        }
        else
        {
            return null;
        }

        return _selectedMap.Terrains.FirstOrDefault(v => string.Equals(v.Id, terrainId, StringComparison.OrdinalIgnoreCase));
    }

    private TilesetRegionDefinition? FindPlannedRegion(int tileX, int tileY)
    {
        return _selectedMap?.TilesetPlan?.Regions.FirstOrDefault(region =>
            tileX >= region.X
            && tileY >= region.Y
            && tileX < region.X + region.Width
            && tileY < region.Y + region.Height);
    }

    private void UseTerrainBrush()
    {
        _canvas.UseTerrainBrush();
        UpdateStatusText();
        UpdateBrushButtons();
    }

    private void UseTilesetBrush()
    {
        if (_selectedTilesetTile.X < 0 || _selectedTilesetTile.Y < 0)
        {
            MessageBox.Show(this, "请先在图集中选择一个瓦片。", "图块画笔", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _canvas.UseTilesetBrush();
        UpdateStatusText();
        UpdateBrushButtons();
    }

    private void SelectTool(MapEditorTool tool)
    {
        _canvas.ActiveTool = tool;
        _paintButton.Checked = tool == MapEditorTool.Paint;
        _eraseButton.Checked = tool == MapEditorTool.Erase;
        _fillButton.Checked = tool == MapEditorTool.Fill;
        _rectButton.Checked = tool == MapEditorTool.Rectangle;
        _pickButton.Checked = tool == MapEditorTool.Eyedropper;
        UpdateStatusText();
    }

    private void CreateMap()
    {
        var id = UniqueId("map.custom", _context.Project.AssetLibrary.Maps.Select(v => v.Id));
        var map = new MapDefinition
        {
            Id = id,
            DisplayName = "新地图",
            Description = "",
            ViewType = "TopDown",
            Width = 48,
            Height = 32,
            TileSize = 32,
            Tileset = "tileset.default"
        };
        MapDefaults.Normalize(map);
        _context.Project.AssetLibrary.Maps.Add(map);
        RefreshMapList();
        _mapList.SelectedItem = map;
        SaveProject();
    }

    private void DeleteMap()
    {
        if (_selectedMap is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(_selectedMap.DisplayName) ? _selectedMap.Id : _selectedMap.DisplayName;
        var confirm = MessageBox.Show(
            this,
            $"确定删除地图“{name}”吗？",
            "删除地图",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.OK)
        {
            return;
        }

        var index = _context.Project.AssetLibrary.Maps.IndexOf(_selectedMap);
        _context.Project.AssetLibrary.Maps.Remove(_selectedMap);
        _selectedMap = null;
        _selectedLayer = null;
        _selectedTerrain = null;
        RefreshMapList();

        if (_mapList.Items.Count > 0)
        {
            _mapList.SelectedIndex = Math.Clamp(index, 0, _mapList.Items.Count - 1);
        }
        else
        {
            SelectMap(null);
        }

        SaveProject();
    }

    private void CreateLayer()
    {
        if (_selectedMap is null)
        {
            return;
        }

        var id = UniqueId("layer.custom", _selectedMap.Layers.Select(v => v.Id));
        var layer = new MapLayerDefinition
        {
            Id = id,
            Name = "新图层",
            Kind = "Tile",
            Visible = true,
            Locked = false,
            Opacity = 1f,
            Tiles = []
        };
        _selectedMap.Layers.Add(layer);
        RefreshLayerList();
        SelectLayer(layer);
    }

    private void DeleteLayer()
    {
        if (_selectedMap is null || _selectedLayer is null || _selectedMap.Layers.Count <= 1)
        {
            return;
        }

        var index = _selectedMap.Layers.IndexOf(_selectedLayer);
        _selectedMap.Layers.Remove(_selectedLayer);
        RefreshLayerList();
        SelectLayer(_selectedMap.Layers[Math.Clamp(index - 1, 0, _selectedMap.Layers.Count - 1)]);
    }

    private void MoveLayer(int direction)
    {
        if (_selectedMap is null || _selectedLayer is null)
        {
            return;
        }

        var index = _selectedMap.Layers.IndexOf(_selectedLayer);
        var next = index + direction;
        if (index < 0 || next < 0 || next >= _selectedMap.Layers.Count)
        {
            return;
        }

        _selectedMap.Layers.RemoveAt(index);
        _selectedMap.Layers.Insert(next, _selectedLayer);
        RefreshLayerList();
        SelectLayer(_selectedLayer);
    }

    private void ApplyMapProperties()
    {
        ApplyMapSettings(
            _idBox.Text,
            _nameBox.Text,
            _descriptionBox.Text,
            _viewTypeCombo.SelectedItem?.ToString() ?? "TopDown",
            (int)_widthBox.Value,
            (int)_heightBox.Value,
            (int)_tileSizeBox.Value,
            _tilesetBox.Text,
            _backgroundBox.Text);
    }

    private void ApplyLayerProperties()
    {
        if (_suppressUiEvents || _selectedLayer is null)
        {
            return;
        }

        _selectedLayer.Visible = _layerVisibleBox.Checked;
        _selectedLayer.Locked = _layerLockedBox.Checked;
        _selectedLayer.Opacity = (float)_layerOpacityBox.Value / 100f;
        UpdateLayerListText();
        _canvas.Invalidate();
    }

    private void RefreshMapListSelection(MapDefinition selected)
    {
        _suppressUiEvents = true;
        var index = _mapList.SelectedIndex;
        _mapList.Items.Clear();
        foreach (var map in _context.Project.AssetLibrary.Maps)
        {
            _mapList.Items.Add(map);
        }

        _mapList.SelectedItem = selected;
        if (_mapList.SelectedIndex < 0 && index >= 0 && index < _mapList.Items.Count)
        {
            _mapList.SelectedIndex = index;
        }

        _suppressUiEvents = false;
    }

    private void UpdateLayerButtons()
    {
        var hasMap = _selectedMap is not null;
        var hasLayer = _selectedLayer is not null;
        _addLayerButton.Enabled = hasMap;
        _deleteLayerButton.Enabled = hasMap && hasLayer && _selectedMap!.Layers.Count > 1;
        _moveLayerUpButton.Enabled = hasMap && hasLayer && _selectedMap!.Layers.IndexOf(_selectedLayer!) < _selectedMap.Layers.Count - 1;
        _moveLayerDownButton.Enabled = hasMap && hasLayer && _selectedMap!.Layers.IndexOf(_selectedLayer!) > 0;
    }

    private void UpdateStatusText()
    {
        var map = _selectedMap?.DisplayName ?? "-";
        var layer = _selectedLayer?.Name ?? "-";
        var terrain = _selectedTerrain?.DisplayName ?? _selectedTerrain?.Id ?? "-";
        var brush = _canvas.BrushSource == MapBrushSource.Tileset && _selectedTilesetTile.X >= 0
            ? $"图块 {_selectedTilesetTile.X},{_selectedTilesetTile.Y}"
            : $"地形 {terrain}";
        _statusLabel.Text = $"{map} | {layer} | {brush}{ShiftStatusSuffix()}";
    }

    private string ShiftStatusSuffix()
    {
        if (!_shiftAutoTerrainMode)
        {
            return string.Empty;
        }

        var terrain = _shiftTerrain ?? _selectedTerrain;
        var name = terrain?.DisplayName ?? terrain?.Id;
        if (_shiftTerrain is null)
        {
            return " | Shift 已按下";
        }

        return string.IsNullOrWhiteSpace(name)
            ? " | Shift 自动地形"
            : $" | Shift 自动地形: {name}";
    }

    private void SaveProject()
    {
        if (_selectedMap is not null)
        {
            MapDefaults.Normalize(_selectedMap);
        }

        _projectService.SaveProject(_context);
        UpdateStatusText();
    }

    private void ImportTileset()
    {
        if (_selectedMap is null)
        {
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif",
            Title = "选择图集图片"
        };
        ApplyTilesetDialogDirectory(dialog);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SaveLastTilesetDirectory(Path.GetDirectoryName(dialog.FileName));
        var importMode = ChooseTilesetImportMode(_selectedMap);
        if (importMode == TilesetImportMode.Cancel)
        {
            return;
        }

        using var stream = File.OpenRead(dialog.FileName);
        using var image = Image.FromStream(stream);
        using var planDialog = new TilesetPlannerDialog(image, _selectedMap.TileSize, null);
        if (planDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (importMode == TilesetImportMode.Append)
        {
            AppendTileset(_selectedMap, image, EnsureAppendPlanHasRegions(planDialog.Plan, image, _selectedMap.TileSize));
        }
        else
        {
            _selectedMap.TilesetImagePath = dialog.FileName;
            _selectedMap.TilesetPlan = planDialog.Plan;
        }

        _tilesetBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        ApplyTilesetPlanToTerrains(_selectedMap);
        LoadTilesetPreview();
        RefreshTerrainPalette();
        SelectTerrain(_selectedTerrain?.Id ?? _selectedMap.Terrains.FirstOrDefault()?.Id);
        UpdateTilesetInfo();
        UpdateA2Highlight();
        SaveProject();
    }

    private void ApplyTilesetDialogDirectory(FileDialog dialog)
    {
        if (!string.IsNullOrWhiteSpace(_settings.LastTilesetDirectory) && Directory.Exists(_settings.LastTilesetDirectory))
        {
            dialog.InitialDirectory = _settings.LastTilesetDirectory;
        }
    }

    private void SaveLastTilesetDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        _settings.LastTilesetDirectory = directory;
        _settingsService.Save(_settings);
    }

    private TilesetImportMode ChooseTilesetImportMode(MapDefinition map)
    {
        if (string.IsNullOrWhiteSpace(map.TilesetImagePath))
        {
            return TilesetImportMode.Replace;
        }

        var result = MessageBox.Show(
            this,
            "当前地图已经存在图集。\r\n\r\n选择“是”：追加到现有图集下方。\r\n选择“否”：覆盖当前图集。",
            "导入图集",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);

        return result switch
        {
            DialogResult.Yes => TilesetImportMode.Append,
            DialogResult.No => TilesetImportMode.Replace,
            _ => TilesetImportMode.Cancel
        };
    }

    private void AppendTileset(MapDefinition map, Image appendedImage, TilesetPlanDefinition appendedPlan)
    {
        if (string.IsNullOrWhiteSpace(map.TilesetImagePath) || !File.Exists(map.TilesetImagePath))
        {
            map.TilesetImagePath = SaveImportedTilesetImage(appendedImage, map.Id);
            map.TilesetPlan = appendedPlan;
            return;
        }

        using var stream = File.OpenRead(map.TilesetImagePath);
        using var existingImage = Image.FromStream(stream);
        var tileSize = Math.Max(8, appendedPlan.TileSize > 0 ? appendedPlan.TileSize : CurrentTilesetTileSize(map));
        var existingTileRows = (int)Math.Ceiling(existingImage.Height / (double)tileSize);
        using var mergedImage = MergeTilesetImages(existingImage, appendedImage, tileSize);

        map.TilesetImagePath = SaveImportedTilesetImage(mergedImage, map.Id);
        map.TilesetPlan = MergeTilesetPlans(map.TilesetPlan, appendedPlan, existingTileRows);
    }

    private string SaveImportedTilesetImage(Image image, string mapId)
    {
        var assetsPath = string.IsNullOrWhiteSpace(_context.Project.Paths?.Assets)
            ? "Assets"
            : _context.Project.Paths.Assets;
        var tilesetDirectory = Path.Combine(_context.RootDirectory, assetsPath, "Tilesets");
        Directory.CreateDirectory(tilesetDirectory);

        var safeMapId = SafeFileName(string.IsNullOrWhiteSpace(mapId) ? "map" : mapId);
        var fileName = $"{safeMapId}.tileset.{DateTime.Now:yyyyMMddHHmmss}.png";
        var path = UniqueFilePath(Path.Combine(tilesetDirectory, fileName));
        image.Save(path, ImageFormat.Png);
        return path;
    }

    private static Bitmap MergeTilesetImages(Image existingImage, Image appendedImage, int tileSize)
    {
        var existingRows = (int)Math.Ceiling(existingImage.Height / (double)tileSize);
        var appendedRows = (int)Math.Ceiling(appendedImage.Height / (double)tileSize);
        var width = Math.Max(existingImage.Width, appendedImage.Width);
        var height = Math.Max(tileSize, (existingRows + appendedRows) * tileSize);
        var merged = new Bitmap(width, height);
        using var g = Graphics.FromImage(merged);
        g.Clear(Color.Transparent);
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        g.DrawImage(existingImage, 0, 0, existingImage.Width, existingImage.Height);
        g.DrawImage(appendedImage, 0, existingRows * tileSize, appendedImage.Width, appendedImage.Height);
        return merged;
    }

    private static TilesetPlanDefinition MergeTilesetPlans(TilesetPlanDefinition existingPlan, TilesetPlanDefinition appendedPlan, int yOffset)
    {
        var regions = CloneTilesetRegions(existingPlan.Regions);
        regions.AddRange(CloneTilesetRegions(appendedPlan.Regions).Select(region =>
        {
            region.Id = string.IsNullOrWhiteSpace(region.Id)
                ? $"tileset.region.{Guid.NewGuid():N}"
                : $"{region.Id}.append.{Guid.NewGuid():N}";
            region.Y += yOffset;
            return region;
        }));

        return new TilesetPlanDefinition
        {
            TileSize = existingPlan.TileSize > 0 ? existingPlan.TileSize : appendedPlan.TileSize,
            Mode = existingPlan.Mode,
            RpgMakerKind = existingPlan.RpgMakerKind,
            RpgMakerLayout = existingPlan.RpgMakerLayout,
            Regions = regions
        };
    }

    private static List<TilesetRegionDefinition> CloneTilesetRegions(IEnumerable<TilesetRegionDefinition>? regions)
    {
        return (regions ?? [])
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

    private static TilesetPlanDefinition EnsureAppendPlanHasRegions(TilesetPlanDefinition plan, Image image, int fallbackTileSize)
    {
        var result = new TilesetPlanDefinition
        {
            TileSize = Math.Clamp(plan.TileSize > 0 ? plan.TileSize : fallbackTileSize, 8, 128),
            Mode = plan.Mode,
            RpgMakerKind = plan.RpgMakerKind,
            RpgMakerLayout = plan.RpgMakerLayout,
            Regions = CloneTilesetRegions(plan.Regions)
        };

        if (result.Regions.Count > 0)
        {
            return result;
        }

        result.Mode = TilesetPlanModes.Normal;
        result.Regions.Add(new TilesetRegionDefinition
        {
            Id = $"tileset.region.normal.{Guid.NewGuid():N}",
            Name = "普通追加",
            Kind = TilesetRegionKinds.Normal,
            Variant = string.Empty,
            X = 0,
            Y = 0,
            Width = Math.Max(1, image.Width / result.TileSize),
            Height = Math.Max(1, image.Height / result.TileSize)
        });
        return result;
    }

    private static void UpdateTilesetPlanForTileSize(MapDefinition map, int oldTileSize, int newTileSize)
    {
        if (map.TilesetPlan is null)
        {
            map.TilesetPlan = new TilesetPlanDefinition { TileSize = newTileSize };
            return;
        }

        oldTileSize = Math.Clamp(map.TilesetPlan.TileSize > 0 ? map.TilesetPlan.TileSize : oldTileSize, 8, 128);
        newTileSize = Math.Clamp(newTileSize, 8, 128);
        if (oldTileSize == newTileSize)
        {
            return;
        }

        var hasOnlyGeneratedA2Regions = HasOnlyGeneratedA2Regions(map.TilesetPlan);
        map.TilesetPlan.TileSize = newTileSize;
        if (!hasOnlyGeneratedA2Regions)
        {
            ScaleTilesetRegions(map.TilesetPlan, oldTileSize, newTileSize);
            return;
        }

        var columns = EstimateTilesetColumns(map.TilesetImagePath, newTileSize);
        var rows = EstimateTilesetRows(map.TilesetImagePath, newTileSize);
        if (columns < 4 || rows < 3)
        {
            return;
        }

        map.TilesetPlan.Regions = CreateStandardA2Regions(columns, rows);
    }

    private static void ScaleTilesetRegions(TilesetPlanDefinition plan, int oldTileSize, int newTileSize)
    {
        foreach (var region in plan.Regions)
        {
            var left = region.X * oldTileSize;
            var top = region.Y * oldTileSize;
            var right = (region.X + region.Width) * oldTileSize;
            var bottom = (region.Y + region.Height) * oldTileSize;
            region.X = Math.Max(0, (int)Math.Floor(left / (double)newTileSize));
            region.Y = Math.Max(0, (int)Math.Floor(top / (double)newTileSize));
            region.Width = Math.Max(1, (int)Math.Ceiling(right / (double)newTileSize) - region.X);
            region.Height = Math.Max(1, (int)Math.Ceiling(bottom / (double)newTileSize) - region.Y);
        }
    }

    private static bool HasOnlyGeneratedA2Regions(TilesetPlanDefinition plan)
    {
        return plan.Regions.Count > 0
            && plan.Regions.All(region =>
                string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA2, StringComparison.OrdinalIgnoreCase)
                && region.Width == 2
                && region.Height == 3
                && region.X % 2 == 0
                && region.Y % 3 == 0);
    }

    private static List<TilesetRegionDefinition> CreateStandardA2Regions(int columns, int rows)
    {
        var regions = new List<TilesetRegionDefinition>();
        for (var y = 0; y + 3 <= rows; y += 3)
        {
            for (var x = 0; x + 4 <= columns; x += 4)
            {
                regions.Add(CreateStandardA2Region(x, y));
                regions.Add(CreateStandardA2Region(x + 2, y));
            }
        }

        return regions;
    }

    private static TilesetRegionDefinition CreateStandardA2Region(int x, int y)
    {
        return new TilesetRegionDefinition
        {
            Id = $"tileset.region.a2.{x}.{y}.{Guid.NewGuid():N}",
            Name = $"RM A2 {x},{y}",
            Kind = TilesetRegionKinds.RpgMakerA2,
            Variant = string.Empty,
            X = x,
            Y = y,
            Width = 2,
            Height = 3
        };
    }

    private static int EstimateTilesetColumns(string imagePath, int tileSize)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return 0;
        }

        using var stream = File.OpenRead(imagePath);
        using var image = Image.FromStream(stream);
        return Math.Max(1, image.Width / Math.Max(8, tileSize));
    }

    private static int EstimateTilesetRows(string imagePath, int tileSize)
    {
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
        {
            return 0;
        }

        using var stream = File.OpenRead(imagePath);
        using var image = Image.FromStream(stream);
        return Math.Max(1, image.Height / Math.Max(8, tileSize));
    }

    private static int CurrentTilesetTileSize(MapDefinition map)
    {
        var plannedTileSize = map.TilesetPlan?.TileSize ?? 0;
        return Math.Clamp(plannedTileSize > 0 ? plannedTileSize : map.TileSize, 8, 128);
    }

    private void PlanTileset()
    {
        if (_selectedMap is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedMap.TilesetImagePath) || !File.Exists(_selectedMap.TilesetImagePath))
        {
            MessageBox.Show(this, "请先导入一张图集图片。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var stream = File.OpenRead(_selectedMap.TilesetImagePath);
        using var image = Image.FromStream(stream);
        using var dialog = new TilesetPlannerDialog(image, CurrentTilesetTileSize(_selectedMap), _selectedMap.TilesetPlan);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _selectedMap.TilesetPlan = dialog.Plan;
        ApplyTilesetPlanToTerrains(_selectedMap);
        _tilesetPalette.SetTilesetPlan(_selectedMap.TilesetPlan);
        RefreshTerrainPalette();
        SelectTerrain(_selectedTerrain?.Id ?? _selectedMap.Terrains.FirstOrDefault()?.Id);
        UpdateTilesetInfo();
        UpdateA2Highlight();
        SaveProject();
    }

    private void LoadTilesetPreview()
    {
        if (_selectedMap is null || string.IsNullOrWhiteSpace(_selectedMap.TilesetImagePath) || !File.Exists(_selectedMap.TilesetImagePath))
        {
            _tilesetPalette.SetTileset(null, _selectedMap is null ? 32 : CurrentTilesetTileSize(_selectedMap));
            _tilesetPalette.SetTilesetPlan(null);
            _canvas.SetTilesetImage(null);
            _selectedTilesetTile = new Point(-1, -1);
            UpdateTilesetInfo();
            UpdateA2Highlight();
            UpdateBrushButtons();
            UpdateTilesetButtons();
            return;
        }

        using var stream = File.OpenRead(_selectedMap.TilesetImagePath);
        using var image = Image.FromStream(stream);
        var paletteImage = new Bitmap(image);
        var canvasImage = new Bitmap(image);
        _tilesetPalette.SetTileset(paletteImage, CurrentTilesetTileSize(_selectedMap));
        _tilesetPalette.SetTilesetPlan(_selectedMap.TilesetPlan);
        _canvas.SetTilesetImage(canvasImage);
        UpdateTilesetInfo();
        UpdateA2Highlight();
        UpdateBrushButtons();
        UpdateTilesetButtons();
    }

    private void UpdateTilesetInfo()
    {
        if (_selectedMap is null || string.IsNullOrWhiteSpace(_selectedMap.TilesetImagePath))
        {
            _tilesetInfoLabel.Text = "图集: 未导入";
            return;
        }

        var name = Path.GetFileName(_selectedMap.TilesetImagePath);
        var tileText = _selectedTilesetTile.X >= 0 ? $" | 当前图块 {_selectedTilesetTile.X},{_selectedTilesetTile.Y}" : "";
        var autoText = _selectedTerrain is not null && RpgMakerAutoTile.IsA1(_selectedTerrain)
            ? $" | {_selectedTerrain.DisplayName} A1 {_selectedTerrain.TileX},{_selectedTerrain.TileY}"
            : _selectedTerrain is not null && RpgMakerAutoTile.IsA2(_selectedTerrain)
                ? $" | {_selectedTerrain.DisplayName} A2 {_selectedTerrain.TileX},{_selectedTerrain.TileY}"
            : "";
        var regionCount = _selectedMap.TilesetPlan?.Regions?.Count ?? 0;
        var planText = regionCount > 0 ? $" | 区域 {regionCount}" : "";
        _tilesetInfoLabel.Text = $"图集: {name}{tileText}{autoText}{planText}";
    }

    private static void ApplyTilesetPlanToTerrains(MapDefinition map)
    {
        var a1Origins = EnumerateA1Origins(map.TilesetPlan)
            .Distinct()
            .ToList();
        var a2Origins = EnumerateA2Origins(map.TilesetPlan)
            .Distinct()
            .ToList();
        foreach (var origin in a1Origins)
        {
            var sourceRegion = FindA1RegionForOrigin(map.TilesetPlan, origin);
            var newId = CreateA1TerrainId(origin.X, origin.Y, sourceRegion);
            foreach (var legacyId in LegacyA1TerrainIds(origin.X, origin.Y))
            {
                ReplaceTerrainReferences(map, legacyId, newId);
            }
        }

        var expectedIds = a1Origins
            .Select(v =>
            {
                var sourceRegion = FindA1RegionForOrigin(map.TilesetPlan, v);
                return CreateA1TerrainId(v.X, v.Y, sourceRegion);
            })
            .Concat(a2Origins.Select(v => CreateA2TerrainId(v.X, v.Y)))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var terrain in map.Terrains.Where(v => v.Rule.Equals(MapDefaults.RuleRpgMakerA1, StringComparison.OrdinalIgnoreCase) || v.Rule.Equals(MapDefaults.RuleRpgMakerA2, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            if (!expectedIds.Contains(terrain.Id) && IsGeneratedTilesetTerrain(terrain.Id))
            {
                map.Terrains.Remove(terrain);
            }
        }

        foreach (var origin in a1Origins)
        {
            var sourceRegion = FindA1RegionForOrigin(map.TilesetPlan, origin);
            var isWaterfall = sourceRegion is not null && IsA1WaterfallLike(sourceRegion);
            var id = CreateA1TerrainId(origin.X, origin.Y, sourceRegion);
            var terrain = map.Terrains.FirstOrDefault(v => string.Equals(v.Id, id, StringComparison.OrdinalIgnoreCase));
            if (terrain is null)
            {
                terrain = new MapTerrainDefinition
                {
                    Id = id,
                    DisplayName = A1TerrainDisplayName(sourceRegion, origin),
                    ColorHex = "#3b82f6",
                    EdgeColorHex = "#1d4ed8"
                };
                map.Terrains.Add(terrain);
            }
            else
            {
                terrain.DisplayName = A1TerrainDisplayName(sourceRegion, origin);
            }

            terrain.Rule = MapDefaults.RuleRpgMakerA1;
            var isAnimated = sourceRegion is not null
                && (isWaterfall
                    || string.Equals(sourceRegion.Variant, RpgMakerA1RegionVariants.Ocean, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sourceRegion.Variant, RpgMakerA1RegionVariants.DeepSea, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(sourceRegion.Variant, RpgMakerA1RegionVariants.Water, StringComparison.OrdinalIgnoreCase));
            terrain.Animated = isAnimated;
            terrain.AnimationFrames = isAnimated ? (isWaterfall ? 3 : 4) : 1;
            terrain.AnimationFps = 4;
            terrain.TileX = origin.X;
            terrain.TileY = origin.Y;
        }

        foreach (var origin in a2Origins)
        {
            var id = CreateA2TerrainId(origin.X, origin.Y);
            var terrain = map.Terrains.FirstOrDefault(v => string.Equals(v.Id, id, StringComparison.OrdinalIgnoreCase));
            if (terrain is null)
            {
                terrain = new MapTerrainDefinition
                {
                    Id = id,
                    DisplayName = $"A2自动 {origin.X},{origin.Y}",
                    ColorHex = "#4f9f55",
                    EdgeColorHex = "#2f6f38"
                };
                map.Terrains.Add(terrain);
            }

            terrain.Rule = MapDefaults.RuleRpgMakerA2;
            terrain.TileX = origin.X;
            terrain.TileY = origin.Y;
        }
    }

    private static IEnumerable<Point> EnumerateA1Origins(TilesetPlanDefinition plan)
    {
        foreach (var region in plan.Regions.Where(v => string.Equals(v.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase)))
        {
            var blockWidth = GetA1RegionBlockWidth(region);
            var blockHeight = GetA1RegionBlockHeight(region);
            var blockColumns = Math.Max(1, region.Width / blockWidth);
            var blockRows = Math.Max(1, region.Height / blockHeight);
            for (var row = 0; row < blockRows; row++)
            {
                for (var column = 0; column < blockColumns; column++)
                {
                    yield return new Point(region.X + column * blockWidth, region.Y + row * blockHeight);
                }
            }
        }
    }

    private static int GetA1RegionBlockWidth(TilesetRegionDefinition region)
    {
        if (IsA1WaterfallLike(region))
        {
            return 2;
        }

        return region.Width % 6 == 0 && region.Width >= 6 ? 6 : 2;
    }

    private static int GetA1RegionBlockHeight(TilesetRegionDefinition region)
    {
        return 3;
    }

    private static TilesetRegionDefinition? FindA1RegionForOrigin(TilesetPlanDefinition plan, Point origin)
    {
        return plan.Regions.FirstOrDefault(region =>
            string.Equals(region.Kind, TilesetRegionKinds.RpgMakerA1, StringComparison.OrdinalIgnoreCase)
            && origin.X >= region.X
            && origin.Y >= region.Y
            && origin.X < region.X + region.Width
            && origin.Y < region.Y + region.Height);
    }

    private static bool IsA1WaterfallLike(TilesetRegionDefinition region)
    {
        return string.Equals(region.Variant, RpgMakerA1RegionVariants.Waterfall, StringComparison.OrdinalIgnoreCase);
    }

    private static string A1TerrainDisplayName(TilesetRegionDefinition? region, Point origin)
    {
        var label = region?.Variant switch
        {
            RpgMakerA1RegionVariants.Ocean => "A海洋",
            RpgMakerA1RegionVariants.DeepSea => "B深海",
            RpgMakerA1RegionVariants.OceanDecor => "C海洋装饰",
            RpgMakerA1RegionVariants.Waterfall => "E瀑布",
            _ => "D水面"
        };
        return $"A1{label} {origin.X},{origin.Y}";
    }

    private static IEnumerable<Point> EnumerateA2Origins(TilesetPlanDefinition plan)
    {
        foreach (var region in plan.Regions.Where(v => string.Equals(v.Kind, TilesetRegionKinds.RpgMakerA2, StringComparison.OrdinalIgnoreCase)))
        {
            var blockColumns = Math.Max(1, region.Width / 2);
            var blockRows = Math.Max(1, region.Height / 3);
            for (var row = 0; row < blockRows; row++)
            {
                for (var column = 0; column < blockColumns; column++)
                {
                    yield return new Point(region.X + column * 2, region.Y + row * 3);
                }
            }
        }
    }

    private static bool IsGeneratedTilesetTerrain(string id)
    {
        return id.StartsWith("terrain.tileset.a1.", StringComparison.OrdinalIgnoreCase)
            || id.StartsWith("terrain.tileset.a2.", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateA1TerrainId(int x, int y, TilesetRegionDefinition? region)
    {
        var key = region?.Variant switch
        {
            RpgMakerA1RegionVariants.Ocean => "ocean",
            RpgMakerA1RegionVariants.DeepSea => "deepsea",
            RpgMakerA1RegionVariants.OceanDecor => "decor",
            RpgMakerA1RegionVariants.Waterfall => "waterfall",
            _ => "water"
        };
        return $"terrain.tileset.a1.{key}.{x}.{y}";
    }

    private static IEnumerable<string> LegacyA1TerrainIds(int x, int y)
    {
        yield return $"terrain.tileset.a1.{x}.{y}";
        yield return $"terrain.tileset.a1.waterfall.{x}.{y}";
    }

    private static void ReplaceTerrainReferences(MapDefinition map, string oldId, string newId)
    {
        if (string.Equals(oldId, newId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        foreach (var layer in map.Layers)
        {
            foreach (var tile in layer.Tiles)
            {
                if (string.Equals(tile.TerrainId, oldId, StringComparison.OrdinalIgnoreCase))
                {
                    tile.TerrainId = newId;
                }
            }
        }
    }

    private static string CreateA2TerrainId(int x, int y)
    {
        return $"terrain.tileset.a2.{x}.{y}";
    }

    private void UpdateA2Highlight()
    {
        if (_selectedTerrain is not null && RpgMakerAutoTile.IsA1(_selectedTerrain))
        {
            _tilesetPalette.HighlightBlockOrigin = new Point(_selectedTerrain.TileX, _selectedTerrain.TileY);
            _tilesetPalette.HighlightBlockSize = new Size(6, 3);
            return;
        }

        if (_selectedTerrain is not null && RpgMakerAutoTile.IsA2(_selectedTerrain))
        {
            _tilesetPalette.HighlightBlockOrigin = new Point(_selectedTerrain.TileX, _selectedTerrain.TileY);
            _tilesetPalette.HighlightBlockSize = new Size(2, 3);
            return;
        }

        _tilesetPalette.HighlightBlockOrigin = new Point(-1, -1);
        _tilesetPalette.HighlightBlockSize = Size.Empty;
    }

    private void UpdateBrushButtons()
    {
        var useTileset = _canvas.BrushSource == MapBrushSource.Tileset;
        _useTerrainBrushButton.BackColor = useTileset ? SystemColors.Control : Color.FromArgb(215, 232, 255);
        _useTilesetBrushButton.BackColor = useTileset ? Color.FromArgb(215, 232, 255) : SystemColors.Control;
        _useTilesetBrushButton.Enabled = _selectedTilesetTile.X >= 0 && _selectedTilesetTile.Y >= 0;
        _bindA2PaletteButton.Enabled = _selectedTerrain is not null && _selectedTilesetTile.X >= 0 && _selectedTilesetTile.Y >= 0;
    }

    private void UpdateTilesetButtons()
    {
        _planTilesetButton.Enabled = _selectedMap is not null
            && !string.IsNullOrWhiteSpace(_selectedMap.TilesetImagePath)
            && File.Exists(_selectedMap.TilesetImagePath);
    }

    private static string TerrainButtonText(MapTerrainDefinition terrain)
    {
        if (terrain.Rule.Equals(MapDefaults.RuleRpgMakerA2, StringComparison.OrdinalIgnoreCase))
        {
            return $"{terrain.DisplayName}\r\nA2自动";
        }

        if (terrain.Rule.Equals(MapDefaults.RuleRpgMakerA1, StringComparison.OrdinalIgnoreCase))
        {
            return $"{terrain.DisplayName}\r\nA1动态";
        }

        return terrain.Animated ? $"{terrain.DisplayName}\r\n动态" : terrain.DisplayName;
    }

    private static string LayerKindLabel(string kind)
    {
        return kind switch
        {
            "Collision" => "碰撞",
            "Region" => "区域",
            _ => "瓦片"
        };
    }

    private string T(string key, string fallback)
    {
        var value = _localization.T(key);
        return value == key ? fallback : value;
    }

    private static string UniqueId(string prefix, IEnumerable<string> existingIds)
    {
        var existing = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(prefix))
        {
            return prefix;
        }

        for (var index = 1; index < 10_000; index++)
        {
            var candidate = string.Create(CultureInfo.InvariantCulture, $"{prefix}.{index:000}");
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{prefix}.{Guid.NewGuid():N}";
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = value
            .Trim()
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray();
        var name = new string(chars).Trim('.', ' ');
        return string.IsNullOrWhiteSpace(name) ? "tileset" : name;
    }

    private static string UniqueFilePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        var directory = Path.GetDirectoryName(path) ?? "";
        var name = Path.GetFileNameWithoutExtension(path);
        var extension = Path.GetExtension(path);
        for (var index = 1; index < 10_000; index++)
        {
            var candidate = Path.Combine(directory, $"{name}.{index:000}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(directory, $"{name}.{Guid.NewGuid():N}{extension}");
    }
}
