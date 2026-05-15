using Axe2DEditor.Core.Maps;
using Axe2DEditor.Editor.Controls;

namespace Axe2DEditor.Editor.Modules;

public sealed class TilesetPlannerDialog : Form
{
    private const int SideRowTitleHeight = 30;
    private const int SideRowPlanNormalHeight = 32;
    private const int SideRowPlanRpgHeight = 100;
    private const int SideRowPlanRpgA1Height = 132;
    private const int SideRowModeHeight = 30;
    private const int SideRowSelectionHeight = 24;
    private const int SideRowActionHeight = 34;
    private const int SideRowRegionListHeight = 124;
    private const int SideRowDeleteHeight = 36;
    private const int SideRowAnimationHeight = 224;
    private const int SideRowButtonsHeight = 44;
    private const int AnimationToggleHeight = 30;
    private const int AnimationDurationHeight = 28;
    private const int AnimationButtonsHeight = 30;
    private const int AnimationFrameListHeight = 84;
    private const int AnimationPreviewHeight = 50;

    private readonly TilesetPlannerCanvas _canvas = new();
    private readonly ListView _regionList = new();
    private readonly RadioButton _normalButton = new();
    private readonly RadioButton _ignoredButton = new();
    private readonly RadioButton _hiddenButton = new();
    private readonly ComboBox _modeCombo = new();
    private readonly ComboBox _rpgKindCombo = new();
    private readonly ComboBox _a1VariantCombo = new();
    private readonly CheckBox _animatedCheckBox = new();
    private readonly NumericUpDown _frameDurationBox = new();
    private readonly Label _frameDurationLabel = new();
    private readonly ListView _animationFrameList = new();
    private readonly Button _addAnimationFrameButton = new();
    private readonly Button _editAnimationFrameButton = new();
    private readonly Button _deleteAnimationFrameButton = new();
    private readonly Button _moveAnimationFrameUpButton = new();
    private readonly Button _moveAnimationFrameDownButton = new();
    private readonly PictureBox _animationPreviewBox = new();
    private readonly System.Windows.Forms.Timer _animationPreviewTimer = new() { Interval = 80 };
    private readonly Button _autoRpgButton = new();
    private readonly Button _clearA2Button = new();
    private readonly Button _addButton = new();
    private readonly Button _editTileButton = new();
    private readonly Button _deleteButton = new();
    private readonly Button _okButton = new();
    private readonly Button _cancelButton = new();
    private readonly Label _selectionLabel = new();
    private readonly Label _hintLabel = new();
    private readonly ToolTip _toolTip = new();
    private readonly ListView _wangSetList = new();
    private readonly ListView _wangColorList = new();
    private readonly RadioButton _mixedTypeButton = new();
    private readonly RadioButton _cornerTypeButton = new();
    private readonly RadioButton _edgeTypeButton = new();
    private readonly CheckBox _allowRotateBox = new();
    private readonly CheckBox _allowFlipHBox = new();
    private readonly CheckBox _allowFlipVBox = new();
    private readonly CheckBox _preferOriginalBox = new();
    private readonly Button _addWangSetButton = new();
    private readonly Button _deleteWangSetButton = new();
    private readonly Button _addWangColorButton = new();
    private readonly Button _deleteWangColorButton = new();
    private readonly Button _setWangRepresentativeButton = new();
    private readonly Button _setWangColorRepresentativeButton = new();
    private readonly Button _clearWangTileButton = new();
    private readonly Button _checkWangPatternButton = new();
    private readonly Image _image;
    private readonly int _tileSize;
    private readonly List<TilesetRegionDefinition> _regions;
    private readonly List<TilesetTileMetadataDefinition> _tiles;
    private readonly TilesetAdvancedPlanDefinition _advanced;
    private TableLayoutPanel? _planPanel;
    private TableLayoutPanel? _sidePanel;
    private FlowLayoutPanel? _modePanel;
    private TableLayoutPanel? _selectionActionPanel;
    private TableLayoutPanel? _advancedPanel;
    private string _mode;
    private string _rpgKind;
    private TilesetRegionDefinition? _selectedRegion;
    private TilesetWangSetDefinition? _selectedWangSet;
    private TilesetWangColorDefinition? _selectedWangColor;
    private bool _suppressAdvancedListEvents;

    public TilesetPlannerDialog(Image image, int tileSize, TilesetPlanDefinition? existingPlan)
    {
        _image = new Bitmap(image);
        _tileSize = Math.Max(8, tileSize);
        _regions = CloneRegions(existingPlan?.Regions ?? []);
        _tiles = CloneTiles(existingPlan?.Tiles ?? []);
        _advanced = CloneAdvanced(existingPlan?.Advanced);
        _mode = existingPlan?.Mode ?? TilesetPlanModes.Normal;
        _rpgKind = existingPlan?.RpgMakerKind ?? RpgMakerTilesetKinds.A2;

        Text = "图集规划";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = true;
        MinimumSize = new Size(1280, 820);
        Size = new Size(1500, 900);
        Font = new Font("Microsoft YaHei UI", 9F);

        BuildUi();
        RefreshRegionList();
        UpdateSelectionText(Rectangle.Empty);
        _animationPreviewTimer.Tick += (_, _) =>
        {
            if (_animationPreviewBox.Visible && CanPreviewAnimation())
            {
                _animationPreviewBox.Invalidate();
            }
        };
        _animationPreviewTimer.Start();
    }

    public TilesetPlanDefinition Plan => new()
    {
        TileSize = _tileSize,
        Mode = _mode,
        RpgMakerKind = _rpgKind,
        RpgMakerLayout = TilesetPlanModes.RpgMaker.Equals(_mode, StringComparison.OrdinalIgnoreCase) ? RpgMakerTilesetLayouts.Standard : RpgMakerTilesetLayouts.Standard,
        Regions = CloneRegions(_regions),
        Tiles = CloneTiles(_tiles),
        Advanced = CloneAdvanced(_advanced)
    };

