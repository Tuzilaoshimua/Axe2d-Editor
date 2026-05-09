using Axe2DEditor.Core.Projects;
using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Runtime;

public sealed class RuntimePreviewForm : Form
{
    private readonly ProjectContext _context;
    private readonly LocalizationService _localization;
    private readonly string _mode;
    private readonly RuntimeSession _session;
    private readonly RuntimeSimulation _simulation;
    private readonly RuntimeRenderer _renderer = new();
    private readonly System.Windows.Forms.Timer _frameTimer = new();
    private readonly RuntimeViewportPanel _viewport = new();
    private readonly Panel _sidePanel = new();
    private readonly Label _titleLabel = new();
    private readonly Label _summaryLabel = new();
    private readonly Label _statusLabel = new();
    private readonly ListBox _detailList = new();
    private readonly ListBox _triggerListBox = new();
    private readonly Label _controlHintLabel = new();
    private readonly Label _triggerResultLabel = new();
    private readonly Label _dialogueResultLabel = new();
    private readonly Label _rewardResultLabel = new();
    private readonly Button _triggerButton = new();
    private readonly DateTime _startedAt = DateTime.Now;
    private float _pulse;

    public RuntimePreviewForm(ProjectContext context, LocalizationService localization, string mode)
    {
        _context = context;
        _localization = localization;
        _mode = mode;
        _session = new RuntimeSession(context);
        _simulation = new RuntimeSimulation(_session);
        BuildUi();
        LoadProjectSummary();
        _frameTimer.Interval = 16;
        _frameTimer.Tick += (_, _) =>
        {
            _pulse += 0.06f;
            _simulation.Step(1f / 60f);
            _viewport.Invalidate();
            UpdateStatus();
        };
        _frameTimer.Start();
        FormClosed += (_, _) => _frameTimer.Stop();
        KeyDown += RuntimePreviewForm_KeyDown;
        KeyUp += RuntimePreviewForm_KeyUp;
    }