    public static bool TryEditTileMetadata(
        IWin32Window owner,
        Image image,
        int tileSize,
        TilesetTileMetadataDefinition tile,
        Font? font = null)
    {
        var editor = new TileMetadataDialogEditor(image, Math.Max(8, tileSize), font ?? Control.DefaultFont, owner);
        return editor.ShowTileMetadataDialog(tile);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationPreviewTimer.Dispose();
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
        _canvas.SetTileMetadata(_tiles);
        _canvas.SetAdvancedPlan(_advanced);
        _canvas.SelectionChanged += (_, e) =>
        {
            UpdateSelectionText(e.Selection);
            UpdateAnimationFrameButtons();
        };
        _canvas.WangTileEdited += (_, _) =>
        {
            RefreshSelectedWangTileState();
            _canvas.Invalidate();
        };
        _canvas.TileDoubleClicked += (_, e) => EditTileMetadata(e.TileX, e.TileY);
        _canvas.WangTileDoubleClicked += (_, e) => EditSelectedWangTile(e.TileX, e.TileY);
        root.Panel1.Controls.Add(_canvas);

        _sidePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 1,
            AutoScroll = true,
            RowCount = 12
        };
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, SideRowTitleHeight));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, SideRowPlanRpgA1Height));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, SideRowModeHeight));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, SideRowSelectionHeight));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, SideRowActionHeight));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, SideRowDeleteHeight));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, SideRowButtonsHeight));
        _sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

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
        _addButton.Dock = DockStyle.Fill;
        _addButton.Height = 32;
        _addButton.Margin = new Padding(0, 2, 7, 2);
        _addButton.Click += (_, _) => AddSelection();
        _editTileButton.Text = "编辑瓦片";
        _editTileButton.Dock = DockStyle.Fill;
        _editTileButton.Height = 32;
        _editTileButton.Margin = new Padding(7, 2, 0, 2);
        _editTileButton.Click += (_, _) => EditSelectedTileMetadata();

        _selectionActionPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        _selectionActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _selectionActionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _selectionActionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _selectionActionPanel.Controls.Add(_addButton, 0, 0);
        _selectionActionPanel.Controls.Add(_editTileButton, 1, 0);

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
            _selectedRegion = region;
            _canvas.SelectedRegion = region;
            _deleteButton.Enabled = region is not null;
            SyncAnimationControls();
            RefreshAnimationFrameList();
        };

        _deleteButton.Text = "删除区域";
        _deleteButton.Dock = DockStyle.Left;
        _deleteButton.Width = 108;
        _deleteButton.Height = 32;
        _deleteButton.Margin = new Padding(0, 2, 0, 2);
        _deleteButton.Enabled = false;
        _deleteButton.Click += (_, _) => DeleteSelectedRegion();

        _hintLabel.Dock = DockStyle.Fill;
        _hintLabel.TextAlign = ContentAlignment.MiddleLeft;
        _hintLabel.ForeColor = SystemColors.GrayText;
        _hintLabel.AutoSize = false;
        _hintLabel.Text = string.Empty;
        _hintLabel.Visible = false;

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0),
            Margin = Padding.Empty
        };
        _okButton.Text = "确定";
        _okButton.Width = 92;
        _okButton.Height = 34;
        _okButton.DialogResult = DialogResult.OK;
        _cancelButton.Text = "取消";
        _cancelButton.Width = 92;
        _cancelButton.Height = 34;
        _cancelButton.DialogResult = DialogResult.Cancel;
        buttonPanel.Controls.Add(_okButton);
        buttonPanel.Controls.Add(_cancelButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        _sidePanel.Controls.Add(title, 0, 0);
        _sidePanel.Controls.Add(planPanel, 0, 1);
        _sidePanel.Controls.Add(_modePanel, 0, 2);
        _sidePanel.Controls.Add(_selectionLabel, 0, 3);
        _sidePanel.Controls.Add(_selectionActionPanel, 0, 4);
        _sidePanel.Controls.Add(_regionList, 0, 5);
        _sidePanel.Controls.Add(_deleteButton, 0, 6);
        _sidePanel.Controls.Add(_hintLabel, 0, 7);
        _sidePanel.Controls.Add(BuildAnimationPanel(), 0, 8);
        _advancedPanel = BuildAdvancedPanel();
        _sidePanel.Controls.Add(_advancedPanel, 0, 9);
        _sidePanel.Controls.Add(buttonPanel, 0, 10);
        root.Panel2.Controls.Add(_sidePanel);
        Controls.Add(root);

        root.Resize += (_, _) => SplitContainerLayout.ClampCurrentSafe(root, 560, 500);
        Shown += (_, _) => BeginInvoke(new Action(() => SplitContainerLayout.ApplySafe(root, 980, 560, 500)));
        UpdatePlanControls();
        RefreshAdvancedLists();
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
        ConfigureModeButton(_hiddenButton, "隐藏瓦片");

        _normalButton.Margin = new Padding(0, 0, 12, 6);
        _ignoredButton.Margin = new Padding(0, 0, 12, 6);
        _hiddenButton.Margin = new Padding(0, 0, 0, 6);

        panel.Controls.Add(_normalButton);
        panel.Controls.Add(_ignoredButton);
        panel.Controls.Add(_hiddenButton);
        return panel;
    }

    private Control BuildAnimationPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Margin = Padding.Empty,
            Padding = new Padding(0, 6, 0, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, AnimationToggleHeight));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, AnimationDurationHeight));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, AnimationButtonsHeight));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, AnimationPreviewHeight));

        _animatedCheckBox.Text = "动态瓦片";
        _animatedCheckBox.AutoSize = false;
        _animatedCheckBox.Dock = DockStyle.Fill;
        _animatedCheckBox.Margin = Padding.Empty;
        _animatedCheckBox.TextAlign = ContentAlignment.MiddleLeft;
        _animatedCheckBox.CheckedChanged += (_, _) =>
        {
            if (_selectedRegion is not null)
            {
                _selectedRegion.Animated = _animatedCheckBox.Checked;
                if (_selectedRegion.Animated && _selectedRegion.AnimationFrames.Count <= 0)
                {
                    _selectedRegion.AnimationFrames.Add(new TilesetFrameDefinition
                    {
                        TileX = _selectedRegion.X,
                        TileY = _selectedRegion.Y,
                        DurationMs = _selectedRegion.AnimationFrameDurationMs
                    });
                }

                UpdatePlanControls();
                RefreshAnimationFrameList();
                _canvas.Invalidate();
            }
        };

        _frameDurationLabel.Text = "帧时长";
        _frameDurationLabel.TextAlign = ContentAlignment.MiddleLeft;
        _frameDurationLabel.Dock = DockStyle.Fill;

        _frameDurationBox.Minimum = 16;
        _frameDurationBox.Maximum = 2000;
        _frameDurationBox.Increment = 10;
        _frameDurationBox.Value = 100;
        _frameDurationBox.Dock = DockStyle.Left;
        _frameDurationBox.Width = 100;
        _frameDurationBox.Margin = new Padding(0, 1, 0, 1);
        _frameDurationBox.ValueChanged += (_, _) =>
        {
            if (_selectedRegion is not null)
            {
                _selectedRegion.AnimationFrameDurationMs = (int)_frameDurationBox.Value;
                foreach (var frame in _selectedRegion.AnimationFrames.Where(frame => frame.DurationMs <= 0))
                {
                    frame.DurationMs = _selectedRegion.AnimationFrameDurationMs;
                }

                UpdatePlanControls();
                RefreshAnimationFrameList();
                _canvas.Invalidate();
            }
        };

        var frameLabel = new Label
        {
            Dock = DockStyle.Fill,
            Text = "动画帧",
            TextAlign = ContentAlignment.MiddleLeft
        };

        var frameButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        for (var index = 0; index < 5; index++)
        {
            frameButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        }

        frameButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        ConfigureSmallButton(_addAnimationFrameButton, "+");
        ConfigureSmallButton(_editAnimationFrameButton, "✎");
        ConfigureSmallButton(_deleteAnimationFrameButton, "×");
        ConfigureSmallButton(_moveAnimationFrameUpButton, "↑");
        ConfigureSmallButton(_moveAnimationFrameDownButton, "↓");
        ConfigureAnimationIconButton(_addAnimationFrameButton, "添加选区为帧");
        ConfigureAnimationIconButton(_editAnimationFrameButton, "编辑帧");
        ConfigureAnimationIconButton(_deleteAnimationFrameButton, "删除帧");
        ConfigureAnimationIconButton(_moveAnimationFrameUpButton, "上移");
        ConfigureAnimationIconButton(_moveAnimationFrameDownButton, "下移");
        _toolTip.SetToolTip(_animatedCheckBox, "开启后当前普通区域可配置为动画瓦片。");
        _toolTip.SetToolTip(_frameDurationBox, "默认帧时长。若帧列表里某一帧未单独设置时长，就使用这里的值。");
        _toolTip.SetToolTip(_animationFrameList, "手动维护动画帧列表。未配置帧列表时，会按区域矩形从左到右、从上到下自动补齐。");
        _toolTip.SetToolTip(_animationPreviewBox, "底部显示当前动画预览。点击可打开更大的辅助预览窗口。");
        _addAnimationFrameButton.Click += (_, _) => AddAnimationFrameFromSelection();
        _editAnimationFrameButton.Click += (_, _) => EditSelectedAnimationFrame();
        _deleteAnimationFrameButton.Click += (_, _) => DeleteSelectedAnimationFrame();
        _moveAnimationFrameUpButton.Click += (_, _) => MoveSelectedAnimationFrame(-1);
        _moveAnimationFrameDownButton.Click += (_, _) => MoveSelectedAnimationFrame(1);
        frameButtons.Controls.Add(_addAnimationFrameButton, 0, 0);
        frameButtons.Controls.Add(_editAnimationFrameButton, 1, 0);
        frameButtons.Controls.Add(_deleteAnimationFrameButton, 2, 0);
        frameButtons.Controls.Add(_moveAnimationFrameUpButton, 3, 0);
        frameButtons.Controls.Add(_moveAnimationFrameDownButton, 4, 0);

        _animationFrameList.Dock = DockStyle.Fill;
        _animationFrameList.View = View.Details;
        _animationFrameList.FullRowSelect = true;
        _animationFrameList.HideSelection = false;
        _animationFrameList.MultiSelect = false;
        _animationFrameList.Columns.Add("序号", 46);
        _animationFrameList.Columns.Add("瓦片", 76);
        _animationFrameList.Columns.Add("时长", 76);
        _animationFrameList.SelectedIndexChanged += (_, _) => UpdateAnimationFrameButtons();
        _animationFrameList.DoubleClick += (_, _) => EditSelectedAnimationFrame();
        _animationPreviewBox.Dock = DockStyle.Fill;
        _animationPreviewBox.BorderStyle = BorderStyle.FixedSingle;
        _animationPreviewBox.BackColor = SystemColors.ControlDark;
        _animationPreviewBox.Cursor = Cursors.Hand;
        _animationPreviewBox.Paint += DrawAnimationPreview;
        _animationPreviewBox.Click += (_, _) => ShowAnimationPreviewWindow();

        panel.Controls.Add(_animatedCheckBox, 0, 0);
        panel.SetColumnSpan(_animatedCheckBox, 2);
        panel.Controls.Add(_frameDurationLabel, 0, 1);
        panel.Controls.Add(_frameDurationBox, 1, 1);
        panel.Controls.Add(frameLabel, 0, 2);
        panel.Controls.Add(frameButtons, 1, 2);
        panel.Controls.Add(_animationFrameList, 0, 3);
        panel.SetColumnSpan(_animationFrameList, 2);
        panel.Controls.Add(_animationPreviewBox, 0, 4);
        panel.SetColumnSpan(_animationPreviewBox, 2);
        return panel;
    }

    private TableLayoutPanel BuildAdvancedPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var options = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        options.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        options.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        ConfigureAdvancedCheckBox(_allowRotateBox, "允许旋转", _advanced.AllowRotate);
        ConfigureAdvancedCheckBox(_allowFlipHBox, "允许水平翻转", _advanced.AllowFlipHorizontally);
        ConfigureAdvancedCheckBox(_allowFlipVBox, "允许垂直翻转", _advanced.AllowFlipVertically);
        ConfigureAdvancedCheckBox(_preferOriginalBox, "优先原图块", _advanced.PreferUntransformedTiles);
        _allowRotateBox.CheckedChanged += (_, _) => _advanced.AllowRotate = _allowRotateBox.Checked;
        _allowFlipHBox.CheckedChanged += (_, _) => _advanced.AllowFlipHorizontally = _allowFlipHBox.Checked;
        _allowFlipVBox.CheckedChanged += (_, _) => _advanced.AllowFlipVertically = _allowFlipVBox.Checked;
        _preferOriginalBox.CheckedChanged += (_, _) => _advanced.PreferUntransformedTiles = _preferOriginalBox.Checked;
        options.Controls.Add(_allowRotateBox, 0, 0);
        options.Controls.Add(_allowFlipHBox, 1, 0);
        options.Controls.Add(_allowFlipVBox, 0, 1);
        options.Controls.Add(_preferOriginalBox, 1, 1);

        var setButtons = BuildThreeButtonRow(_addWangSetButton, _deleteWangSetButton, _setWangRepresentativeButton);
        ConfigureAdvancedButton(_addWangSetButton, "添加集合");
        ConfigureAdvancedButton(_deleteWangSetButton, "删除集合");
        ConfigureAdvancedButton(_setWangRepresentativeButton, "设为代表");
        _addWangSetButton.Click += (_, _) => AddWangSet();
        _deleteWangSetButton.Click += (_, _) => DeleteWangSet();
        _setWangRepresentativeButton.Click += (_, _) => SetSelectedWangSetRepresentative();

        _wangSetList.Dock = DockStyle.Fill;
        _wangSetList.View = View.Details;
        _wangSetList.FullRowSelect = true;
        _wangSetList.HideSelection = false;
        _wangSetList.MultiSelect = false;
        _wangSetList.Columns.Add("集合", 116);
        _wangSetList.Columns.Add("类型", 72);
        _wangSetList.Columns.Add("代表", 70);
        _wangSetList.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressAdvancedListEvents)
            {
                return;
            }

            var selectedSet = SelectedTag<TilesetWangSetDefinition>(_wangSetList);
            if (selectedSet is null)
            {
                RestoreWangSetSelection();
                return;
            }

            _selectedWangSet = selectedSet;
            _selectedWangColor = _selectedWangSet?.Colors.FirstOrDefault();
            RefreshAdvancedLists(keepSetSelection: true);
        };
        _wangSetList.DoubleClick += (_, _) =>
        {
            if (SelectedTag<TilesetWangSetDefinition>(_wangSetList) is TilesetWangSetDefinition set)
            {
                EditWangSet(set);
            }
        };

        panel.Controls.Add(BuildWangTypeSelector(), 0, 3);

        var colorButtons = BuildThreeButtonRow(_addWangColorButton, _deleteWangColorButton, _setWangColorRepresentativeButton);
        ConfigureAdvancedButton(_addWangColorButton, "添加标签");
        ConfigureAdvancedButton(_deleteWangColorButton, "删除标签");
        ConfigureAdvancedButton(_setWangColorRepresentativeButton, "设为代表");
        _addWangColorButton.Click += (_, _) => AddWangColor();
        _deleteWangColorButton.Click += (_, _) => DeleteWangColor();
        _setWangColorRepresentativeButton.Click += (_, _) => SetSelectedWangColorRepresentative();

        _wangColorList.Dock = DockStyle.Fill;
        _wangColorList.View = View.Details;
        _wangColorList.FullRowSelect = true;
        _wangColorList.HideSelection = false;
        _wangColorList.MultiSelect = false;
        _wangColorList.OwnerDraw = true;
        _wangColorList.DrawColumnHeader += (_, e) => e.DrawDefault = true;
        _wangColorList.DrawSubItem += DrawWangColorListSubItem;
        _wangColorList.Columns.Add("地形标签", 94);
        _wangColorList.Columns.Add("颜色", 52);
        _wangColorList.Columns.Add("索引", 48);
        _wangColorList.Columns.Add("概率", 52);
        _wangColorList.Columns.Add("代表", 66);
        _wangColorList.SelectedIndexChanged += (_, _) =>
        {
            if (_suppressAdvancedListEvents)
            {
                return;
            }

            var selectedColor = SelectedTag<TilesetWangColorDefinition>(_wangColorList);
            if (selectedColor is null)
            {
                RestoreWangColorSelection();
                return;
            }

            _selectedWangColor = selectedColor;
            _canvas.ActiveWangColor = _selectedWangColor;
        };
        _wangColorList.DoubleClick += (_, _) =>
        {
            if (SelectedTag<TilesetWangColorDefinition>(_wangColorList) is TilesetWangColorDefinition color)
            {
                EditWangColor(color);
            }
        };

        var tileButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        tileButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        tileButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        ConfigureAdvancedButton(_clearWangTileButton, "清空当前瓦片");
        ConfigureAdvancedButton(_checkWangPatternButton, "检查图案");
        _clearWangTileButton.Click += (_, _) => ClearSelectedWangTile();
        _checkWangPatternButton.Click += (_, _) => ShowWangPatternsDialog();
        tileButtons.Controls.Add(_clearWangTileButton, 0, 0);
        tileButtons.Controls.Add(_checkWangPatternButton, 1, 0);

        panel.Controls.Add(options, 0, 0);
        panel.Controls.Add(setButtons, 0, 1);
        panel.Controls.Add(_wangSetList, 0, 2);
        panel.Controls.Add(colorButtons, 0, 4);
        panel.Controls.Add(_wangColorList, 0, 5);
        panel.Controls.Add(tileButtons, 0, 6);

        _toolTip.SetToolTip(_wangSetList, "管理 Tiled 风格 Wang 集合。");
        _toolTip.SetToolTip(_wangColorList, "管理当前集合内的地形标签。");
        _toolTip.SetToolTip(_clearWangTileButton, "清空当前选中图块上的 Wang 标记。");
        _toolTip.SetToolTip(_checkWangPatternButton, "查看当前集合已覆盖的边/角组合。");
        _toolTip.SetToolTip(panel, "在图集上点击或拖过边、角热区，直接标记当前集合的 Wang 图案。");
        return panel;
    }

    private Control BuildWangTypeSelector()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));

        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = "匹配类型",
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };

        ConfigureWangTypeButton(_mixedTypeButton, "混合", TilesetWangSetTypes.Mixed);
        ConfigureWangTypeButton(_edgeTypeButton, "边", TilesetWangSetTypes.Edge);
        ConfigureWangTypeButton(_cornerTypeButton, "角", TilesetWangSetTypes.Corner);

        panel.Controls.Add(label, 0, 0);
        panel.Controls.Add(_mixedTypeButton, 1, 0);
        panel.Controls.Add(_edgeTypeButton, 2, 0);
        panel.Controls.Add(_cornerTypeButton, 3, 0);
        return panel;
    }

    private static TableLayoutPanel BuildThreeButtonRow(Button first, Button second, Button third)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        panel.Controls.Add(first, 0, 0);
        panel.Controls.Add(second, 1, 0);
        panel.Controls.Add(third, 2, 0);
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

    private static void ConfigureAdvancedCheckBox(CheckBox checkBox, string text, bool isChecked)
    {
        checkBox.Text = text;
        checkBox.Checked = isChecked;
        checkBox.AutoSize = true;
        checkBox.Dock = DockStyle.None;
        checkBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        checkBox.Margin = new Padding(0, 4, 8, 0);
        checkBox.TextAlign = ContentAlignment.MiddleLeft;
    }

    private void ConfigureWangTypeButton(RadioButton button, string text, string type)
    {
        button.Text = text;
        button.Tag = type;
        button.AutoSize = false;
        button.Dock = DockStyle.Fill;
        button.Margin = Padding.Empty;
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.CheckAlign = ContentAlignment.MiddleLeft;
        button.CheckedChanged += (_, _) =>
        {
            if (_suppressAdvancedListEvents || _selectedWangSet is null || !button.Checked)
            {
                return;
            }

            _selectedWangSet.Type = type;
            RefreshAdvancedLists(keepSetSelection: true);
        };
    }

    private static void ConfigureAdvancedButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Height = 32;
        button.Margin = new Padding(0, 2, 6, 2);
        button.UseVisualStyleBackColor = true;
    }

    private static void ConfigureSmallButton(Button button, string text)
    {
        button.Text = text;
        button.Dock = DockStyle.Fill;
        button.Height = 30;
        button.Margin = new Padding(0, 1, 6, 1);
        button.UseVisualStyleBackColor = true;
    }

    private static void AddPlanRow(TableLayoutPanel panel, int row, string label, Control editor)
    {
        panel.Controls.Add(new Label { Dock = DockStyle.Fill, Text = label, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        panel.Controls.Add(editor, 1, row);
    }

    private void UpdatePlanControls()
    {
        var isRpgMaker = string.Equals(_mode, TilesetPlanModes.RpgMaker, StringComparison.OrdinalIgnoreCase);
        var isAdvanced = string.Equals(_mode, TilesetPlanModes.Advanced, StringComparison.OrdinalIgnoreCase);
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
        _canvas.WangEditEnabled = isAdvanced;
        _canvas.ActiveWangSet = isAdvanced ? _selectedWangSet : null;
        _canvas.ActiveWangColor = isAdvanced ? _selectedWangColor : null;
        SetSideRowHeight(1, isAdvanced ? SideRowPlanNormalHeight : isA1Kind ? SideRowPlanRpgA1Height : isRpgMaker ? SideRowPlanRpgHeight : SideRowPlanNormalHeight);
        SetSideRowHeight(2, isAdvanced ? 0 : SideRowModeHeight);
        SetSideRowHeight(3, isAdvanced ? 0 : SideRowSelectionHeight);
        SetSideRowHeight(4, isAdvanced ? 0 : SideRowActionHeight);
        SetSideRowWeighted(5, !isAdvanced, 45);
        SetSideRowHeight(6, isAdvanced ? 0 : SideRowDeleteHeight);
        SetSideRowHeight(7, 0);
        SetSideRowWeighted(8, !isAdvanced, 55);
        SetSideRowPercent(9, isAdvanced);
        SetSideRowHeight(10, SideRowButtonsHeight);
        if (_modePanel is not null)
        {
            _modePanel.Visible = !isAdvanced;
        }

        _selectionLabel.Visible = !isAdvanced;
        if (_selectionActionPanel is not null)
        {
            _selectionActionPanel.Visible = !isAdvanced;
        }

        _addButton.Visible = !isAdvanced;
        _editTileButton.Visible = !isAdvanced;
        _regionList.Visible = !isAdvanced;
        _deleteButton.Visible = !isAdvanced;
        _hintLabel.Visible = false;
        if (_advancedPanel is not null)
        {
            _advancedPanel.Visible = isAdvanced;
        }

        _hintLabel.Text = string.Empty;
        _toolTip.SetToolTip(_hintLabel, isRpgMaker && isA1Kind
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
                : isAdvanced
                    ? "高级模式: 使用 Tiled Terrain Set / Wang Set 方式，在图集格子的边、角热区绘制自动地形标记。"
                    : "普通模式: 直接按单格图块绘制；需要自动边缘时可切到 RPG Maker 或高级模式。");
        RefreshAdvancedLists(keepSetSelection: true);
        UpdateAnimationFrameButtons();
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

    private void SetSideRowAuto(int row, bool visible)
    {
        if (_sidePanel is null || row < 0 || row >= _sidePanel.RowStyles.Count)
        {
            return;
        }

        _sidePanel.RowStyles[row].SizeType = visible ? SizeType.AutoSize : SizeType.Absolute;
        _sidePanel.RowStyles[row].Height = visible ? 0 : 0;
    }

    private void SetSideRowPercent(int row, bool visible)
    {
        if (_sidePanel is null || row < 0 || row >= _sidePanel.RowStyles.Count)
        {
            return;
        }

        _sidePanel.RowStyles[row].SizeType = visible ? SizeType.Percent : SizeType.Absolute;
        _sidePanel.RowStyles[row].Height = visible ? 100 : 0;
    }

    private void SetSideRowWeighted(int row, bool visible, float weight)
    {
        if (_sidePanel is null || row < 0 || row >= _sidePanel.RowStyles.Count)
        {
            return;
        }

        _sidePanel.RowStyles[row].SizeType = visible ? SizeType.Percent : SizeType.Absolute;
        _sidePanel.RowStyles[row].Height = visible ? weight : 0;
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
        _selectedRegion = region;
        RefreshRegionList(region);
        _canvas.Invalidate();
        UpdatePlanControls();
        SyncAnimationControls();
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
        _selectedRegion = null;
        _canvas.Invalidate();
        UpdatePlanControls();
        SyncAnimationControls();
    }

    private void AddWangSet()
    {
        var nextIndex = _advanced.WangSets.Count + 1;
        var set = new TilesetWangSetDefinition
        {
            Id = $"wangset.{Guid.NewGuid():N}",
            Name = $"地形集合 {nextIndex}",
            Type = TilesetWangSetTypes.Mixed,
            Colors =
            [
                new TilesetWangColorDefinition
                {
                    Index = 1,
                    Name = "地形",
                    ColorHex = DefaultWangColor(1)
                }
            ]
        };
        _advanced.WangSets.Add(set);
        _selectedWangSet = set;
        _selectedWangColor = set.Colors[0];
        RefreshAdvancedLists();
    }

    private void DeleteWangSet()
    {
        if (_selectedWangSet is null)
        {
            return;
        }

        _advanced.WangSets.Remove(_selectedWangSet);
        _selectedWangSet = _advanced.WangSets.FirstOrDefault();
        _selectedWangColor = _selectedWangSet?.Colors.FirstOrDefault();
        RefreshAdvancedLists();
    }

    private void EditWangSet(TilesetWangSetDefinition set)
    {
        if (!_advanced.WangSets.Contains(set) || !TryEditWangSet(set, out var name, out var type, out var tileX, out var tileY))
        {
            return;
        }

        set.Name = name;
        set.Type = type;
        set.TileX = tileX;
        set.TileY = tileY;
        _selectedWangSet = set;
        RefreshAdvancedLists();
    }

    private void AddWangColor()
    {
        if (_selectedWangSet is null)
        {
            return;
        }

        var used = _selectedWangSet.Colors.Select(v => v.Index).ToHashSet();
        var nextIndex = 1;
        while (used.Contains(nextIndex))
        {
            nextIndex++;
        }

        var color = new TilesetWangColorDefinition
        {
            Index = nextIndex,
            Name = $"地形 {nextIndex}",
            ColorHex = DefaultWangColor(nextIndex)
        };
        _selectedWangSet.Colors.Add(color);
        _selectedWangColor = color;
        RefreshAdvancedLists(keepSetSelection: true);
    }

    private void DeleteWangColor()
    {
        if (_selectedWangSet is null || _selectedWangColor is null || _selectedWangSet.Colors.Count <= 1)
        {
            return;
        }

        var removedIndex = _selectedWangColor.Index;
        _selectedWangSet.Colors.Remove(_selectedWangColor);
        foreach (var tile in _selectedWangSet.Tiles.ToList())
        {
            for (var index = 0; index < tile.WangId.Count; index++)
            {
                if (tile.WangId[index] == removedIndex)
                {
                    tile.WangId[index] = 0;
                }
            }

            if (tile.WangId.All(v => v == 0))
            {
                _selectedWangSet.Tiles.Remove(tile);
            }
        }

        _selectedWangColor = _selectedWangSet.Colors.FirstOrDefault();
        RefreshAdvancedLists(keepSetSelection: true);
    }

    private void EditWangColor(TilesetWangColorDefinition color)
    {
        if (_selectedWangSet is null || !_selectedWangSet.Colors.Contains(color))
        {
            return;
        }

        if (!TryEditWangColor(_selectedWangSet, color, out var name, out var index, out var colorHex, out var probability, out var tileX, out var tileY))
        {
            return;
        }

        var oldIndex = color.Index;
        color.Name = name;
        color.Index = index;
        color.ColorHex = colorHex;
        color.Probability = probability;
        color.TileX = tileX;
        color.TileY = tileY;
        if (oldIndex != index)
        {
            foreach (var tile in _selectedWangSet.Tiles)
            {
                for (var i = 0; i < tile.WangId.Count; i++)
                {
                    if (tile.WangId[i] == oldIndex)
                    {
                        tile.WangId[i] = index;
                    }
                }
            }
        }

        _selectedWangColor = color;
        RefreshAdvancedLists(keepSetSelection: true);
    }

    private void SetSelectedWangSetRepresentative()
    {
        if (_selectedWangSet is null || _canvas.Selection.Width <= 0 || _canvas.Selection.Height <= 0)
        {
            return;
        }

        _selectedWangSet.TileX = _canvas.Selection.X;
        _selectedWangSet.TileY = _canvas.Selection.Y;
        RefreshAdvancedLists(keepSetSelection: true);
    }

    private void SetSelectedWangColorRepresentative()
    {
        if (_selectedWangColor is null || _canvas.Selection.Width <= 0 || _canvas.Selection.Height <= 0)
        {
            return;
        }

        _selectedWangColor.TileX = _canvas.Selection.X;
        _selectedWangColor.TileY = _canvas.Selection.Y;
        RefreshAdvancedLists(keepSetSelection: true);
    }

    private void ClearSelectedWangTile()
    {
        if (_selectedWangSet is null || _canvas.Selection.Width <= 0 || _canvas.Selection.Height <= 0)
        {
            return;
        }

        var selection = _canvas.Selection;
        _selectedWangSet.Tiles.RemoveAll(v =>
            v.TileX >= selection.Left && v.TileX < selection.Right
            && v.TileY >= selection.Top && v.TileY < selection.Bottom);
        RefreshSelectedWangTileState();
        _canvas.Invalidate();
    }

    private void EditSelectedWangTile(int tileX, int tileY)
    {
        if (_selectedWangSet is null)
        {
            return;
        }

        var tile = _selectedWangSet.Tiles.FirstOrDefault(entry => entry.TileX == tileX && entry.TileY == tileY);
        if (tile is null)
        {
            return;
        }

        if (!TryEditWangTile(tile, out var probability))
        {
            return;
        }

        tile.Probability = probability;
        RefreshSelectedWangTileState();
    }

    private void RefreshAdvancedLists(bool keepSetSelection = false)
    {
        if (_advancedPanel is null)
        {
            return;
        }

        _suppressAdvancedListEvents = true;
        try
        {
            if (!keepSetSelection && _selectedWangSet is null)
            {
                _selectedWangSet = _advanced.WangSets.FirstOrDefault();
            }

            if (_selectedWangSet is not null && !_advanced.WangSets.Contains(_selectedWangSet))
            {
                _selectedWangSet = _advanced.WangSets.FirstOrDefault();
            }

            _wangSetList.Items.Clear();
            foreach (var set in _advanced.WangSets)
            {
                var item = new ListViewItem([set.Name, WangSetTypeLabel(set.Type), WangRepresentativeText(set.TileX, set.TileY)])
                {
                    Tag = set
                };
                _wangSetList.Items.Add(item);
                item.Selected = ReferenceEquals(set, _selectedWangSet);
            }

            if (_selectedWangSet is null)
            {
                _selectedWangColor = null;
            }
            else if (_selectedWangColor is null || !_selectedWangSet.Colors.Contains(_selectedWangColor))
            {
                _selectedWangColor = _selectedWangSet.Colors.FirstOrDefault();
            }

            _wangColorList.Items.Clear();
            if (_selectedWangSet is not null)
            {
                foreach (var color in _selectedWangSet.Colors.OrderBy(v => v.Index))
                {
                    var item = new ListViewItem([color.Name, "", color.Index.ToString(), color.Probability.ToString("0.##"), WangRepresentativeText(color.TileX, color.TileY)])
                    {
                        Tag = color
                    };
                    _wangColorList.Items.Add(item);
                    item.Selected = ReferenceEquals(color, _selectedWangColor);
                }
            }

            _deleteWangSetButton.Enabled = _selectedWangSet is not null;
            _setWangRepresentativeButton.Enabled = _selectedWangSet is not null;
        _mixedTypeButton.Enabled = _selectedWangSet is not null;
        _edgeTypeButton.Enabled = _selectedWangSet is not null;
        _cornerTypeButton.Enabled = _selectedWangSet is not null;
        _addWangColorButton.Enabled = _selectedWangSet is not null;
            _deleteWangColorButton.Enabled = _selectedWangColor is not null && _selectedWangSet?.Colors.Count > 1;
            _setWangColorRepresentativeButton.Enabled = _selectedWangColor is not null;
            _clearWangTileButton.Enabled = _selectedWangSet is not null;
            _checkWangPatternButton.Enabled = _selectedWangSet is not null;
            _canvas.ActiveWangSet = _selectedWangSet;
            _canvas.ActiveWangColor = _selectedWangColor;
            SyncWangTypeButtons();
        }
        finally
        {
            _suppressAdvancedListEvents = false;
        }

        _canvas.Invalidate();
    }

    private void RestoreWangSetSelection()
    {
        if (_selectedWangSet is null || !_advanced.WangSets.Contains(_selectedWangSet))
        {
            _selectedWangSet = _advanced.WangSets.FirstOrDefault();
        }

        _suppressAdvancedListEvents = true;
        try
        {
            foreach (ListViewItem item in _wangSetList.Items)
            {
                item.Selected = ReferenceEquals(item.Tag, _selectedWangSet);
                if (item.Selected)
                {
                    item.Focused = true;
                }
            }
        }
        finally
        {
            _suppressAdvancedListEvents = false;
        }
    }

    private void RestoreWangColorSelection()
    {
        if (_selectedWangSet is null)
        {
            _selectedWangColor = null;
            return;
        }

        if (_selectedWangColor is null || !_selectedWangSet.Colors.Contains(_selectedWangColor))
        {
            _selectedWangColor = _selectedWangSet.Colors.FirstOrDefault();
        }

        _suppressAdvancedListEvents = true;
        try
        {
            foreach (ListViewItem item in _wangColorList.Items)
            {
                item.Selected = ReferenceEquals(item.Tag, _selectedWangColor);
                if (item.Selected)
                {
                    item.Focused = true;
                }
            }
        }
        finally
        {
            _suppressAdvancedListEvents = false;
        }

        _canvas.ActiveWangColor = _selectedWangColor;
    }

    private static void DrawWangColorListSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.ColumnIndex != 1 || e.Item?.Tag is not TilesetWangColorDefinition color)
        {
            e.DrawDefault = true;
            return;
        }

        e.DrawBackground();
        var selected = e.Item.Selected;
        var bounds = e.Bounds;
        var swatch = new Rectangle(bounds.Left + 8, bounds.Top + 5, Math.Max(12, bounds.Width - 16), Math.Max(8, bounds.Height - 10));
        var fillColor = ParseColorOrDefault(color.ColorHex);
        using var brush = new SolidBrush(fillColor);
        using var border = new Pen(selected ? SystemColors.HighlightText : SystemColors.ControlDark);
        e.Graphics.FillRectangle(brush, swatch);
        e.Graphics.DrawRectangle(border, swatch);
    }

    private void RefreshAnimationFrameList(int selectedIndex = -1)
    {
        _animationFrameList.Items.Clear();
        if (_selectedRegion is not null)
        {
            for (var index = 0; index < _selectedRegion.AnimationFrames.Count; index++)
            {
                var frame = _selectedRegion.AnimationFrames[index];
                var item = new ListViewItem([(index + 1).ToString(), $"{frame.TileX},{frame.TileY}", $"{Math.Max(16, frame.DurationMs)} ms"])
                {
                    Tag = frame
                };
                _animationFrameList.Items.Add(item);
                item.Selected = index == selectedIndex;
            }
        }

        UpdateAnimationFrameButtons();
        _animationPreviewBox.Visible = true;
        _animationPreviewBox.Invalidate();
    }

    private void UpdateAnimationFrameButtons()
    {
        var canEdit = _selectedRegion is not null
            && string.Equals(_selectedRegion.Kind, TilesetRegionKinds.Normal, StringComparison.OrdinalIgnoreCase)
            && _selectedRegion.Animated;
        var selected = canEdit && _animationFrameList.SelectedItems.Count > 0;
        var selectedIndex = selected ? _animationFrameList.SelectedItems[0].Index : -1;
        _addAnimationFrameButton.Enabled = canEdit && _canvas.Selection.Width > 0 && _canvas.Selection.Height > 0;
        _editAnimationFrameButton.Enabled = selected;
        _deleteAnimationFrameButton.Enabled = selected;
        _moveAnimationFrameUpButton.Enabled = selected && selectedIndex > 0;
        _moveAnimationFrameDownButton.Enabled = selected && _selectedRegion is not null && selectedIndex < _selectedRegion.AnimationFrames.Count - 1;
        _animationFrameList.Enabled = canEdit;
        _animationPreviewBox.Enabled = CanPreviewAnimation();
    }

    private bool CanPreviewAnimation()
    {
        return _selectedRegion is not null
            && string.Equals(_selectedRegion.Kind, TilesetRegionKinds.Normal, StringComparison.OrdinalIgnoreCase)
            && _selectedRegion.Animated;
    }

    private void DrawAnimationPreview(object? sender, PaintEventArgs e)
    {
        e.Graphics.Clear(SystemColors.ControlDark);
        if (!CanPreviewAnimation() || _selectedRegion is null)
        {
            return;
        }

        var selectedFrame = CurrentAnimationFrame();
        if (selectedFrame is null)
        {
            return;
        }

        DrawPreviewTile(e.Graphics, selectedFrame.TileX, selectedFrame.TileY, _animationPreviewBox.ClientRectangle);
    }

    private void ShowAnimationPreviewWindow()
    {
        if (!CanPreviewAnimation())
        {
            return;
        }

        using var form = new Form
        {
            Text = "动画帧预览",
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ClientSize = new Size(300, 300),
            Font = Font
        };
        var preview = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = SystemColors.ControlDark,
            BorderStyle = BorderStyle.FixedSingle
        };
        using var timer = new System.Windows.Forms.Timer { Interval = 80 };
        preview.Paint += (_, e) =>
        {
            e.Graphics.Clear(SystemColors.ControlDark);
            DrawCurrentAnimationFrame(e.Graphics, preview.ClientRectangle);
        };
        timer.Tick += (_, _) => preview.Invalidate();
        timer.Start();
        form.Controls.Add(preview);
        form.ShowDialog(this);
    }

    private void DrawPreviewTile(Graphics graphics, int tileX, int tileY, Rectangle bounds)
    {
        if (tileX < 0 || tileY < 0 || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var source = new Rectangle(tileX * _tileSize, tileY * _tileSize, _tileSize, _tileSize);
        if (source.Right > _image.Width || source.Bottom > _image.Height)
        {
            return;
        }

        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
        var scale = Math.Max(1, Math.Min(bounds.Width / Math.Max(1, _tileSize), bounds.Height / Math.Max(1, _tileSize)));
        var size = _tileSize * scale;
        var destination = new Rectangle(
            bounds.Left + Math.Max(0, (bounds.Width - size) / 2),
            bounds.Top + Math.Max(0, (bounds.Height - size) / 2),
            size,
            size);
        graphics.DrawImage(_image, destination, source, GraphicsUnit.Pixel);
    }

    private void DrawCurrentAnimationFrame(Graphics graphics, Rectangle bounds)
    {
        if (!CanPreviewAnimation() || _selectedRegion is null)
        {
            return;
        }

        var selectedFrame = CurrentAnimationFrame();
        if (selectedFrame is null)
        {
            return;
        }

        DrawPreviewTile(graphics, selectedFrame.TileX, selectedFrame.TileY, bounds);
    }

    private TilesetFrameDefinition? CurrentAnimationFrame()
    {
        if (!CanPreviewAnimation() || _selectedRegion is null)
        {
            return null;
        }

        var frames = _selectedRegion.AnimationFrames.Count > 0
            ? _selectedRegion.AnimationFrames
            : Enumerable.Range(0, _selectedRegion.Height)
                .SelectMany(y => Enumerable.Range(0, _selectedRegion.Width).Select(x => new TilesetFrameDefinition
                {
                    TileX = _selectedRegion.X + x,
                    TileY = _selectedRegion.Y + y,
                    DurationMs = _selectedRegion.AnimationFrameDurationMs
                }))
                .ToList();
        if (frames.Count <= 0)
        {
            return null;
        }

        var elapsed = Environment.TickCount;
        var total = frames.Sum(frame => Math.Max(16, frame.DurationMs <= 0 ? _selectedRegion.AnimationFrameDurationMs : frame.DurationMs));
        var cursor = total <= 0 ? 0 : PositiveModulo(elapsed, total);
        var accumulated = 0;
        foreach (var frame in frames)
        {
            accumulated += Math.Max(16, frame.DurationMs <= 0 ? _selectedRegion.AnimationFrameDurationMs : frame.DurationMs);
            if (cursor < accumulated)
            {
                return frame;
            }
        }

        return frames[0];
    }

    private void AddAnimationFrameFromSelection()
    {
        if (!CanEditAnimationFrames())
        {
            return;
        }

        var selection = _canvas.Selection;
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            MessageBox.Show(this, "请先在图集中选中要加入动画的瓦片。", "动画帧", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        foreach (var y in Enumerable.Range(selection.Y, selection.Height))
        {
            foreach (var x in Enumerable.Range(selection.X, selection.Width))
            {
                _selectedRegion!.AnimationFrames.Add(new TilesetFrameDefinition
                {
                    TileX = x,
                    TileY = y,
                    DurationMs = (int)_frameDurationBox.Value
                });
            }
        }

        _selectedRegion!.Animated = true;
        RefreshAnimationFrameList(_selectedRegion.AnimationFrames.Count - 1);
        _canvas.Invalidate();
    }

    private void EditSelectedAnimationFrame()
    {
        if (!CanEditAnimationFrames() || _animationFrameList.SelectedItems.Count <= 0)
        {
            return;
        }

        var index = _animationFrameList.SelectedItems[0].Index;
        var frame = _selectedRegion!.AnimationFrames[index];
        if (!TryEditAnimationFrame(frame, out var tileX, out var tileY, out var durationMs))
        {
            return;
        }

        frame.TileX = tileX;
        frame.TileY = tileY;
        frame.DurationMs = durationMs;
        RefreshAnimationFrameList(index);
        _canvas.Invalidate();
    }

    private void DeleteSelectedAnimationFrame()
    {
        if (!CanEditAnimationFrames() || _animationFrameList.SelectedItems.Count <= 0)
        {
            return;
        }

        var index = _animationFrameList.SelectedItems[0].Index;
        _selectedRegion!.AnimationFrames.RemoveAt(index);
        RefreshAnimationFrameList(Math.Min(index, _selectedRegion.AnimationFrames.Count - 1));
        _canvas.Invalidate();
    }

    private void MoveSelectedAnimationFrame(int direction)
    {
        if (!CanEditAnimationFrames() || _animationFrameList.SelectedItems.Count <= 0)
        {
            return;
        }

        var index = _animationFrameList.SelectedItems[0].Index;
        var target = index + direction;
        if (target < 0 || target >= _selectedRegion!.AnimationFrames.Count)
        {
            return;
        }

        (_selectedRegion.AnimationFrames[index], _selectedRegion.AnimationFrames[target]) = (_selectedRegion.AnimationFrames[target], _selectedRegion.AnimationFrames[index]);
        RefreshAnimationFrameList(target);
        _canvas.Invalidate();
    }

    private bool CanEditAnimationFrames()
    {
        return _selectedRegion is not null
            && string.Equals(_selectedRegion.Kind, TilesetRegionKinds.Normal, StringComparison.OrdinalIgnoreCase)
            && _selectedRegion.Animated;
    }

    private bool TryEditAnimationFrame(TilesetFrameDefinition frame, out int tileX, out int tileY, out int durationMs)
    {
        tileX = frame.TileX;
        tileY = frame.TileY;
        durationMs = Math.Clamp(frame.DurationMs <= 0 ? (int)_frameDurationBox.Value : frame.DurationMs, 16, 2000);

        using var form = CreateEditDialog("编辑动画帧", 3, 420, 222);
        var tileXBox = CreateCoordinateBox(tileX);
        var tileYBox = CreateCoordinateBox(tileY);
        var durationBox = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 120,
            Minimum = 16,
            Maximum = 2000,
            Increment = 10,
            Value = Math.Clamp(durationMs, 16, 2000)
        };
        AddEditorRow((TableLayoutPanel)form.Tag!, 0, "瓦片X", tileXBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 1, "瓦片Y", tileYBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 2, "时长(ms)", durationBox);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        tileX = (int)tileXBox.Value;
        tileY = (int)tileYBox.Value;
        durationMs = (int)durationBox.Value;
        return true;
    }

    private void EditSelectedTileMetadata()
    {
        var selection = _canvas.Selection;
        if (selection.Width <= 0 || selection.Height <= 0)
        {
            MessageBox.Show(this, "请先在图集中选择一个瓦片。", "瓦片属性", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        EditTileMetadata(selection.X, selection.Y);
    }

    private void EditTileMetadata(int tileX, int tileY)
    {
        if (tileX < 0 || tileY < 0)
        {
            return;
        }

        var existing = _tiles.FirstOrDefault(tile => tile.TileX == tileX && tile.TileY == tileY);
        var working = existing is null
            ? new TilesetTileMetadataDefinition { TileX = tileX, TileY = tileY }
            : CloneTiles([existing])[0];
        if (!ShowTileMetadataDialog(working))
        {
            return;
        }

        _tiles.RemoveAll(tile => tile.TileX == tileX && tile.TileY == tileY);
        _tiles.Add(working);
        _canvas.SetTileMetadata(_tiles);
    }

    private bool ShowTileMetadataDialog(TilesetTileMetadataDefinition tile)
    {
        var editor = new TileMetadataDialogEditor(_image, _tileSize, Font, this);
        return editor.ShowTileMetadataDialog(tile);
    }

    private sealed class TileMetadataDialogEditor
    {
        private readonly Image _image;
        private readonly int _tileSize;
        private readonly Font _font;
        private readonly IWin32Window _owner;

        public TileMetadataDialogEditor(Image image, int tileSize, Font font, IWin32Window owner)
        {
            _image = image;
            _tileSize = Math.Max(8, tileSize);
            _font = font;
            _owner = owner;
        }

        public bool ShowTileMetadataDialog(TilesetTileMetadataDefinition tile)
        {
            using var form = new Form
        {
            Text = $"瓦片属性 {tile.TileX},{tile.TileY}",
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            ClientSize = new Size(980, 640),
            Font = _font
        };

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var propertyPage = new TabPage("属性");
        var collisionPage = new TabPage("碰撞");
        tabs.TabPages.Add(propertyPage);
        tabs.TabPages.Add(collisionPage);
        BuildTilePropertyPage(propertyPage, tile);
        BuildTileCollisionPage(collisionPage, tile);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 12, 12)
        };
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 88, Height = 34 };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 88, Height = 34 };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        form.Controls.Add(tabs);
        form.Controls.Add(buttons);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
            return form.ShowDialog(_owner) == DialogResult.OK;
        }

        private static void BuildTilePropertyPage(Control page, TilesetTileMetadataDefinition tile)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 10,
                Padding = new Padding(14)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var row = 0; row < 10; row++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, row >= 8 ? 80 : 36));
            }

        var nameBox = new TextBox { Dock = DockStyle.Fill, Text = tile.DisplayName };
        var categoryBox = new TextBox { Dock = DockStyle.Fill, Text = tile.Category };
        var walkableBox = new CheckBox { Dock = DockStyle.Fill, Text = "允许通行", Checked = tile.Walkable, TextAlign = ContentAlignment.MiddleLeft };
        var sightBox = new CheckBox { Dock = DockStyle.Fill, Text = "阻挡视线", Checked = tile.BlocksSight, TextAlign = ContentAlignment.MiddleLeft };
        var moveCostPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty };
        var overrideMoveCostBox = new CheckBox { AutoSize = true, Text = "覆盖", Checked = tile.MoveCost is not null, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 7, 8, 0) };
        var moveCostBox = new NumericUpDown
        {
            Width = 120,
            DecimalPlaces = 2,
            Increment = 0.1M,
            Minimum = 0.01M,
            Maximum = 999M,
            Value = (decimal)Math.Clamp(tile.MoveCost ?? 1d, 0.01d, 999d),
            Enabled = tile.MoveCost is not null
        };
        var moveCostHint = new Label { AutoSize = true, Text = "不覆盖时使用数据库地形规则", ForeColor = SystemColors.GrayText, Margin = new Padding(8, 8, 0, 0) };
        moveCostPanel.Controls.Add(overrideMoveCostBox);
        moveCostPanel.Controls.Add(moveCostBox);
        moveCostPanel.Controls.Add(moveCostHint);
        var materialBox = new TextBox { Dock = DockStyle.Fill, Text = tile.MaterialTag };
        var footstepBox = new TextBox { Dock = DockStyle.Fill, Text = tile.FootstepSoundId };
        var tagsBox = new TextBox { Dock = DockStyle.Fill, Text = string.Join(", ", tile.Tags) };
        var customBox = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = string.Join(Environment.NewLine, tile.CustomProperties.Select(pair => $"{pair.Key}={pair.Value}"))
        };
        var hint = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "标签用逗号分隔；自定义属性每行一个 key=value。"
        };

        AddEditorRow(layout, 0, "显示名", nameBox);
        AddEditorRow(layout, 1, "分类", categoryBox);
        AddEditorRow(layout, 2, "通行", walkableBox);
        AddEditorRow(layout, 3, "视线", sightBox);
        AddEditorRow(layout, 4, "移动消耗", moveCostPanel);
        AddEditorRow(layout, 5, "材质标签", materialBox);
        AddEditorRow(layout, 6, "脚步音", footstepBox);
        AddEditorRow(layout, 7, "标签", tagsBox);
        AddEditorRow(layout, 8, "自定义属性", customBox);
        layout.Controls.Add(hint, 0, 9);
        layout.SetColumnSpan(hint, 2);
        page.Controls.Add(layout);

        nameBox.TextChanged += (_, _) => tile.DisplayName = nameBox.Text.Trim();
        categoryBox.TextChanged += (_, _) => tile.Category = categoryBox.Text.Trim();
        walkableBox.CheckedChanged += (_, _) => tile.Walkable = walkableBox.Checked;
        sightBox.CheckedChanged += (_, _) => tile.BlocksSight = sightBox.Checked;
        overrideMoveCostBox.CheckedChanged += (_, _) =>
        {
            moveCostBox.Enabled = overrideMoveCostBox.Checked;
            tile.MoveCost = overrideMoveCostBox.Checked ? (double)moveCostBox.Value : null;
        };
        moveCostBox.ValueChanged += (_, _) =>
        {
            if (overrideMoveCostBox.Checked)
            {
                tile.MoveCost = (double)moveCostBox.Value;
            }
        };
        materialBox.TextChanged += (_, _) => tile.MaterialTag = materialBox.Text.Trim();
        footstepBox.TextChanged += (_, _) => tile.FootstepSoundId = footstepBox.Text.Trim();
        tagsBox.TextChanged += (_, _) => tile.Tags = tagsBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        customBox.TextChanged += (_, _) => tile.CustomProperties = ParseCustomProperties(customBox.Text);
        }

        private void BuildTileCollisionPage(Control page, TilesetTileMetadataDefinition tile)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(14)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false,
            MultiSelect = false
        };
        list.Columns.Add("类型", 90);
        list.Columns.Add("区域/点", 260);
        list.Columns.Add("标签", 220);

        var canvas = new TileCollisionEditorCanvas(_image, _tileSize, tile)
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 14, 0)
        };

        var rightPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        rightPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var toolButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        for (var index = 0; index < 4; index++)
        {
            toolButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        var selectTool = CreateToolRadioButton("选择", true);
        var rectTool = CreateToolRadioButton("矩形");
        var ellipseTool = CreateToolRadioButton("椭圆");
        var polygonTool = CreateToolRadioButton("多边形");
        toolButtons.Controls.Add(selectTool, 0, 0);
        toolButtons.Controls.Add(rectTool, 1, 0);
        toolButtons.Controls.Add(ellipseTool, 2, 0);
        toolButtons.Controls.Add(polygonTool, 3, 0);

        var actionButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        actionButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        actionButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        var edit = new Button { Text = "编辑", Dock = DockStyle.Fill, Height = 32 };
        var delete = new Button { Text = "删除", Dock = DockStyle.Fill, Height = 32 };
        edit.Margin = new Padding(0, 2, 6, 2);
        delete.Margin = new Padding(6, 2, 0, 2);
        actionButtons.Controls.Add(edit, 0, 0);
        actionButtons.Controls.Add(delete, 1, 0);

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            Text = "在左侧瓦片上拖拽绘制矩形/椭圆；多边形点击添加点，右键或双击结束。表格用于选择和精确编辑。",
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.MiddleLeft
        };

        rightPanel.Controls.Add(toolButtons, 0, 0);
        rightPanel.Controls.Add(actionButtons, 0, 1);
        rightPanel.Controls.Add(list, 0, 2);
        rightPanel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "选中形状后可拖动移动；拖动控制点可缩放或调整多边形点。",
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 3);
        rightPanel.Controls.Add(hint, 0, 4);

        layout.Controls.Add(canvas, 0, 0);
        layout.Controls.Add(rightPanel, 1, 0);
        page.Controls.Add(layout);

        var syncingSelection = false;

        void RefreshList(TileCollisionShapeDefinition? selected = null)
        {
            selected ??= canvas.SelectedShape;
            syncingSelection = true;
            list.BeginUpdate();
            try
            {
                list.Items.Clear();
                foreach (var shape in tile.CollisionShapes)
                {
                    var item = new ListViewItem([CollisionShapeTypeLabel(shape.ShapeType), CollisionShapeSummary(shape), shape.Tag])
                    {
                        Tag = shape
                    };
                    list.Items.Add(item);
                    item.Selected = ReferenceEquals(shape, selected);
                }
            }
            finally
            {
                list.EndUpdate();
                syncingSelection = false;
            }

            var hasSelected = list.SelectedItems.Count > 0;
            edit.Enabled = hasSelected;
            delete.Enabled = hasSelected;
        }

        void SelectShape(TileCollisionShapeDefinition? shape)
        {
            if (syncingSelection)
            {
                return;
            }

            syncingSelection = true;
            try
            {
                canvas.SelectedShape = shape;
                foreach (ListViewItem item in list.Items)
                {
                    item.Selected = ReferenceEquals(item.Tag, shape);
                    if (item.Selected)
                    {
                        item.EnsureVisible();
                    }
                }

                RefreshCollisionButtons(list, edit, delete);
            }
            finally
            {
                syncingSelection = false;
            }
        }

        selectTool.CheckedChanged += (_, _) =>
        {
            if (selectTool.Checked)
            {
                canvas.ActiveTool = TileCollisionEditTool.Select;
            }
        };
        rectTool.CheckedChanged += (_, _) =>
        {
            if (rectTool.Checked)
            {
                canvas.ActiveTool = TileCollisionEditTool.Rectangle;
            }
        };
        ellipseTool.CheckedChanged += (_, _) =>
        {
            if (ellipseTool.Checked)
            {
                canvas.ActiveTool = TileCollisionEditTool.Ellipse;
            }
        };
        polygonTool.CheckedChanged += (_, _) =>
        {
            if (polygonTool.Checked)
            {
                canvas.ActiveTool = TileCollisionEditTool.Polygon;
            }
        };
        edit.Click += (_, _) =>
        {
            if (list.SelectedItems.Count <= 0 || list.SelectedItems[0].Tag is not TileCollisionShapeDefinition shape)
            {
                return;
            }

            if (TryEditCollisionShape(shape))
            {
                canvas.Invalidate();
                RefreshList(shape);
            }
        };
        delete.Click += (_, _) =>
        {
            if (list.SelectedItems.Count <= 0 || list.SelectedItems[0].Tag is not TileCollisionShapeDefinition shape)
            {
                return;
            }

            tile.CollisionShapes.Remove(shape);
            SelectShape(null);
            RefreshList();
            canvas.Invalidate();
        };
        list.SelectedIndexChanged += (_, _) =>
        {
            if (syncingSelection)
            {
                return;
            }

            SelectShape(list.SelectedItems.Count > 0
                ? list.SelectedItems[0].Tag as TileCollisionShapeDefinition
                : null);
        };
        list.DoubleClick += (_, _) => edit.PerformClick();
        canvas.SelectionChanged += (_, shape) => SelectShape(shape);
        canvas.ShapesChanged += (_, _) => RefreshList(canvas.SelectedShape);
        RefreshList();
        }

        private static RadioButton CreateToolRadioButton(string text, bool @checked = false)
        {
            return new RadioButton
            {
                Appearance = Appearance.Button,
                Dock = DockStyle.Fill,
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 32,
                Checked = @checked,
                Margin = new Padding(0, 2, 6, 2)
            };
        }

        private static void RefreshCollisionButtons(ListView list, Button edit, Button delete)
        {
            var enabled = list.SelectedItems.Count > 0;
            edit.Enabled = enabled;
            delete.Enabled = enabled;
        }

        private bool TryEditCollisionShape(TileCollisionShapeDefinition shape)
        {
            var isPolygon = string.Equals(shape.ShapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase);
            using var form = CreateEditDialog($"编辑{CollisionShapeTypeLabel(shape.ShapeType)}碰撞", isPolygon ? 3 : 5, 520, isPolygon ? 300 : 320);
            var tagBox = new TextBox { Dock = DockStyle.Fill, Text = shape.Tag };
            if (isPolygon)
            {
                var layout = (TableLayoutPanel)form.Tag!;
                layout.Height = 206;
                layout.RowStyles[1].SizeType = SizeType.Absolute;
                layout.RowStyles[1].Height = 126;
                var pointsBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    WordWrap = false,
                    Text = string.Join("; ", shape.Points.Select(point => $"{point.X:0.##},{point.Y:0.##}"))
                };
                AddEditorRow(layout, 0, "标签", tagBox);
                AddEditorRow(layout, 1, "点列表", pointsBox);
                AddEditorRow(layout, 2, "格式", new Label { Dock = DockStyle.Fill, Text = "例如：0,0; 1,0; 0.5,1", TextAlign = ContentAlignment.MiddleLeft, ForeColor = SystemColors.GrayText });
                if (form.ShowDialog(_owner) != DialogResult.OK)
                {
                    return false;
                }

                var points = ParseCollisionPoints(pointsBox.Text);
                if (points.Count < 3)
                {
                    MessageBox.Show(_owner, "多边形至少需要 3 个点。", "碰撞形状", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                shape.Tag = tagBox.Text.Trim();
                shape.Points = points;
                return true;
            }

            var xBox = CreateNormalizedBox(shape.X);
            var yBox = CreateNormalizedBox(shape.Y);
            var widthBox = CreateNormalizedBox(shape.Width <= 0 ? 1f : shape.Width);
            var heightBox = CreateNormalizedBox(shape.Height <= 0 ? 1f : shape.Height);
            AddEditorRow((TableLayoutPanel)form.Tag!, 0, "标签", tagBox);
            AddEditorRow((TableLayoutPanel)form.Tag!, 1, "X", xBox);
            AddEditorRow((TableLayoutPanel)form.Tag!, 2, "Y", yBox);
            AddEditorRow((TableLayoutPanel)form.Tag!, 3, "宽", widthBox);
            AddEditorRow((TableLayoutPanel)form.Tag!, 4, "高", heightBox);
            if (form.ShowDialog(_owner) != DialogResult.OK)
            {
                return false;
            }

            shape.Tag = tagBox.Text.Trim();
            shape.X = (float)xBox.Value;
            shape.Y = (float)yBox.Value;
            shape.Width = (float)Math.Max(0.01M, widthBox.Value);
            shape.Height = (float)Math.Max(0.01M, heightBox.Value);
            return true;
        }

        private Form CreateEditDialog(string title, int rows, int width = 420, int height = -1)
        {
            var form = new Form
            {
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false,
                ClientSize = new Size(width, height > 0 ? height : Math.Max(160, rows * 44 + 74)),
                Font = _font,
                ShowInTaskbar = false
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = rows
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (var row = 0; row < rows; row++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            }

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 10, 12, 12)
            };
            var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 88, Height = 34 };
            var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 88, Height = 34 };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            form.Controls.Add(layout);
            form.Controls.Add(buttons);
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            form.Tag = layout;
            return form;
        }

        private static NumericUpDown CreateNormalizedBox(float value)
        {
            return new NumericUpDown
            {
                Dock = DockStyle.Left,
                Width = 110,
                DecimalPlaces = 2,
                Increment = 0.05M,
                Minimum = 0,
                Maximum = 1,
                Value = (decimal)Math.Clamp(value, 0f, 1f)
            };
        }

        private static Dictionary<string, string> ParseCustomProperties(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in text.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    result[key] = value;
                }
            }

            return result;
        }

        private static List<TileCollisionPointDefinition> ParseCollisionPoints(string text)
        {
            var result = new List<TileCollisionPointDefinition>();
            foreach (var token in text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = token.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length != 2 || !float.TryParse(parts[0], out var x) || !float.TryParse(parts[1], out var y))
                {
                    continue;
                }

                result.Add(new TileCollisionPointDefinition
                {
                    X = Math.Clamp(x, 0f, 1f),
                    Y = Math.Clamp(y, 0f, 1f)
                });
            }

            return result;
        }

        private static string CollisionShapeTypeLabel(string shapeType)
        {
            return shapeType switch
            {
                TileCollisionShapeTypes.Ellipse => "椭圆",
                TileCollisionShapeTypes.Polygon => "多边形",
                _ => "矩形"
            };
        }

        private static string CollisionShapeSummary(TileCollisionShapeDefinition shape)
        {
            if (string.Equals(shape.ShapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase))
            {
                return string.Join("; ", shape.Points.Select(point => $"{point.X:0.##},{point.Y:0.##}"));
            }

            return $"{shape.X:0.##},{shape.Y:0.##} {shape.Width:0.##}x{shape.Height:0.##}";
        }

        private static void AddEditorRow(TableLayoutPanel panel, int row, string label, Control editor)
        {
            panel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = label,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, row);
            panel.Controls.Add(editor, 1, row);
        }
    }

    private void ConfigureAnimationIconButton(Button button, string tooltip)
    {
        button.Width = 36;
        button.Height = 30;
        button.Margin = new Padding(0, 2, 4, 2);
        button.TextAlign = ContentAlignment.MiddleCenter;
        _toolTip.SetToolTip(button, tooltip);
    }

    private void RefreshSelectedWangTileState()
    {
        _clearWangTileButton.Enabled = _selectedWangSet is not null;
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

    private void SyncAnimationControls()
    {
        if (_selectedRegion is null || !string.Equals(_selectedRegion.Kind, TilesetRegionKinds.Normal, StringComparison.OrdinalIgnoreCase))
        {
            _animatedCheckBox.Checked = false;
            _frameDurationBox.Value = 100;
            RefreshAnimationFrameList();
            return;
        }

        if (_animatedCheckBox.Checked != _selectedRegion.Animated)
        {
            _animatedCheckBox.Checked = _selectedRegion.Animated;
        }

        var clamped = Math.Clamp(_selectedRegion.AnimationFrameDurationMs, (int)_frameDurationBox.Minimum, (int)_frameDurationBox.Maximum);
        if (_frameDurationBox.Value != clamped)
        {
            _frameDurationBox.Value = clamped;
        }

        RefreshAnimationFrameList();
    }

    private string SelectedKind()
    {
        if (_ignoredButton.Checked)
        {
            return TilesetRegionKinds.Ignored;
        }

        if (_hiddenButton.Checked)
        {
            return TilesetRegionKinds.Hidden;
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
            TilesetRegionKinds.Hidden => "隐藏",
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

    private bool TryEditWangSet(TilesetWangSetDefinition set, out string name, out string type, out int tileX, out int tileY)
    {
        name = set.Name;
        type = set.Type;
        tileX = set.TileX;
        tileY = set.TileY;
        using var form = CreateEditDialog("编辑 Wang 集合", 4);
        var nameBox = new TextBox { Dock = DockStyle.Fill, Text = name };
        var typeBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
        typeBox.Items.AddRange([
            new PlannerComboItem(TilesetWangSetTypes.Mixed, "混合：边和角"),
            new PlannerComboItem(TilesetWangSetTypes.Corner, "角：四角匹配"),
            new PlannerComboItem(TilesetWangSetTypes.Edge, "边：四边匹配")
        ]);
        SelectComboByValue(typeBox, set.Type);
        var tileXBox = CreateCoordinateBox(tileX);
        var tileYBox = CreateCoordinateBox(tileY);
        AddEditorRow((TableLayoutPanel)form.Tag!, 0, "名称", nameBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 1, "匹配类型", typeBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 2, "代表X", tileXBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 3, "代表Y", tileYBox);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        name = string.IsNullOrWhiteSpace(nameBox.Text) ? set.Id : nameBox.Text.Trim();
        type = SelectedComboValue(typeBox, TilesetWangSetTypes.Mixed);
        tileX = (int)tileXBox.Value;
        tileY = (int)tileYBox.Value;
        return true;
    }

    private bool TryEditWangColor(TilesetWangSetDefinition set, TilesetWangColorDefinition color, out string name, out int index, out string colorHex, out double probability, out int tileX, out int tileY)
    {
        name = color.Name;
        index = color.Index;
        colorHex = string.IsNullOrWhiteSpace(color.ColorHex) ? DefaultWangColor(color.Index) : color.ColorHex;
        probability = color.Probability;
        tileX = color.TileX;
        tileY = color.TileY;
        var selectedColorHex = colorHex;
        using var form = CreateEditDialog("编辑地形标签", 6, 540, 368);
        var nameBox = new TextBox { Dock = DockStyle.Fill, Text = name };
        var indexBox = new NumericUpDown { Dock = DockStyle.Left, Width = 120, Minimum = 1, Maximum = 255, Value = Math.Clamp(index, 1, 255) };
        var colorBox = new TextBox { Dock = DockStyle.Fill, Text = colorHex, ReadOnly = true };
        var colorButton = new Button { Text = "选择...", Dock = DockStyle.Left, Width = 78 };
        var colorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        colorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        colorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
        colorPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        colorPanel.Controls.Add(colorBox, 0, 0);
        colorPanel.Controls.Add(colorButton, 1, 0);
        colorButton.Dock = DockStyle.Fill;
        colorButton.Margin = new Padding(6, 0, 0, 0);
        colorBox.Margin = Padding.Empty;
        var probabilityBox = new NumericUpDown { Dock = DockStyle.Left, Width = 120, DecimalPlaces = 2, Increment = 0.05M, Minimum = 0, Maximum = 999, Value = (decimal)Math.Clamp(probability <= 0 ? 1d : probability, 0d, 999d) };
        var tileXBox = CreateCoordinateBox(tileX);
        var tileYBox = CreateCoordinateBox(tileY);
        colorButton.Click += (_, _) =>
        {
            using var dialog = new ColorDialog
            {
                AnyColor = true,
                FullOpen = true,
                SolidColorOnly = false
            };

            try
            {
                dialog.Color = ColorTranslator.FromHtml(string.IsNullOrWhiteSpace(colorBox.Text) ? selectedColorHex : colorBox.Text);
            }
            catch
            {
                dialog.Color = ColorTranslator.FromHtml(DefaultWangColor(color.Index));
            }

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                colorBox.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}".ToLowerInvariant();
            }
        };
        AddEditorRow((TableLayoutPanel)form.Tag!, 0, "名称", nameBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 1, "索引", indexBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 2, "颜色", colorPanel);
        AddEditorRow((TableLayoutPanel)form.Tag!, 3, "概率", probabilityBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 4, "代表X", tileXBox);
        AddEditorRow((TableLayoutPanel)form.Tag!, 5, "代表Y", tileYBox);
        var layout = (TableLayoutPanel)form.Tag!;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        var colorHint = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = SystemColors.GrayText,
            Text = "颜色支持从系统色表中选择，也可直接输入十六进制值。"
        };
        layout.Controls.Add(colorHint, 0, 6);
        layout.SetColumnSpan(colorHint, 2);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        var newIndex = (int)indexBox.Value;
        if (newIndex != color.Index && set.Colors.Any(v => v.Index == newIndex))
        {
            MessageBox.Show(this, "地形标签索引不能重复。", "图集规划", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return false;
        }

        name = string.IsNullOrWhiteSpace(nameBox.Text) ? $"地形 {newIndex}" : nameBox.Text.Trim();
        index = newIndex;
        colorHex = string.IsNullOrWhiteSpace(colorBox.Text) ? DefaultWangColor(newIndex) : colorBox.Text.Trim();
        probability = (double)probabilityBox.Value;
        tileX = (int)tileXBox.Value;
        tileY = (int)tileYBox.Value;
        return true;
    }

    private bool TryEditWangTile(TilesetWangTileDefinition tile, out double probability)
    {
        probability = tile.Probability;
        using var form = CreateEditDialog("编辑 Wang 瓦片", 1, 360, 160);
        var probabilityBox = new NumericUpDown { Dock = DockStyle.Left, Width = 110, DecimalPlaces = 2, Increment = 0.05M, Minimum = 0, Maximum = 999, Value = (decimal)Math.Clamp(probability <= 0 ? 1d : probability, 0d, 999d) };
        AddEditorRow((TableLayoutPanel)form.Tag!, 0, "概率", probabilityBox);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return false;
        }

        probability = (double)probabilityBox.Value;
        return true;
    }

    private Form CreateEditDialog(string title, int rows, int width = 420, int height = -1)
    {
        var form = new Form
        {
            Text = title,
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(width, height > 0 ? height : 84 + rows * 46),
            Font = Font
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = rows,
            Height = rows * 46,
            Padding = new Padding(12, 14, 12, 6)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < rows; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 54,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 10, 12, 12)
        };
        var ok = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 88, Height = 34 };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 88, Height = 34 };
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        form.Controls.Add(layout);
        form.Controls.Add(buttons);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.Tag = layout;
        return form;
    }

    private static NumericUpDown CreateCoordinateBox(int value)
    {
        return new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 110,
            Minimum = -1,
            Maximum = 4096,
            Value = Math.Clamp(value, -1, 4096)
        };
    }

    private static void AddEditorRow(TableLayoutPanel panel, int row, string label, Control editor)
    {
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = label,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, row);
        panel.Controls.Add(editor, 1, row);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
        {
            return 0;
        }

        var result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static T? SelectedTag<T>(ListView list) where T : class
    {
        return list.SelectedItems.Count > 0 ? list.SelectedItems[0].Tag as T : null;
    }

    private static void SelectComboByValue(ComboBox combo, string value)
    {
        for (var index = 0; index < combo.Items.Count; index++)
        {
            if (combo.Items[index] is PlannerComboItem item
                && string.Equals(item.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = index;
                return;
            }
        }

        if (combo.Items.Count > 0 && combo.SelectedIndex < 0)
        {
            combo.SelectedIndex = 0;
        }
    }

    private void SyncWangTypeButtons()
    {
        if (_selectedWangSet is null)
        {
            _mixedTypeButton.Checked = false;
            _edgeTypeButton.Checked = false;
            _cornerTypeButton.Checked = false;
            return;
        }

        _mixedTypeButton.Checked = string.Equals(_selectedWangSet.Type, TilesetWangSetTypes.Mixed, StringComparison.OrdinalIgnoreCase);
        _edgeTypeButton.Checked = string.Equals(_selectedWangSet.Type, TilesetWangSetTypes.Edge, StringComparison.OrdinalIgnoreCase);
        _cornerTypeButton.Checked = string.Equals(_selectedWangSet.Type, TilesetWangSetTypes.Corner, StringComparison.OrdinalIgnoreCase);
    }

    private static string DefaultWangColor(int index)
    {
        string[] colors = ["#22c55e", "#3b82f6", "#f97316", "#a855f7", "#eab308", "#ef4444", "#14b8a6", "#64748b"];
        return colors[(Math.Max(1, index) - 1) % colors.Length];
    }

    private static Color ParseColorOrDefault(string? hex)
    {
        try
        {
            return string.IsNullOrWhiteSpace(hex) ? SystemColors.Window : ColorTranslator.FromHtml(hex);
        }
        catch
        {
            return SystemColors.Window;
        }
    }

    private static string WangRepresentativeText(int tileX, int tileY)
    {
        return tileX >= 0 && tileY >= 0 ? $"{tileX},{tileY}" : "-";
    }

    private static string WangSetTypeLabel(string type)
    {
        return type switch
        {
            TilesetWangSetTypes.Corner => "角",
            TilesetWangSetTypes.Edge => "边",
            _ => "混合"
        };
    }

    private void ShowWangPatternsDialog()
    {
        if (_selectedWangSet is null)
        {
            return;
        }

        var edgeSnapshots = _selectedWangSet.Type is TilesetWangSetTypes.Edge or TilesetWangSetTypes.Mixed
            ? BuildWangPatternSnapshots(_selectedWangSet, TilesetWangSetTypes.Edge)
            : [];
        var cornerSnapshots = _selectedWangSet.Type is TilesetWangSetTypes.Corner or TilesetWangSetTypes.Mixed
            ? BuildWangPatternSnapshots(_selectedWangSet, TilesetWangSetTypes.Corner)
            : [];

        using var form = new Form
        {
            Text = "Wang 图案检查",
            StartPosition = FormStartPosition.CenterParent,
            Size = new Size(760, 560),
            MinimumSize = new Size(640, 420),
            Font = Font
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        var summary = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = BuildWangPatternSummary(_selectedWangSet, edgeSnapshots, cornerSnapshots)
        };
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        list.Columns.Add("状态", 64);
        list.Columns.Add("类别", 64);
        list.Columns.Add("组合", 80);
        list.Columns.Add("说明", 220);
        list.Columns.Add("候选", 70);
        list.Columns.Add("回退", 180);
        foreach (var snapshot in edgeSnapshots.Concat(cornerSnapshots))
        {
            var item = new ListViewItem([
                snapshot.Present ? "有" : "缺",
                snapshot.Kind,
                snapshot.Key,
                snapshot.Description,
                snapshot.Count.ToString(),
                snapshot.Present ? "-" : ResolveWangFallback(snapshot)
            ]);
            if (!snapshot.Present)
            {
                item.ForeColor = Color.Firebrick;
            }

            list.Items.Add(item);
        }

        var hint = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = SystemColors.GrayText,
            TextAlign = ContentAlignment.MiddleLeft,
            Text = "缺失组合会在绘制时使用最接近的已标记图块回退；建议补齐常用边、角和孤立组合。"
        };
        layout.Controls.Add(summary, 0, 0);
        layout.Controls.Add(list, 0, 1);
        layout.Controls.Add(hint, 0, 2);
        form.Controls.Add(layout);
        form.ShowDialog(this);
    }

    private static string BuildWangPatternSummary(
        TilesetWangSetDefinition set,
        IReadOnlyList<WangPatternSnapshot> edgeSnapshots,
        IReadOnlyList<WangPatternSnapshot> cornerSnapshots)
    {
        var lines = new List<string>
        {
            $"集合：{set.Name}",
            $"类型：{WangSetTypeLabel(set.Type)}"
        };
        if (edgeSnapshots.Count > 0)
        {
            lines.Add($"边组合：{edgeSnapshots.Count(v => v.Present)}/{edgeSnapshots.Count}，缺失 {edgeSnapshots.Count(v => !v.Present)}");
        }

        if (cornerSnapshots.Count > 0)
        {
            lines.Add($"角组合：{cornerSnapshots.Count(v => v.Present)}/{cornerSnapshots.Count}，缺失 {cornerSnapshots.Count(v => !v.Present)}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string ResolveWangFallback(WangPatternSnapshot snapshot)
    {
        if (snapshot.Key.All(v => v == '0'))
        {
            return "建议添加空白/孤立基础图块";
        }

        var activeCount = snapshot.Key.Count(v => v == '1');
        return activeCount switch
        {
            1 => "回退到单边/单角邻近图块",
            2 => "回退到相邻连接图块",
            _ => "回退到连接数更少的图块"
        };
    }

    private static List<WangPatternSnapshot> BuildWangPatternSnapshots(TilesetWangSetDefinition set, string patternKind)
    {
        var groups = set.Tiles
            .GroupBy(tile => NormalizePatternKey(tile.WangId, patternKind))
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(tile => tile.TileY).ThenBy(tile => tile.TileX).ToList(),
                StringComparer.Ordinal);

        var snapshots = new List<WangPatternSnapshot>();
        foreach (var pattern in BuildExpectedPatterns(patternKind))
        {
            groups.TryGetValue(pattern.Key, out var matches);
            snapshots.Add(new WangPatternSnapshot(
                pattern.Kind,
                pattern.Key,
                pattern.Description,
                matches is { Count: > 0 },
                matches?.Count ?? 0));
        }

        return snapshots;
    }

    private static List<WangPatternDescriptor> BuildExpectedPatterns(string setType)
    {
        var result = new List<WangPatternDescriptor>();
        if (setType is TilesetWangSetTypes.Edge or TilesetWangSetTypes.Mixed)
        {
            for (var value = 0; value < 16; value++)
            {
                var bits = Convert.ToString(value, 2).PadLeft(4, '0');
                result.Add(new WangPatternDescriptor("边", bits, $"上下左右：{bits}"));
            }
        }

        if (setType is TilesetWangSetTypes.Corner or TilesetWangSetTypes.Mixed)
        {
            for (var value = 0; value < 16; value++)
            {
                var bits = Convert.ToString(value, 2).PadLeft(4, '0');
                result.Add(new WangPatternDescriptor("角", bits, $"四角：{bits}"));
            }
        }

        return result;
    }

    private static string NormalizePatternKey(IReadOnlyList<int> wangId, string setType)
    {
        static char BitFor(IReadOnlyList<int> values, int index)
            => index < values.Count && values[index] > 0 ? '1' : '0';

        if (setType == TilesetWangSetTypes.Edge)
        {
            return string.Concat(BitFor(wangId, 0), BitFor(wangId, 2), BitFor(wangId, 4), BitFor(wangId, 6));
        }

        if (setType == TilesetWangSetTypes.Corner)
        {
            return string.Concat(BitFor(wangId, 1), BitFor(wangId, 3), BitFor(wangId, 5), BitFor(wangId, 7));
        }

        return string.Concat(
            BitFor(wangId, 0), BitFor(wangId, 1), BitFor(wangId, 2), BitFor(wangId, 3),
            BitFor(wangId, 4), BitFor(wangId, 5), BitFor(wangId, 6), BitFor(wangId, 7));
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
                Height = v.Height,
                Animated = v.Animated,
                AnimationFrameDurationMs = v.AnimationFrameDurationMs,
                AnimationFrames = v.AnimationFrames?
                    .Select(frame => new TilesetFrameDefinition
                    {
                        TileX = frame.TileX,
                        TileY = frame.TileY,
                        DurationMs = frame.DurationMs
                    })
                    .ToList() ?? []
            })
            .ToList();
    }

    private static List<TilesetTileMetadataDefinition> CloneTiles(IEnumerable<TilesetTileMetadataDefinition> tiles)
    {
        return tiles
            .Select(tile => new TilesetTileMetadataDefinition
            {
                TileX = tile.TileX,
                TileY = tile.TileY,
                DisplayName = tile.DisplayName,
                Category = tile.Category,
                Walkable = tile.Walkable,
                BlocksSight = tile.BlocksSight,
                MoveCost = tile.MoveCost,
                MaterialTag = tile.MaterialTag,
                FootstepSoundId = tile.FootstepSoundId,
                Tags = tile.Tags?.ToList() ?? [],
                CustomProperties = tile.CustomProperties is null
                    ? []
                    : new Dictionary<string, string>(tile.CustomProperties, StringComparer.OrdinalIgnoreCase),
                CollisionShapes = tile.CollisionShapes?
                    .Select(shape => new TileCollisionShapeDefinition
                    {
                        ShapeType = shape.ShapeType,
                        X = shape.X,
                        Y = shape.Y,
                        Width = shape.Width,
                        Height = shape.Height,
                        Tag = shape.Tag,
                        Points = shape.Points?
                            .Select(point => new TileCollisionPointDefinition { X = point.X, Y = point.Y })
                            .ToList() ?? []
                    })
                    .ToList() ?? []
            })
            .ToList();
    }

    private static TilesetAdvancedPlanDefinition CloneAdvanced(TilesetAdvancedPlanDefinition? advanced)
    {
        return new TilesetAdvancedPlanDefinition
        {
            AllowFlipHorizontally = advanced?.AllowFlipHorizontally ?? false,
            AllowFlipVertically = advanced?.AllowFlipVertically ?? false,
            AllowRotate = advanced?.AllowRotate ?? false,
            PreferUntransformedTiles = advanced?.PreferUntransformedTiles ?? true,
            WangSets = advanced?.WangSets?
                .Select(set => new TilesetWangSetDefinition
                {
                    Id = set.Id,
                    Name = set.Name,
                    Type = set.Type,
                    TileX = set.TileX,
                    TileY = set.TileY,
                    Colors = set.Colors?
                        .Select(color => new TilesetWangColorDefinition
                        {
                            Index = color.Index,
                            Name = color.Name,
                            ColorHex = color.ColorHex,
                            Probability = color.Probability,
                            TileX = color.TileX,
                            TileY = color.TileY
                        })
                        .ToList() ?? [],
                    Tiles = set.Tiles?
                        .Select(tile => new TilesetWangTileDefinition
                        {
                            TileX = tile.TileX,
                            TileY = tile.TileY,
                            Probability = tile.Probability,
                            WangId = tile.WangId?.Take(8).Concat(Enumerable.Repeat(0, 8)).Take(8).ToList() ?? [0, 0, 0, 0, 0, 0, 0, 0]
                        })
                        .ToList() ?? []
                })
                .ToList() ?? []
        };
    }
}

internal sealed record A4Row(int Y, int Height, string Variant);

internal sealed record WangPatternDescriptor(string Kind, string Key, string Description);

internal sealed record WangPatternSnapshot(string Kind, string Key, string Description, bool Present, int Count);

internal readonly record struct GridPoint(int X, int Y);

internal sealed class TilesetPlannerCanvas : Panel
{
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 8f;
    private const float MinVisibleImagePixels = 64f;
    private Image? _image;
    private int _tileSize = 32;
    private List<TilesetRegionDefinition> _regions = [];
    private TilesetAdvancedPlanDefinition? _advanced;
    private IReadOnlyList<TilesetTileMetadataDefinition> _tiles = [];
    private Rectangle _selection = Rectangle.Empty;
    private TilesetRegionDefinition? _selectedRegion;
    private TilesetWangSetDefinition? _activeWangSet;
    private TilesetWangColorDefinition? _activeWangColor;
    private bool _dragging;
    private bool _panning;
    private bool _wangPainting;
    private bool _wangPaintClearing;
    private Point _dragStart;
    private Point _panStart;
    private PointF _viewStart;
    private PointF _viewOffset;
    private float _zoom = 1f;
    private Bitmap? _renderBuffer;
    private readonly HashSet<string> _paintedWangPositions = [];