    private void BuildUi()
    {
        Text = _mode == "test" ? "Axe2D 测试运行" : "Axe2D 运行";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1100, 720);
        Size = new Size(1280, 860);
        BackColor = Color.FromArgb(24, 24, 28);
        ForeColor = Color.White;
        KeyPreview = true;

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 108,
            Padding = new Padding(16, 14, 16, 12),
            BackColor = Color.FromArgb(33, 33, 38)
        };
        Controls.Add(topPanel);

        _titleLabel.Dock = DockStyle.Top;
        _titleLabel.Height = 34;
        _titleLabel.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
        _titleLabel.ForeColor = Color.White;
        topPanel.Controls.Add(_titleLabel);

        _summaryLabel.Dock = DockStyle.Top;
        _summaryLabel.Height = 24;
        _summaryLabel.Font = new Font("Microsoft YaHei UI", 10F);
        _summaryLabel.ForeColor = Color.FromArgb(220, 220, 220);
        topPanel.Controls.Add(_summaryLabel);

        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 22;
        _statusLabel.Font = new Font("Microsoft YaHei UI", 8.5F);
        _statusLabel.ForeColor = Color.FromArgb(180, 180, 180);
        topPanel.Controls.Add(_statusLabel);

        _sidePanel.Dock = DockStyle.Right;
        _sidePanel.Width = 320;
        _sidePanel.MinimumSize = new Size(260, 0);
        _sidePanel.Padding = new Padding(12);
        _sidePanel.BackColor = Color.FromArgb(30, 30, 34);
        Controls.Add(_sidePanel);

        _viewport.Dock = DockStyle.Fill;
        _viewport.BackColor = Color.FromArgb(16, 18, 22);
        _viewport.Paint += Viewport_Paint;
        _viewport.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var world = _renderer.ScreenToWorld(e.Location, _viewport.ClientRectangle, _session);
            _simulation.TeleportPlayer(world);
            _viewport.Invalidate();
        };
        Controls.Add(_viewport);

        var detailTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "项目摘要",
            Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.White
        };
        _sidePanel.Controls.Add(detailTitle);

        _controlHintLabel.Dock = DockStyle.Top;
        _controlHintLabel.Height = 72;
        _controlHintLabel.Margin = new Padding(0, 8, 0, 8);
        _controlHintLabel.Font = new Font("Microsoft YaHei UI", 9F);
        _controlHintLabel.ForeColor = Color.FromArgb(220, 220, 220);
        _controlHintLabel.Text = "操作说明：\r\nWASD / 方向键移动测试角色\r\n鼠标左键可把角色放到指定位置\r\nEsc 关闭运行窗口";
        _sidePanel.Controls.Add(_controlHintLabel);

        var runtimeStateTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "运行状态",
            Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.White
        };
        _sidePanel.Controls.Add(runtimeStateTitle);

        _detailList.Dock = DockStyle.Fill;
        _detailList.BorderStyle = BorderStyle.FixedSingle;
        _detailList.BackColor = Color.FromArgb(38, 38, 44);
        _detailList.ForeColor = Color.WhiteSmoke;
        _detailList.Font = new Font("Consolas", 9.5F);
        _detailList.Height = 240;
        _detailList.Dock = DockStyle.Top;
        _sidePanel.Controls.Add(_detailList);

        var triggerTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 24,
            Text = "触发器测试",
            Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.White
        };
        _sidePanel.Controls.Add(triggerTitle);

        _triggerResultLabel.Dock = DockStyle.Top;
        _triggerResultLabel.Height = 54;
        _triggerResultLabel.ForeColor = Color.FromArgb(220, 220, 220);
        _triggerResultLabel.Font = new Font("Microsoft YaHei UI", 9F);
        _triggerResultLabel.Text = "最近结果：尚未触发事件。";
        _sidePanel.Controls.Add(_triggerResultLabel);

        _triggerButton.Dock = DockStyle.Top;
        _triggerButton.Height = 34;
        _triggerButton.Text = "触发选中事件";
        _triggerButton.FlatStyle = FlatStyle.Flat;
        _triggerButton.BackColor = Color.FromArgb(57, 108, 189);
        _triggerButton.ForeColor = Color.White;
        _triggerButton.Click += TriggerButton_Click;
        _sidePanel.Controls.Add(_triggerButton);

        _dialogueResultLabel.Dock = DockStyle.Top;
        _dialogueResultLabel.Height = 42;
        _dialogueResultLabel.ForeColor = Color.FromArgb(230, 230, 230);
        _dialogueResultLabel.Font = new Font("Microsoft YaHei UI", 8.8F);
        _dialogueResultLabel.Text = "最近对话：暂无对话。";
        _sidePanel.Controls.Add(_dialogueResultLabel);

        _rewardResultLabel.Dock = DockStyle.Top;
        _rewardResultLabel.Height = 42;
        _rewardResultLabel.ForeColor = Color.FromArgb(230, 230, 230);
        _rewardResultLabel.Font = new Font("Microsoft YaHei UI", 8.8F);
        _rewardResultLabel.Text = "最近奖励：暂无奖励。";
        _sidePanel.Controls.Add(_rewardResultLabel);

        _triggerListBox.Dock = DockStyle.Fill;
        _triggerListBox.BorderStyle = BorderStyle.FixedSingle;
        _triggerListBox.BackColor = Color.FromArgb(38, 38, 44);
        _triggerListBox.ForeColor = Color.WhiteSmoke;
        _triggerListBox.Font = new Font("Consolas", 9F);
        _triggerListBox.DoubleClick += (_, _) => TriggerSelectedEvent();
        _sidePanel.Controls.Add(_triggerListBox);

        FormClosing += (_, _) => _frameTimer.Stop();
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                Close();
            }
        };
    }

    private void LoadProjectSummary()
    {
        var project = _context.Project;
        _titleLabel.Text = $"{project.Name} - {(_mode == "test" ? "测试运行" : "运行")}";
        _summaryLabel.Text = $"项目目录: {_context.RootDirectory}";

        _detailList.Items.Clear();
        _detailList.Items.Add($"引擎版本: {project.EngineVersion}");
        _detailList.Items.Add($"创建时间: {project.CreatedAt:yyyy-MM-dd HH:mm:ss}");
        _detailList.Items.Add($"更新时间: {project.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        _detailList.Items.Add($"资源数: {project.AssetLibrary.Units.Count} 单位, {project.AssetLibrary.Maps.Count} 地图, {project.EventGraphs.Count} 事件图");
        _detailList.Items.Add($"测试角色: {_session.Player.Name}");
        _detailList.Items.Add($"场景对象: {_session.SceneObjects.Count}");
        _detailList.Items.Add($"  普通对象: {_session.SceneObjects.Count(obj => obj.Category == "object")}");
        _detailList.Items.Add($"  相机对象: {_session.SceneObjects.Count(obj => obj.Category == "camera")}");
        _detailList.Items.Add($"  UI对象: {_session.SceneObjects.Count(obj => obj.Category == "ui")}");
        _detailList.Items.Add($"相机模式: {(_session.HasExplicitCamera ? "使用场景相机" : "无相机，跟随玩家")}");
        _detailList.Items.Add($"当前相机: {_session.CameraLabel}");
        _detailList.Items.Add($"跟随策略: {_session.CameraModeLabel}");
        _detailList.Items.Add(string.Empty);

        if (_session.ActiveMap is null)
        {
            _detailList.Items.Add("当前没有可运行地图，runtime 先展示编辑器预览壳。");
        }
        else
        {
            _detailList.Items.Add($"当前地图: {_session.ActiveMap.DisplayName} ({_session.ActiveMap.Id})");
            _detailList.Items.Add($"  尺寸: {_session.ActiveMap.Width} x {_session.ActiveMap.Height}");
            _detailList.Items.Add($"  图块集: {_session.ActiveMap.Tileset}");
            _detailList.Items.Add("  状态: 可测试");
        }

        if (_session.Triggers.Count > 0)
        {
            _detailList.Items.Add(string.Empty);
            _detailList.Items.Add($"触发器数: {_session.Triggers.Count}");
            foreach (var trigger in _session.Triggers.Take(5))
            {
                _detailList.Items.Add($"  {trigger.TriggerName} | 事件={trigger.EventType} | 对象={trigger.Subject}");
            }
        }

        RefreshTriggerList();
        RefreshFeedbackPanels();
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var elapsed = DateTime.Now - _startedAt;
        var mapName = _session.ActiveMap?.DisplayName ?? "无地图";
        _statusLabel.Text = $"模式: {_mode} | 地图: {mapName} | 已运行 {elapsed:mm\\:ss} | 角色: {_session.Player.X:0.0},{_session.Player.Y:0.0} | 相机: {_session.CameraFocus.X:0.0},{_session.CameraFocus.Y:0.0}";
    }

    private void Viewport_Paint(object? sender, PaintEventArgs e)
    {
        _renderer.Render(e.Graphics, _viewport.ClientRectangle, _session);
    }

    private void TriggerButton_Click(object? sender, EventArgs e)
    {
        TriggerSelectedEvent();
    }

    private void TriggerSelectedEvent()
    {
        if (_triggerListBox.SelectedIndex < 0)
        {
            _triggerResultLabel.Text = "最近结果：请先选择一个触发器。";
            return;
        }

        var result = _session.TriggerByIndex(_triggerListBox.SelectedIndex);
        _triggerResultLabel.Text = $"最近结果：{result}";
        RefreshFeedbackPanels();
    }

    private void RefreshTriggerList()
    {
        _triggerListBox.Items.Clear();
        foreach (var trigger in _session.Triggers)
        {
            var eventType = string.IsNullOrWhiteSpace(trigger.EventType) ? "未设置事件" : trigger.EventType;
            var subject = string.IsNullOrWhiteSpace(trigger.Subject) ? "未设置对象" : trigger.Subject;
            _triggerListBox.Items.Add($"{trigger.TriggerName} | {eventType} | {subject}");
        }

        _triggerButton.Enabled = _triggerListBox.Items.Count > 0;
        if (_triggerListBox.Items.Count > 0)
        {
            _triggerListBox.SelectedIndex = 0;
        }
        else
        {
            _triggerResultLabel.Text = "最近结果：当前没有可测试触发器。";
        }
    }

    private void RefreshFeedbackPanels()
    {
        _dialogueResultLabel.Text = $"最近对话：{_session.LastDialogueMessage}";
        _rewardResultLabel.Text = $"最近奖励：{_session.LastRewardMessage}";
    }

    private void RuntimePreviewForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            return;
        }

        _simulation.SetKeyState(e.KeyCode, true);
    }

    private void RuntimePreviewForm_KeyUp(object? sender, KeyEventArgs e)
    {
        _simulation.SetKeyState(e.KeyCode, false);
    }

    private sealed class RuntimeViewportPanel : Panel
    {
        public RuntimeViewportPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