    public event EventHandler<TilesetPlanSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler? WangTileEdited;
    public event EventHandler<TilesetPlanTileActivatedEventArgs>? TileDoubleClicked;
    public event EventHandler<WangTileActivatedEventArgs>? WangTileDoubleClicked;

    public Rectangle Selection => _selection;

    public bool WangEditEnabled { get; set; }

    public TilesetWangSetDefinition? ActiveWangSet
    {
        get => _activeWangSet;
        set
        {
            _activeWangSet = value;
            Invalidate();
        }
    }

    public TilesetWangColorDefinition? ActiveWangColor
    {
        get => _activeWangColor;
        set
        {
            _activeWangColor = value;
            Invalidate();
        }
    }

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
        SetStyle(
            ControlStyles.Selectable
            | ControlStyles.UserPaint
            | ControlStyles.AllPaintingInWmPaint
            | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.Opaque,
            true);
        DoubleBuffered = true;
        AutoScroll = false;
        ResizeRedraw = true;
        TabStop = true;
        BackColor = Color.FromArgb(31, 31, 31);
        UpdateStyles();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _renderBuffer?.Dispose();
            _renderBuffer = null;
        }

        base.Dispose(disposing);
    }

    public void SetTileset(Image image, int tileSize, List<TilesetRegionDefinition> regions)
    {
        _image = image;
        _tileSize = Math.Max(8, tileSize);
        _regions = regions;
        EnsureViewInBounds();
        Invalidate();
    }

    public void SetAdvancedPlan(TilesetAdvancedPlanDefinition advanced)
    {
        _advanced = advanced;
        Invalidate();
    }

    public void SetTileMetadata(IReadOnlyList<TilesetTileMetadataDefinition> tiles)
    {
        _tiles = tiles;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (_image is null)
        {
            return;
        }

        if (e.Button is MouseButtons.Right or MouseButtons.Middle)
        {
            Capture = true;
            _panning = true;
            _panStart = e.Location;
            _viewStart = _viewOffset;
            Cursor = Cursors.SizeAll;
            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var tile = HitTile(e.Location);
        if (tile.X < 0 || tile.Y < 0)
        {
            return;
        }

        if (WangEditEnabled)
        {
            BeginWangPaint(tile, e.Location);
            return;
        }

        _dragging = true;
        Capture = true;
        _dragStart = tile;
        _selection = new Rectangle(tile.X, tile.Y, 1, 1);
        SelectionChanged?.Invoke(this, new TilesetPlanSelectionChangedEventArgs(_selection));
        RedrawNow();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_panning)
        {
            _viewOffset = new PointF(
                _viewStart.X + e.X - _panStart.X,
                _viewStart.Y + e.Y - _panStart.Y);
            EnsureViewInBounds();
            Invalidate();
            return;
        }

        if (WangEditEnabled)
        {
            if (_wangPainting && e.Button == MouseButtons.Left)
            {
                var wangTilePoint = HitTile(e.Location);
                if (wangTilePoint.X < 0 || wangTilePoint.Y < 0)
                {
                    return;
                }

                _selection = new Rectangle(wangTilePoint.X, wangTilePoint.Y, 1, 1);
                SelectionChanged?.Invoke(this, new TilesetPlanSelectionChangedEventArgs(_selection));
                if (ApplyWangPaint(wangTilePoint, e.Location, _wangPaintClearing))
                {
                    RedrawNow();
                }
            }

            return;
        }

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
        RedrawNow();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button is MouseButtons.Right or MouseButtons.Middle)
        {
            _panning = false;
            Cursor = Cursors.Default;
        }

        if (e.Button == MouseButtons.Left)
        {
            _wangPainting = false;
            _paintedWangPositions.Clear();
        }

        _dragging = false;
        if (!_panning && !_wangPainting && !_dragging)
        {
            Capture = false;
        }
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        if (Capture)
        {
            return;
        }

        _panning = false;
        _wangPainting = false;
        _dragging = false;
        _paintedWangPositions.Clear();
        Cursor = Cursors.Default;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (_image is null || e.Delta == 0)
        {
            return;
        }

        var clientPoint = e.Location;
        var oldZoom = _zoom;
        var factor = e.Delta > 0 ? 1.15f : 1f / 1.15f;
        _zoom = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
        if (Math.Abs(_zoom - oldZoom) < 0.001f)
        {
            return;
        }

        var imageX = (clientPoint.X - _viewOffset.X) / oldZoom;
        var imageY = (clientPoint.Y - _viewOffset.Y) / oldZoom;
        _viewOffset = new PointF(
            clientPoint.X - imageX * _zoom,
            clientPoint.Y - imageY * _zoom);
        EnsureViewInBounds();
        if (_panning)
        {
            _panStart = clientPoint;
            _viewStart = _viewOffset;
        }

        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // OnPaint clears the full surface; suppressing background erase avoids visible flicker while zooming.
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Focus();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (!WangEditEnabled || _image is null || e.Button != MouseButtons.Left)
        {
            if (!WangEditEnabled && _image is not null && e.Button == MouseButtons.Left)
            {
                var normalTile = HitTile(e.Location);
                if (normalTile.X >= 0 && normalTile.Y >= 0)
                {
                    TileDoubleClicked?.Invoke(this, new TilesetPlanTileActivatedEventArgs(normalTile.X, normalTile.Y));
                }
            }

            return;
        }

        var tile = HitTile(e.Location);
        if (tile.X < 0 || tile.Y < 0)
        {
            return;
        }

        WangTileDoubleClicked?.Invoke(this, new WangTileActivatedEventArgs(tile.X, tile.Y));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        EnsureRenderBuffer();
        if (_renderBuffer is null)
        {
            return;
        }

        using (var g = Graphics.FromImage(_renderBuffer))
        {
            PaintCanvas(g);
        }

        e.Graphics.DrawImageUnscaled(_renderBuffer, 0, 0);
    }

    private void PaintCanvas(Graphics g)
    {
        g.Clear(BackColor);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

        if (_image is null)
        {
            return;
        }

        var imageRect = ImageRectToScreen(new RectangleF(0, 0, _image.Width, _image.Height));
        g.DrawImage(_image, imageRect);
        DrawRegions(g);
        DrawTileMetadataMarkers(g);
        DrawGrid(g);
        DrawWangTiles(g);
        DrawSelection(g);
    }

    private Point HitTile(Point point)
    {
        if (_image is null)
        {
            return new Point(-1, -1);
        }

        var x = ScreenToImageX(point.X);
        var y = ScreenToImageY(point.Y);
        if (x < 0 || y < 0 || x >= _image.Width || y >= _image.Height)
        {
            return new Point(-1, -1);
        }

        return new Point((int)(x / _tileSize), (int)(y / _tileSize));
    }

    private void DrawGrid(Graphics g)
    {
        if (_image is null)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(95, 255, 255, 255), ScaledLineWidth(1f));
        for (var x = 0; x <= _image.Width; x += _tileSize)
        {
            var scaledX = ImageToScreenX(x);
            g.DrawLine(pen, scaledX, ImageToScreenY(0), scaledX, ImageToScreenY(_image.Height));
        }

        for (var y = 0; y <= _image.Height; y += _tileSize)
        {
            var scaledY = ImageToScreenY(y);
            g.DrawLine(pen, ImageToScreenX(0), scaledY, ImageToScreenX(_image.Width), scaledY);
        }
    }

    private void DrawRegions(Graphics g)
    {
        if (WangEditEnabled)
        {
            return;
        }

        foreach (var region in _regions)
        {
            var rect = RegionToPixels(region);
            var color = RegionColor(region.Kind);
            var selected = ReferenceEquals(region, _selectedRegion);
            var ignored = string.Equals(region.Kind, TilesetRegionKinds.Ignored, StringComparison.OrdinalIgnoreCase);
            var hidden = string.Equals(region.Kind, TilesetRegionKinds.Hidden, StringComparison.OrdinalIgnoreCase);
            var borderColor = selected ? Color.FromArgb(255, 0, 190, 220) : color;
            using var fill = new SolidBrush(Color.FromArgb(selected ? 58 : ignored ? 18 : hidden ? 24 : 36, selected ? borderColor : color));
            using var pen = new Pen(borderColor, Math.Max(1f, Math.Min(selected ? 3f : 1.5f, ScaledLineWidth(selected ? 2f : 1f))));
            if ((ignored || hidden) && !selected)
            {
                pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
            }

            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect);
            DrawRegionLabel(g, rect, region);
        }
    }

    private void DrawWangTiles(Graphics g)
    {
        if (_advanced?.WangSets is null)
        {
            return;
        }

        foreach (var set in _advanced.WangSets)
        {
            var selectedSet = ReferenceEquals(set, _activeWangSet);
            foreach (var tile in set.Tiles)
            {
                var rect = ImageRectToScreen(new RectangleF(
                    tile.TileX * _tileSize,
                    tile.TileY * _tileSize,
                    _tileSize,
                    _tileSize));
                DrawWangTile(g, rect, set, tile, selectedSet);
            }
        }
    }

    private void DrawTileMetadataMarkers(Graphics g)
    {
        if (_tiles.Count <= 0 || _image is null)
        {
            return;
        }

        foreach (var tile in _tiles)
        {
            if (tile.TileX < 0 || tile.TileY < 0)
            {
                continue;
            }

            if (!TileHasVisibleMetadataMarker(tile))
            {
                continue;
            }

            var rect = ImageRectToScreen(new RectangleF(
                tile.TileX * _tileSize,
                tile.TileY * _tileSize,
                _tileSize,
                _tileSize));
            if (rect.Width < 8 || rect.Height < 8)
            {
                continue;
            }

            DrawTileMetadataMarker(g, rect, tile.CollisionShapes.Count > 0);
        }
    }

    private static bool TileHasVisibleMetadataMarker(TilesetTileMetadataDefinition tile)
    {
        return tile.TileX >= 0
            && tile.TileY >= 0
            && (tile.CollisionShapes.Count > 0
                || !string.IsNullOrWhiteSpace(tile.DisplayName)
                || !string.IsNullOrWhiteSpace(tile.Category)
                || tile.Tags.Count > 0
                || tile.CustomProperties.Count > 0
                || !tile.Walkable
                || tile.BlocksSight
                || tile.MoveCost is not null);
    }

    private static void DrawTileMetadataMarker(Graphics g, RectangleF rect, bool hasCollision)
    {
        var color = hasCollision ? Color.FromArgb(230, 220, 40, 40) : Color.FromArgb(220, 45, 105, 200);
        var text = hasCollision ? "C" : "A";
        var badgeSize = Math.Max(14f, Math.Min(22f, Math.Min(rect.Width, rect.Height) * 0.38f));
        var badge = new RectangleF(rect.Right - badgeSize - 3, rect.Top + 3, badgeSize, badgeSize);
        using var back = new SolidBrush(color);
        using var outline = new Pen(Color.White, Math.Max(1f, badgeSize * 0.08f));
        using var font = new Font(SystemFonts.DefaultFont.FontFamily, Math.Max(7f, badgeSize * 0.56f), FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.FillEllipse(back, badge);
        g.DrawEllipse(outline, badge);
        g.DrawString(text, font, textBrush, badge, format);
    }

    private static void DrawWangTile(Graphics g, RectangleF rect, TilesetWangSetDefinition set, TilesetWangTileDefinition tile, bool selectedSet)
    {
        var tileScreenSize = Math.Min(rect.Width, rect.Height);
        var colorIndexes = new int[8];
        for (var index = 0; index < Math.Min(8, tile.WangId.Count); index++)
        {
            if (!IsWangPositionEditable(index, set.Type))
            {
                continue;
            }

            colorIndexes[index] = tile.WangId[index] > 0 ? tile.WangId[index] : 0;
        }

        var previousSmoothing = g.SmoothingMode;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        foreach (var colorIndex in colorIndexes.Where(v => v > 0).Distinct())
        {
            var colorHex = set.Colors.FirstOrDefault(color => color.Index == colorIndex)?.ColorHex ?? "#22c55e";
            var color = ParseColorOrDefault(colorHex);
            using var path = BuildWangColorPath(rect, colorIndexes, colorIndex);
            using var brush = new SolidBrush(Color.FromArgb(selectedSet ? 146 : 112, color));
            using var outline = new Pen(selectedSet ? Color.FromArgb(230, 190, 0, 0) : Darken(color), Math.Max(1f, tileScreenSize * 0.035f));
            g.FillPath(brush, path);
            g.DrawPath(outline, path);
        }

        g.SmoothingMode = previousSmoothing;
    }

    private void BeginWangPaint(Point tile, Point mouseLocation)
    {
        if (_activeWangSet is null)
        {
            return;
        }

        var position = HitWangPosition(tile, mouseLocation);
        if (position < 0)
        {
            return;
        }

        var value = _activeWangColor?.Index ?? 1;
        var indexes = WangIndexesForPosition(position, _activeWangSet.Type).Distinct().ToArray();
        var wangTile = _activeWangSet.Tiles.FirstOrDefault(v => v.TileX == tile.X && v.TileY == tile.Y);
        _wangPaintClearing = indexes.Length > 0 && wangTile is not null && indexes.All(index => index < wangTile.WangId.Count && wangTile.WangId[index] == value);
        _wangPainting = true;
        Capture = true;
        _paintedWangPositions.Clear();
        _selection = new Rectangle(tile.X, tile.Y, 1, 1);
        SelectionChanged?.Invoke(this, new TilesetPlanSelectionChangedEventArgs(_selection));
        if (ApplyWangPaint(tile, mouseLocation, _wangPaintClearing))
        {
            RedrawNow();
        }
    }

    private bool ApplyWangPaint(Point tile, Point mouseLocation, bool clear)
    {
        if (_activeWangSet is null)
        {
            return false;
        }

        var position = HitWangPosition(tile, mouseLocation);
        if (position < 0)
        {
            return false;
        }

        var paintKey = $"{tile.X}:{tile.Y}:{position}";
        if (!_paintedWangPositions.Add(paintKey))
        {
            return false;
        }

        var indexes = WangIndexesForPosition(position, _activeWangSet.Type).Distinct().ToArray();
        if (indexes.Length <= 0)
        {
            return false;
        }

        var value = _activeWangColor?.Index ?? 1;
        var wangTile = _activeWangSet.Tiles.FirstOrDefault(v => v.TileX == tile.X && v.TileY == tile.Y);
        if (wangTile is null)
        {
            if (clear)
            {
                return false;
            }

            wangTile = new TilesetWangTileDefinition
            {
                TileX = tile.X,
                TileY = tile.Y,
                WangId = [0, 0, 0, 0, 0, 0, 0, 0]
            };
            _activeWangSet.Tiles.Add(wangTile);
        }

        while (wangTile.WangId.Count < 8)
        {
            wangTile.WangId.Add(0);
        }

        var changed = false;
        foreach (var index in indexes)
        {
            var nextValue = clear ? 0 : value;
            if (wangTile.WangId[index] == nextValue)
            {
                continue;
            }

            wangTile.WangId[index] = nextValue;
            changed = true;
        }

        if (wangTile.WangId.All(v => v == 0))
        {
            _activeWangSet.Tiles.Remove(wangTile);
        }

        if (changed)
        {
            WangTileEdited?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }

        return changed;
    }

    private int HitWangPosition(Point tile, Point mouseLocation)
    {
        var localX = ScreenToImageX(mouseLocation.X) - tile.X * _tileSize;
        var localY = ScreenToImageY(mouseLocation.Y) - tile.Y * _tileSize;
        var column = localX < _tileSize / 3f
            ? 0
            : localX >= _tileSize * 2f / 3f
                ? 2
                : 1;
        var row = localY < _tileSize / 3f
            ? 0
            : localY >= _tileSize * 2f / 3f
                ? 2
                : 1;

        return (column, row) switch
        {
            (1, 0) => 0,
            (2, 0) => 1,
            (2, 1) => 2,
            (2, 2) => 3,
            (1, 2) => 4,
            (0, 2) => 5,
            (0, 1) => 6,
            (0, 0) => 7,
            _ => -1
        };
    }

    private static IEnumerable<int> WangIndexesForPosition(int position, string setType)
    {
        if (position < 0)
        {
            yield break;
        }

        if (IsWangPositionEditable(position, setType))
        {
            yield return position;
        }
    }

    private static bool IsWangPositionEditable(int position, string setType)
    {
        return setType switch
        {
            TilesetWangSetTypes.Corner => position % 2 == 1,
            TilesetWangSetTypes.Edge => position % 2 == 0,
            _ => position >= 0 && position < 8
        };
    }

    private static System.Drawing.Drawing2D.GraphicsPath BuildWangColorPath(RectangleF rect, int[] colorIndexes, int colorIndex)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var mask = new bool[3, 3];
        var hasTop = colorIndexes[0] == colorIndex;
        var hasTopRight = colorIndexes[1] == colorIndex;
        var hasRight = colorIndexes[2] == colorIndex;
        var hasBottomRight = colorIndexes[3] == colorIndex;
        var hasBottom = colorIndexes[4] == colorIndex;
        var hasBottomLeft = colorIndexes[5] == colorIndex;
        var hasLeft = colorIndexes[6] == colorIndex;
        var hasTopLeft = colorIndexes[7] == colorIndex;
        var edgeCount = new[] { hasTop, hasRight, hasBottom, hasLeft }.Count(v => v);
        mask[1, 1] = edgeCount >= 2;
        mask[1, 0] = hasTop;
        mask[2, 1] = hasRight;
        mask[1, 2] = hasBottom;
        mask[0, 1] = hasLeft;
        mask[0, 0] = hasTopLeft;
        mask[2, 0] = hasTopRight;
        mask[2, 2] = hasBottomRight;
        mask[0, 2] = hasBottomLeft;

        foreach (var loop in TraceMaskLoops(mask))
        {
            AddRoundedLoop(path, loop, rect);
        }

        return path;
    }

    private static List<List<GridPoint>> TraceMaskLoops(bool[,] mask)
    {
        var edges = new List<(GridPoint From, GridPoint To)>();
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                if (!mask[x, y])
                {
                    continue;
                }

                if (y <= 0 || !mask[x, y - 1])
                {
                    edges.Add((new GridPoint(x, y), new GridPoint(x + 1, y)));
                }

                if (x >= 2 || !mask[x + 1, y])
                {
                    edges.Add((new GridPoint(x + 1, y), new GridPoint(x + 1, y + 1)));
                }

                if (y >= 2 || !mask[x, y + 1])
                {
                    edges.Add((new GridPoint(x + 1, y + 1), new GridPoint(x, y + 1)));
                }

                if (x <= 0 || !mask[x - 1, y])
                {
                    edges.Add((new GridPoint(x, y + 1), new GridPoint(x, y)));
                }
            }
        }

        var loops = new List<List<GridPoint>>();
        while (edges.Count > 0)
        {
            var edge = edges[0];
            edges.RemoveAt(0);
            var loop = new List<GridPoint> { edge.From };
            var current = edge.To;
            var guard = 0;
            while (current != loop[0] && guard++ < 64)
            {
                loop.Add(current);
                var nextIndex = edges.FindIndex(v => v.From == current);
                if (nextIndex < 0)
                {
                    break;
                }

                var next = edges[nextIndex];
                edges.RemoveAt(nextIndex);
                current = next.To;
            }

            if (loop.Count >= 4 && current == loop[0])
            {
                loops.Add(loop);
            }
        }

        return loops;
    }

    private static void AddRoundedLoop(System.Drawing.Drawing2D.GraphicsPath path, IReadOnlyList<GridPoint> loop, RectangleF rect)
    {
        var points = loop.Select(point => GridToScreen(point, rect)).ToArray();
        var radius = Math.Min(rect.Width, rect.Height) / 6f;
        var before = new PointF[points.Length];
        var after = new PointF[points.Length];
        for (var i = 0; i < points.Length; i++)
        {
            var previous = points[(i - 1 + points.Length) % points.Length];
            var current = points[i];
            var next = points[(i + 1) % points.Length];
            var previousLength = Distance(current, previous);
            var nextLength = Distance(current, next);
            var cornerRadius = Math.Min(radius, Math.Min(previousLength, nextLength) * 0.45f);
            before[i] = MoveTowards(current, previous, cornerRadius);
            after[i] = MoveTowards(current, next, cornerRadius);
        }

        path.StartFigure();
        path.AddLine(after[0], before[1 % points.Length]);
        for (var i = 1; i < points.Length; i++)
        {
            path.AddBezier(before[i], points[i], points[i], after[i]);
            path.AddLine(after[i], before[(i + 1) % points.Length]);
        }

        path.AddBezier(before[0], points[0], points[0], after[0]);
        path.CloseFigure();
    }

    private static PointF GridToScreen(GridPoint point, RectangleF rect)
        => new(rect.Left + rect.Width * point.X / 3f, rect.Top + rect.Height * point.Y / 3f);

    private static float Distance(PointF from, PointF to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static PointF MoveTowards(PointF from, PointF to, float distance)
    {
        var length = Distance(from, to);
        if (length <= 0.001f)
        {
            return from;
        }

        var ratio = Math.Min(1f, distance / length);
        return new PointF(from.X + (to.X - from.X) * ratio, from.Y + (to.Y - from.Y) * ratio);
    }

    private static RectangleF InsetRect(RectangleF rect, float inset)
        => new(rect.Left + inset, rect.Top + inset, Math.Max(1f, rect.Width - inset * 2f), Math.Max(1f, rect.Height - inset * 2f));

    private static Color ParseColorOrDefault(string? hex)
    {
        try
        {
            return string.IsNullOrWhiteSpace(hex) ? Color.FromArgb(255, 34, 197, 94) : ColorTranslator.FromHtml(hex);
        }
        catch
        {
            return Color.FromArgb(255, 34, 197, 94);
        }
    }

    private static Color Darken(Color color)
        => Color.FromArgb(230, Math.Max(0, color.R - 80), Math.Max(0, color.G - 80), Math.Max(0, color.B - 80));

    private void DrawSelection(Graphics g)
    {
        if (_selection.Width <= 0 || _selection.Height <= 0)
        {
            return;
        }

        var rect = ImageRectToScreen(new RectangleF(
            _selection.X * _tileSize,
            _selection.Y * _tileSize,
            _selection.Width * _tileSize,
            _selection.Height * _tileSize));
        using var fill = new SolidBrush(Color.FromArgb(36, 0, 120, 215));
        using var pen = new Pen(Color.FromArgb(255, 0, 120, 215), Math.Max(1f, Math.Min(3f, _zoom * 1.5f)));
        g.FillRectangle(fill, rect);
        g.DrawRectangle(pen, rect);
    }

    private RectangleF RegionToPixels(TilesetRegionDefinition region)
    {
        return ImageRectToScreen(new RectangleF(
            region.X * _tileSize,
            region.Y * _tileSize,
            region.Width * _tileSize,
            region.Height * _tileSize));
    }

    private void EnsureViewInBounds()
    {
        if (_image is null)
        {
            return;
        }

        var scaledWidth = _image.Width * _zoom;
        var scaledHeight = _image.Height * _zoom;
        _viewOffset.X = ClampViewOffset(_viewOffset.X, scaledWidth, ClientSize.Width);
        _viewOffset.Y = ClampViewOffset(_viewOffset.Y, scaledHeight, ClientSize.Height);
    }

    private static float ClampViewOffset(float offset, float scaledLength, int viewportLength)
    {
        if (scaledLength <= 0 || viewportLength <= 0)
        {
            return 0f;
        }

        var visible = Math.Min(MinVisibleImagePixels, scaledLength);
        var min = -scaledLength + visible;
        var max = viewportLength - visible;
        return Math.Clamp(offset, min, max);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        _renderBuffer?.Dispose();
        _renderBuffer = null;
        EnsureViewInBounds();
    }

    private void EnsureRenderBuffer()
    {
        var width = Math.Max(1, ClientSize.Width);
        var height = Math.Max(1, ClientSize.Height);
        if (_renderBuffer is not null && _renderBuffer.Width == width && _renderBuffer.Height == height)
        {
            return;
        }

        _renderBuffer?.Dispose();
        _renderBuffer = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
    }

    private void RedrawNow()
    {
        Invalidate();
        Update();
    }

    private float ScaledLineWidth(float baseWidth)
        => Math.Max(0.75f, baseWidth * _zoom);

    private static float ScaledLineWidth(float baseWidth, RectangleF rect)
        => Math.Max(0.75f, Math.Min(rect.Width, rect.Height) * baseWidth / 32f);

    private float ImageToScreenX(float imageX) => _viewOffset.X + imageX * _zoom;

    private float ImageToScreenY(float imageY) => _viewOffset.Y + imageY * _zoom;

    private float ScreenToImageX(float screenX) => (screenX - _viewOffset.X) / _zoom;

    private float ScreenToImageY(float screenY) => (screenY - _viewOffset.Y) / _zoom;

    private RectangleF ImageRectToScreen(RectangleF rect)
        => new(ImageToScreenX(rect.X), ImageToScreenY(rect.Y), rect.Width * _zoom, rect.Height * _zoom);

    private static Color RegionColor(string kind)
    {
        return kind switch
        {
            TilesetRegionKinds.RpgMakerA1 => Color.FromArgb(255, 78, 172, 255),
            TilesetRegionKinds.RpgMakerA2 => Color.FromArgb(255, 229, 126, 32),
            TilesetRegionKinds.RpgMakerA3 => Color.FromArgb(255, 186, 104, 200),
            TilesetRegionKinds.RpgMakerA4 => Color.FromArgb(255, 121, 134, 203),
            TilesetRegionKinds.Ignored => Color.FromArgb(255, 145, 145, 145),
            TilesetRegionKinds.Hidden => Color.FromArgb(255, 96, 125, 139),
            _ => Color.FromArgb(255, 38, 166, 91)
        };
    }

    private static void DrawRegionLabel(Graphics g, RectangleF rect, TilesetRegionDefinition region)
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
                        : region.Kind == TilesetRegionKinds.Ignored
                            ? "-"
                            : region.Kind == TilesetRegionKinds.Hidden ? "隐藏" : "普通";
        var size = g.MeasureString(text, font);
        var labelHeight = size.Height + 4;
        var labelRect = new RectangleF(rect.Left + 4, rect.Bottom - labelHeight - 4, size.Width + 8, labelHeight);
        if (labelRect.Top < rect.Top + 2)
        {
            return;
        }

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

internal enum TileCollisionEditTool
{
    Select,
    Rectangle,
    Ellipse,
    Polygon
}

internal sealed class TileCollisionEditorCanvas : Panel
{
    private readonly Image _image;
    private readonly int _tileSize;
    private readonly TilesetTileMetadataDefinition _tile;
    private readonly List<TileCollisionPointDefinition> _polygonDraft = [];
    private TileCollisionShapeDefinition? _selectedShape;
    private TileCollisionShapeDefinition? _draftShape;
    private TileCollisionEditTool _activeTool;
    private bool _dragging;
    private CollisionDragMode _dragMode;
    private int _activeHandle = -1;
    private int _activePointIndex = -1;
    private PointF _dragStart;
    private PointF _shapeStart;
    private PointF _shapeSizeStart;
    private PointF _hoverPoint;

    public TileCollisionEditorCanvas(Image image, int tileSize, TilesetTileMetadataDefinition tile)
    {
        _image = image;
        _tileSize = Math.Max(8, tileSize);
        _tile = tile;
        DoubleBuffered = true;
        ResizeRedraw = true;
        TabStop = true;
        BackColor = SystemColors.Control;
    }

    public event EventHandler<TileCollisionShapeDefinition?>? SelectionChanged;

    public event EventHandler? ShapesChanged;

    public TileCollisionEditTool ActiveTool
    {
        get => _activeTool;
        set
        {
            if (_activeTool == value)
            {
                return;
            }

            FinishPolygonDraft();
            _activeTool = value;
            Cursor = _activeTool == TileCollisionEditTool.Select ? Cursors.Default : Cursors.Cross;
            Invalidate();
        }
    }

    public TileCollisionShapeDefinition? SelectedShape
    {
        get => _selectedShape;
        set
        {
            if (ReferenceEquals(_selectedShape, value))
            {
                return;
            }

            _selectedShape = value;
            SelectionChanged?.Invoke(this, _selectedShape);
            Invalidate();
        }
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData == Keys.Delete || base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode != Keys.Delete || SelectedShape is null)
        {
            return;
        }

        _tile.CollisionShapes.Remove(SelectedShape);
        SelectedShape = null;
        ShapesChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        if (e.Button == MouseButtons.Right && _activeTool == TileCollisionEditTool.Polygon)
        {
            FinishPolygonDraft();
            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (!TryScreenToTile(e.Location, out var tilePoint))
        {
            return;
        }

        if (_activeTool == TileCollisionEditTool.Rectangle || _activeTool == TileCollisionEditTool.Ellipse)
        {
            _dragging = true;
            _dragMode = CollisionDragMode.DrawShape;
            _dragStart = tilePoint;
            _draftShape = new TileCollisionShapeDefinition
            {
                ShapeType = _activeTool == TileCollisionEditTool.Rectangle ? TileCollisionShapeTypes.Rectangle : TileCollisionShapeTypes.Ellipse,
                X = tilePoint.X,
                Y = tilePoint.Y,
                Width = 0.01f,
                Height = 0.01f
            };
            SelectedShape = null;
            Invalidate();
            return;
        }

        if (_activeTool == TileCollisionEditTool.Polygon)
        {
            if (e.Clicks > 1)
            {
                FinishPolygonDraft();
                return;
            }

            AddPolygonDraftPoint(tilePoint);
            return;
        }

        StartSelectionDrag(tilePoint);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var inside = TryScreenToTile(e.Location, out var tilePoint);
        if (inside)
        {
            _hoverPoint = tilePoint;
        }

        if (!_dragging || !inside)
        {
            if (_activeTool == TileCollisionEditTool.Polygon && _polygonDraft.Count > 0)
            {
                Invalidate();
            }

            return;
        }

        switch (_dragMode)
        {
            case CollisionDragMode.DrawShape:
                UpdateDraftShape(tilePoint);
                break;
            case CollisionDragMode.MoveShape:
                MoveSelectedShape(tilePoint);
                break;
            case CollisionDragMode.ResizeShape:
                ResizeSelectedShape(tilePoint);
                break;
            case CollisionDragMode.MovePolygonPoint:
                MovePolygonPoint(tilePoint);
                break;
        }

        Invalidate();
        ShapesChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_dragMode == CollisionDragMode.DrawShape && _draftShape is not null)
        {
            NormalizeShapeBounds(_draftShape);
            if (_draftShape.Width >= 0.02f && _draftShape.Height >= 0.02f)
            {
                _tile.CollisionShapes.Add(_draftShape);
                SelectedShape = _draftShape;
                ShapesChanged?.Invoke(this, EventArgs.Empty);
            }

            _draftShape = null;
        }

        _dragging = false;
        _dragMode = CollisionDragMode.None;
        _activeHandle = -1;
        _activePointIndex = -1;
        Invalidate();
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (_activeTool == TileCollisionEditTool.Polygon)
        {
            FinishPolygonDraft();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        TextRenderer.DrawText(g, "当前瓦片碰撞", Font, new Rectangle(0, 6, Width, 22), SystemColors.ControlText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        var tileRect = TileDisplayRect();
        DrawTile(g, tileRect);
        DrawExistingShapes(g, tileRect);
        if (_draftShape is not null)
        {
            DrawShape(g, tileRect, _draftShape, true, true);
        }

        DrawPolygonDraft(g, tileRect);
        TextRenderer.DrawText(g, ToolHintText(), Font, new Rectangle(8, Height - 44, Math.Max(20, Width - 16), 40), SystemColors.GrayText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }

    private void DrawTile(Graphics g, RectangleF tileRect)
    {
        using var back = new SolidBrush(Color.FromArgb(24, 0, 0, 0));
        using var border = new Pen(SystemColors.ControlDark);
        g.FillRectangle(back, tileRect);
        var source = new Rectangle(_tile.TileX * _tileSize, _tile.TileY * _tileSize, _tileSize, _tileSize);
        if (source.Right <= _image.Width && source.Bottom <= _image.Height)
        {
            var state = g.Save();
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(_image, tileRect, source, GraphicsUnit.Pixel);
            g.Restore(state);
        }

        g.DrawRectangle(border, tileRect.X, tileRect.Y, tileRect.Width, tileRect.Height);
    }

    private void DrawExistingShapes(Graphics g, RectangleF tileRect)
    {
        foreach (var shape in _tile.CollisionShapes)
        {
            DrawShape(g, tileRect, shape, ReferenceEquals(shape, SelectedShape), false);
        }
    }

    private void DrawShape(Graphics g, RectangleF tileRect, TileCollisionShapeDefinition shape, bool selected, bool preview)
    {
        var borderColor = selected ? Color.FromArgb(255, 0, 120, 215) : Color.FromArgb(235, 230, 80, 70);
        var fillColor = preview ? Color.FromArgb(54, borderColor) : Color.FromArgb(selected ? 70 : 46, borderColor);
        using var fill = new SolidBrush(fillColor);
        using var pen = new Pen(borderColor, selected ? 2.4f : 1.6f);

        if (string.Equals(shape.ShapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase))
        {
            var points = ShapePointsToScreen(tileRect, shape).ToArray();
            if (points.Length >= 3)
            {
                g.FillPolygon(fill, points);
                g.DrawPolygon(pen, points);
            }

            if (selected)
            {
                foreach (var point in points)
                {
                    DrawHandle(g, point);
                }
            }

            return;
        }

        var rect = ShapeRectToScreen(tileRect, shape);
        if (string.Equals(shape.ShapeType, TileCollisionShapeTypes.Ellipse, StringComparison.OrdinalIgnoreCase))
        {
            g.FillEllipse(fill, rect);
            g.DrawEllipse(pen, rect);
        }
        else
        {
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        if (selected)
        {
            foreach (var handle in ShapeHandles(rect))
            {
                DrawHandle(g, handle);
            }
        }
    }

    private void DrawPolygonDraft(Graphics g, RectangleF tileRect)
    {
        if (_polygonDraft.Count <= 0)
        {
            return;
        }

        var points = _polygonDraft.Select(point => TileToScreen(tileRect, new PointF(point.X, point.Y))).ToList();
        if (TryScreenToTile(PointToClient(MousePosition), out _) && _polygonDraft.Count > 0)
        {
            points.Add(TileToScreen(tileRect, _hoverPoint));
        }

        using var pen = new Pen(Color.FromArgb(255, 0, 120, 215), 2f) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash };
        if (points.Count >= 2)
        {
            g.DrawLines(pen, points.ToArray());
        }

        foreach (var point in points)
        {
            DrawHandle(g, point);
        }
    }

    private void DrawHandle(Graphics g, PointF center)
    {
        var rect = new RectangleF(center.X - 4, center.Y - 4, 8, 8);
        using var fill = new SolidBrush(Color.White);
        using var pen = new Pen(Color.FromArgb(255, 0, 120, 215), 1.4f);
        g.FillRectangle(fill, rect);
        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
    }

    private void StartSelectionDrag(PointF tilePoint)
    {
        if (SelectedShape is not null)
        {
            if (string.Equals(SelectedShape.ShapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase)
                && HitPolygonPoint(SelectedShape, tilePoint, out var pointIndex))
            {
                _dragging = true;
                _dragMode = CollisionDragMode.MovePolygonPoint;
                _activePointIndex = pointIndex;
                return;
            }

            if (!string.Equals(SelectedShape.ShapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase)
                && HitShapeHandle(SelectedShape, tilePoint, out var handle))
            {
                _dragging = true;
                _dragMode = CollisionDragMode.ResizeShape;
                _activeHandle = handle;
                _dragStart = tilePoint;
                _shapeStart = new PointF(SelectedShape.X, SelectedShape.Y);
                _shapeSizeStart = new PointF(SelectedShape.Width, SelectedShape.Height);
                return;
            }
        }

        var hit = HitShape(tilePoint);
        SelectedShape = hit;
        if (hit is null)
        {
            return;
        }

        _dragging = true;
        _dragMode = CollisionDragMode.MoveShape;
        _dragStart = tilePoint;
        _shapeStart = new PointF(hit.X, hit.Y);
        _shapeSizeStart = new PointF(hit.Width, hit.Height);
    }

    private void AddPolygonDraftPoint(PointF tilePoint)
    {
        _polygonDraft.Add(new TileCollisionPointDefinition { X = tilePoint.X, Y = tilePoint.Y });
        Invalidate();
        if (_polygonDraft.Count >= 3 && Distance(tilePoint, new PointF(_polygonDraft[0].X, _polygonDraft[0].Y)) <= 0.035f)
        {
            _polygonDraft.RemoveAt(_polygonDraft.Count - 1);
            FinishPolygonDraft();
        }
    }

    private void FinishPolygonDraft()
    {
        if (_polygonDraft.Count >= 3)
        {
            var shape = new TileCollisionShapeDefinition
            {
                ShapeType = TileCollisionShapeTypes.Polygon,
                Points = _polygonDraft
                    .Select(point => new TileCollisionPointDefinition { X = Clamp01(point.X), Y = Clamp01(point.Y) })
                    .ToList()
            };
            _tile.CollisionShapes.Add(shape);
            SelectedShape = shape;
            ShapesChanged?.Invoke(this, EventArgs.Empty);
        }

        _polygonDraft.Clear();
        Invalidate();
    }

    private void UpdateDraftShape(PointF tilePoint)
    {
        if (_draftShape is null)
        {
            return;
        }

        _draftShape.X = Math.Min(_dragStart.X, tilePoint.X);
        _draftShape.Y = Math.Min(_dragStart.Y, tilePoint.Y);
        _draftShape.Width = Math.Abs(tilePoint.X - _dragStart.X);
        _draftShape.Height = Math.Abs(tilePoint.Y - _dragStart.Y);
    }

    private void MoveSelectedShape(PointF tilePoint)
    {
        if (SelectedShape is null)
        {
            return;
        }

        var dx = tilePoint.X - _dragStart.X;
        var dy = tilePoint.Y - _dragStart.Y;
        if (string.Equals(SelectedShape.ShapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase))
        {
            var bounds = PolygonBounds(SelectedShape);
            dx = Math.Clamp(dx, -bounds.Left, 1f - bounds.Right);
            dy = Math.Clamp(dy, -bounds.Top, 1f - bounds.Bottom);
            for (var index = 0; index < SelectedShape.Points.Count; index++)
            {
                SelectedShape.Points[index].X = Clamp01(SelectedShape.Points[index].X + dx);
                SelectedShape.Points[index].Y = Clamp01(SelectedShape.Points[index].Y + dy);
            }

            _dragStart = tilePoint;
            return;
        }

        SelectedShape.X = Clamp01(_shapeStart.X + dx);
        SelectedShape.Y = Clamp01(_shapeStart.Y + dy);
        SelectedShape.X = Math.Min(SelectedShape.X, 1f - Math.Max(0.01f, SelectedShape.Width));
        SelectedShape.Y = Math.Min(SelectedShape.Y, 1f - Math.Max(0.01f, SelectedShape.Height));
    }

    private void ResizeSelectedShape(PointF tilePoint)
    {
        if (SelectedShape is null)
        {
            return;
        }

        var left = _shapeStart.X;
        var top = _shapeStart.Y;
        var right = _shapeStart.X + _shapeSizeStart.X;
        var bottom = _shapeStart.Y + _shapeSizeStart.Y;
        if (_activeHandle is 0 or 3)
        {
            left = tilePoint.X;
        }
        if (_activeHandle is 1 or 2)
        {
            right = tilePoint.X;
        }
        if (_activeHandle is 0 or 1)
        {
            top = tilePoint.Y;
        }
        if (_activeHandle is 2 or 3)
        {
            bottom = tilePoint.Y;
        }

        SelectedShape.X = Clamp01(Math.Min(left, right));
        SelectedShape.Y = Clamp01(Math.Min(top, bottom));
        SelectedShape.Width = Math.Max(0.01f, Math.Min(1f - SelectedShape.X, Math.Abs(right - left)));
        SelectedShape.Height = Math.Max(0.01f, Math.Min(1f - SelectedShape.Y, Math.Abs(bottom - top)));
    }

    private void MovePolygonPoint(PointF tilePoint)
    {
        if (SelectedShape is null || _activePointIndex < 0 || _activePointIndex >= SelectedShape.Points.Count)
        {
            return;
        }

        SelectedShape.Points[_activePointIndex].X = Clamp01(tilePoint.X);
        SelectedShape.Points[_activePointIndex].Y = Clamp01(tilePoint.Y);
    }

    private TileCollisionShapeDefinition? HitShape(PointF tilePoint)
    {
        for (var index = _tile.CollisionShapes.Count - 1; index >= 0; index--)
        {
            var shape = _tile.CollisionShapes[index];
            if (ShapeContains(shape, tilePoint))
            {
                return shape;
            }
        }

        return null;
    }

    private static bool ShapeContains(TileCollisionShapeDefinition shape, PointF tilePoint)
    {
        if (string.Equals(shape.ShapeType, TileCollisionShapeTypes.Polygon, StringComparison.OrdinalIgnoreCase))
        {
            if (shape.Points.Count < 3)
            {
                return false;
            }

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddPolygon(shape.Points.Select(point => new PointF(point.X, point.Y)).ToArray());
            return path.IsVisible(tilePoint);
        }

        var rect = ShapeBounds(shape);
        if (!rect.Contains(tilePoint))
        {
            return false;
        }

        if (!string.Equals(shape.ShapeType, TileCollisionShapeTypes.Ellipse, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var rx = rect.Width / 2f;
        var ry = rect.Height / 2f;
        var cx = rect.Left + rx;
        var cy = rect.Top + ry;
        if (rx <= 0 || ry <= 0)
        {
            return false;
        }

        var nx = (tilePoint.X - cx) / rx;
        var ny = (tilePoint.Y - cy) / ry;
        return nx * nx + ny * ny <= 1f;
    }

    private static bool HitShapeHandle(TileCollisionShapeDefinition shape, PointF tilePoint, out int handle)
    {
        handle = -1;
        var points = ShapeHandlePoints(ShapeBounds(shape));
        for (var index = 0; index < points.Length; index++)
        {
            if (Distance(tilePoint, points[index]) <= 0.035f)
            {
                handle = index;
                return true;
            }
        }

        return false;
    }

    private static bool HitPolygonPoint(TileCollisionShapeDefinition shape, PointF tilePoint, out int pointIndex)
    {
        pointIndex = -1;
        for (var index = 0; index < shape.Points.Count; index++)
        {
            if (Distance(tilePoint, new PointF(shape.Points[index].X, shape.Points[index].Y)) <= 0.035f)
            {
                pointIndex = index;
                return true;
            }
        }

        return false;
    }

    private bool TryScreenToTile(Point point, out PointF tilePoint)
    {
        var rect = TileDisplayRect();
        if (!rect.Contains(point))
        {
            tilePoint = PointF.Empty;
            return false;
        }

        tilePoint = new PointF(
            Clamp01((point.X - rect.Left) / rect.Width),
            Clamp01((point.Y - rect.Top) / rect.Height));
        return true;
    }

    private RectangleF TileDisplayRect()
    {
        var availableWidth = Math.Max(1, Width - 28);
        var availableHeight = Math.Max(1, Height - 92);
        var size = Math.Min(availableWidth, availableHeight);
        var left = (Width - size) / 2f;
        var top = 34f + (availableHeight - size) / 2f;
        return new RectangleF(left, top, size, size);
    }

    private static RectangleF ShapeBounds(TileCollisionShapeDefinition shape)
        => new(Clamp01(shape.X), Clamp01(shape.Y), Math.Max(0.01f, Math.Min(1f, shape.Width)), Math.Max(0.01f, Math.Min(1f, shape.Height)));

    private static RectangleF PolygonBounds(TileCollisionShapeDefinition shape)
    {
        if (shape.Points.Count <= 0)
        {
            return RectangleF.Empty;
        }

        var left = shape.Points.Min(point => point.X);
        var top = shape.Points.Min(point => point.Y);
        var right = shape.Points.Max(point => point.X);
        var bottom = shape.Points.Max(point => point.Y);
        return RectangleF.FromLTRB(left, top, right, bottom);
    }

    private static PointF[] ShapeHandlePoints(RectangleF rect)
    {
        return
        [
            new(rect.Left, rect.Top),
            new(rect.Right, rect.Top),
            new(rect.Right, rect.Bottom),
            new(rect.Left, rect.Bottom)
        ];
    }

    private static IEnumerable<PointF> ShapeHandles(RectangleF screenRect)
    {
        yield return new PointF(screenRect.Left, screenRect.Top);
        yield return new PointF(screenRect.Right, screenRect.Top);
        yield return new PointF(screenRect.Right, screenRect.Bottom);
        yield return new PointF(screenRect.Left, screenRect.Bottom);
    }

    private static RectangleF ShapeRectToScreen(RectangleF tileRect, TileCollisionShapeDefinition shape)
    {
        var rect = ShapeBounds(shape);
        return new RectangleF(
            tileRect.Left + rect.X * tileRect.Width,
            tileRect.Top + rect.Y * tileRect.Height,
            rect.Width * tileRect.Width,
            rect.Height * tileRect.Height);
    }

    private static IEnumerable<PointF> ShapePointsToScreen(RectangleF tileRect, TileCollisionShapeDefinition shape)
        => shape.Points.Select(point => TileToScreen(tileRect, new PointF(point.X, point.Y)));

    private static PointF TileToScreen(RectangleF tileRect, PointF tilePoint)
        => new(tileRect.Left + tilePoint.X * tileRect.Width, tileRect.Top + tilePoint.Y * tileRect.Height);

    private static void NormalizeShapeBounds(TileCollisionShapeDefinition shape)
    {
        shape.X = Clamp01(shape.X);
        shape.Y = Clamp01(shape.Y);
        shape.Width = Math.Max(0.01f, Math.Min(1f - shape.X, shape.Width));
        shape.Height = Math.Max(0.01f, Math.Min(1f - shape.Y, shape.Height));
    }

    private string ToolHintText()
    {
        return _activeTool switch
        {
            TileCollisionEditTool.Rectangle => "矩形：在瓦片上拖拽生成碰撞框",
            TileCollisionEditTool.Ellipse => "椭圆：在瓦片上拖拽生成椭圆碰撞",
            TileCollisionEditTool.Polygon => "多边形：左键添加点，右键或双击结束",
            _ => "选择：点击形状后可移动，拖动控制点可缩放"
        };
    }

    private static float Distance(PointF from, PointF to)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);

    private enum CollisionDragMode
    {
        None,
        DrawShape,
        MoveShape,
        ResizeShape,
        MovePolygonPoint
    }
}

internal sealed class TilesetPlanTileActivatedEventArgs : EventArgs
{
    public TilesetPlanTileActivatedEventArgs(int tileX, int tileY)
    {
        TileX = tileX;
        TileY = tileY;
    }

    public int TileX { get; }

    public int TileY { get; }
}

internal sealed class WangTileActivatedEventArgs : EventArgs
{
    public WangTileActivatedEventArgs(int tileX, int tileY)
    {
        TileX = tileX;
        TileY = tileY;
    }

    public int TileX { get; }

    public int TileY { get; }
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
