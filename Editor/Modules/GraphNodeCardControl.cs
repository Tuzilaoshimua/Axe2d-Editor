using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Editor.Localization;
using System.Drawing.Drawing2D;

namespace Axe2DEditor.Editor.Modules;

internal sealed class GraphNodeCardControl : Panel
{
    public const int CardWidth = 360;
    public const int HeaderHeight = 44;
    public const int SocketSize = 12;
    public static readonly Padding CardPadding = new(16, 12, 16, 12);
    private const int SocketRowCenterY = HeaderHeight + 22;

    private readonly LocalizationService _localization;
    private readonly GraphNodeDefinition _node;
    private readonly Action<GraphNodeDefinition> _onChanged;
    private readonly Action<GraphNodeDefinition> _onSelected;

    private readonly Panel _headerPanel = new();
    private readonly Label _kindLabel = new();
    private readonly TextBox _titleTextBox = new();
    private readonly Label _summaryLabel = new();
    private readonly TableLayoutPanel _bodyLayout = new();

    private readonly ComboBox _eventComboBox = new();
    private readonly ComboBox _subjectComboBox = new();
    private readonly ComboBox _areaSourceComboBox = new();
    private readonly ComboBox _shapeComboBox = new();
    private readonly NumericUpDown _widthNumericUpDown = new();
    private readonly NumericUpDown _heightNumericUpDown = new();
    private readonly NumericUpDown _radiusNumericUpDown = new();
    private readonly CheckBox _onceCheckBox = new();

    private readonly ComboBox _templateComboBox = new();
    private readonly TextBox _detailTextBox = new();
    private readonly TextBox _parametersTextBox = new();

    private bool _suppressUpdate;
    private bool _dragging;
    private Point _dragStart;
    private Point _nodeStart;
    private bool _selected;

    public GraphNodeCardControl(
        LocalizationService localization,
        GraphNodeDefinition node,
        Action<GraphNodeDefinition> onChanged,
        Action<GraphNodeDefinition> onSelected)
    {
        _localization = localization;
        _node = node;
        _onChanged = onChanged;
        _onSelected = onSelected;

        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        DoubleBuffered = true;
        UpdateStyles();
        BackColor = Color.Transparent;
        Size = new Size(CardWidth, GetCardHeight(node));
        Margin = Padding.Empty;
        Padding = CardPadding;

        BuildChrome();
        LoadNode();
    }

    public GraphNodeDefinition Node => _node;

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            Invalidate();
        }
    }

    public Point GetSocketCenter(string direction)
    {
        var x = direction == NodePortDirections.Input ? Padding.Left : Width - Padding.Right;
        return new Point(x, SocketRowCenterY);
    }

    public static Point GetSocketCenter(Size cardSize, Padding cardPadding, string direction)
    {
        var x = direction == NodePortDirections.Input ? cardPadding.Left : cardSize.Width - cardPadding.Right;
        return new Point(x, SocketRowCenterY);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        BeginDrag(e.Location);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_dragging)
        {
            return;
        }

        var dx = e.X - _dragStart.X;
        var dy = e.Y - _dragStart.Y;
        Location = new Point(_nodeStart.X + dx, _nodeStart.Y + dy);
        _node.X = Location.X;
        _node.Y = Location.Y;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        _onChanged(_node);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var shadowBrush = new SolidBrush(Color.FromArgb(28, 0, 0, 0));
        using var fillBrush = new SolidBrush(Color.White);
        using var borderPen = new Pen(Selected ? Color.FromArgb(41, 128, 185) : Color.FromArgb(195, 200, 208), Selected ? 2F : 1F);
        using var headerBrush = new SolidBrush(GetHeaderColor());

        DrawRounded(e.Graphics, new Rectangle(3, 5, Width - 1, Height - 1), 12, shadowBrush);
        DrawRounded(e.Graphics, rect, 12, fillBrush);
        DrawRounded(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), 12, borderPen);

        using var socketOutline = new Pen(Color.FromArgb(106, 115, 128), 1.5F);
        using var inputFill = new SolidBrush(Color.FromArgb(255, 255, 255));
        using var outputFill = new SolidBrush(Color.FromArgb(65, 140, 220));
        var inputCenter = GetSocketCenter(NodePortDirections.Input);
        var outputCenter = GetSocketCenter(NodePortDirections.Output);
        e.Graphics.FillEllipse(inputFill, inputCenter.X - SocketSize / 2, inputCenter.Y - SocketSize / 2, SocketSize, SocketSize);
        e.Graphics.DrawEllipse(socketOutline, inputCenter.X - SocketSize / 2, inputCenter.Y - SocketSize / 2, SocketSize, SocketSize);
        e.Graphics.FillEllipse(outputFill, outputCenter.X - SocketSize / 2, outputCenter.Y - SocketSize / 2, SocketSize, SocketSize);
        e.Graphics.DrawEllipse(socketOutline, outputCenter.X - SocketSize / 2, outputCenter.Y - SocketSize / 2, SocketSize, SocketSize);

        using var headerOverlay = new SolidBrush(Color.FromArgb(40, Color.White));
        e.Graphics.FillRectangle(headerBrush, new Rectangle(1, 1, Width - 2, HeaderHeight - 2));
        e.Graphics.FillRectangle(headerOverlay, new Rectangle(1, 1, Width - 2, 2));
    }

    private void BuildChrome()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, HeaderHeight));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        Controls.Add(layout);

        _headerPanel.Dock = DockStyle.Fill;
        _headerPanel.BackColor = Color.Transparent;
        _headerPanel.Margin = Padding.Empty;
        _headerPanel.Padding = new Padding(18, 12, 18, 8);
        _headerPanel.Cursor = Cursors.SizeAll;
        _headerPanel.MouseDown += Header_MouseDown;
        _headerPanel.MouseMove += Header_MouseMove;
        _headerPanel.MouseUp += Header_MouseUp;
        layout.Controls.Add(_headerPanel, 0, 0);

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            BackColor = Color.Transparent
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _headerPanel.Controls.Add(headerLayout);
        headerLayout.MouseDown += Header_MouseDown;
        headerLayout.MouseMove += Header_MouseMove;
        headerLayout.MouseUp += Header_MouseUp;
        _kindLabel.MouseDown += Header_MouseDown;
        _kindLabel.MouseMove += Header_MouseMove;
        _kindLabel.MouseUp += Header_MouseUp;
        _summaryLabel.MouseDown += Header_MouseDown;
        _summaryLabel.MouseMove += Header_MouseMove;
        _summaryLabel.MouseUp += Header_MouseUp;

        _kindLabel.Dock = DockStyle.Fill;
        _kindLabel.AutoSize = false;
        _kindLabel.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold);
        _kindLabel.ForeColor = Color.FromArgb(90, 60, 10);
        _kindLabel.BackColor = Color.FromArgb(255, 242, 214);
        _kindLabel.BorderStyle = BorderStyle.FixedSingle;
        _kindLabel.TextAlign = ContentAlignment.MiddleCenter;
        _kindLabel.Margin = new Padding(0, 0, 10, 0);
        headerLayout.Controls.Add(_kindLabel, 0, 0);

        _summaryLabel.Dock = DockStyle.Fill;
        _summaryLabel.Font = new Font(Font.FontFamily, 9.5F, FontStyle.Regular);
        _summaryLabel.ForeColor = Color.FromArgb(36, 38, 42);
        _summaryLabel.TextAlign = ContentAlignment.MiddleRight;
        headerLayout.Controls.Add(_summaryLabel, 1, 0);

        _titleTextBox.Dock = DockStyle.Fill;
        _titleTextBox.Margin = new Padding(0, 2, 0, 0);
        _titleTextBox.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
        _titleTextBox.BorderStyle = BorderStyle.None;
        _titleTextBox.BackColor = Color.FromArgb(255, 252, 246);
        _titleTextBox.TextChanged += (_, _) => UpdateTitleFromEditor();
        _titleTextBox.Enter += (_, _) => _onSelected(_node);
        headerLayout.Controls.Add(_titleTextBox, 0, 1);
        headerLayout.SetColumnSpan(_titleTextBox, 2);

        _bodyLayout.Dock = DockStyle.Fill;
        _bodyLayout.Margin = new Padding(0, 4, 0, 0);
        _bodyLayout.Padding = Padding.Empty;
        _bodyLayout.ColumnCount = 2;
        _bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
        _bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(_bodyLayout, 0, 1);

        BuildEditors();
    }

    private void BuildEditors()
    {
        _bodyLayout.Controls.Clear();
        _bodyLayout.RowStyles.Clear();
        _bodyLayout.RowCount = 0;

        if (string.Equals(_node.Kind, NodeKinds.Trigger, StringComparison.OrdinalIgnoreCase))
        {
            BuildTriggerEditors();
        }
        else
        {
            BuildTemplateEditors();
        }
    }

    private void BuildTriggerEditors()
    {
        AddRow(_localization.T("graph.trigger.eventType"), _eventComboBox);
        AddRow(_localization.T("graph.trigger.subject"), _subjectComboBox);
        AddRow(_localization.T("graph.trigger.areaSource"), _areaSourceComboBox);
        AddRow(_localization.T("graph.trigger.shape"), _shapeComboBox);
        AddRow(_localization.T("graph.trigger.width"), _widthNumericUpDown);
        AddRow(_localization.T("graph.trigger.height"), _heightNumericUpDown);
        AddRow(_localization.T("graph.trigger.radius"), _radiusNumericUpDown);

        _onceCheckBox.Text = _localization.T("graph.trigger.once");
        _onceCheckBox.Margin = new Padding(0, 4, 0, 0);
        _onceCheckBox.CheckedChanged += (_, _) => UpdateNodeFromEditors();
        _bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        _bodyLayout.Controls.Add(_onceCheckBox, 1, _bodyLayout.RowCount);
        _bodyLayout.SetColumnSpan(_onceCheckBox, 2);
        _bodyLayout.RowCount++;

        _eventComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _subjectComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _areaSourceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _shapeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        _eventComboBox.Items.AddRange(new object[]
        {
            new TemplateOption("OnInteract", _localization.T("graph.trigger.event.interact")),
            new TemplateOption("OnEnterArea", _localization.T("graph.trigger.event.enterArea")),
            new TemplateOption("OnTouch", _localization.T("graph.trigger.event.touch")),
            new TemplateOption("OnSkillCast", _localization.T("graph.trigger.event.skillCast")),
            new TemplateOption("OnDamageDealt", _localization.T("graph.trigger.event.damageDealt"))
        });
        _subjectComboBox.Items.AddRange(new object[]
        {
            new TemplateOption("player", _localization.T("graph.trigger.subject.player")),
            new TemplateOption("any", _localization.T("graph.trigger.subject.any")),
            new TemplateOption("specific", _localization.T("graph.trigger.subject.specific"))
        });
        _areaSourceComboBox.Items.AddRange(new object[]
        {
            new TemplateOption("self", _localization.T("graph.trigger.area.self")),
            new TemplateOption("mapTrigger", _localization.T("graph.trigger.area.mapTrigger")),
            new TemplateOption("custom", _localization.T("graph.trigger.area.custom"))
        });
        _shapeComboBox.Items.AddRange(new object[]
        {
            new TemplateOption("box", _localization.T("graph.trigger.shape.box")),
            new TemplateOption("circle", _localization.T("graph.trigger.shape.circle"))
        });

        ConfigureNumeric(_widthNumericUpDown);
        ConfigureNumeric(_heightNumericUpDown);
        ConfigureNumeric(_radiusNumericUpDown);

        _eventComboBox.SelectedIndexChanged += (_, _) => RefreshTriggerAreaFields();
        _shapeComboBox.SelectedIndexChanged += (_, _) => RefreshTriggerAreaFields();
        _eventComboBox.SelectedIndexChanged += (_, _) => UpdateNodeFromEditors();
        _subjectComboBox.SelectedIndexChanged += (_, _) => UpdateNodeFromEditors();
        _areaSourceComboBox.SelectedIndexChanged += (_, _) => UpdateNodeFromEditors();
        _shapeComboBox.SelectedIndexChanged += (_, _) => UpdateNodeFromEditors();
        _widthNumericUpDown.ValueChanged += (_, _) => UpdateNodeFromEditors();
        _heightNumericUpDown.ValueChanged += (_, _) => UpdateNodeFromEditors();
        _radiusNumericUpDown.ValueChanged += (_, _) => UpdateNodeFromEditors();
    }

    private void BuildTemplateEditors()
    {
        AddRow(_localization.T("graph.field.template"), _templateComboBox);
        AddRow(_localization.T("graph.field.detail"), _detailTextBox);

        var parametersLabel = new Label
        {
            Text = _localization.T("graph.field.parameters"),
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.TopLeft
        };

        _parametersTextBox.AcceptsReturn = true;
        _parametersTextBox.AcceptsTab = true;
        _parametersTextBox.Multiline = true;
        _parametersTextBox.ScrollBars = ScrollBars.Vertical;
        _parametersTextBox.Dock = DockStyle.Fill;
        _parametersTextBox.Margin = new Padding(0, 4, 0, 0);
        _parametersTextBox.TextChanged += (_, _) => UpdateNodeFromEditors();

        var rowIndex = _bodyLayout.RowCount;
        _bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        _bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
        _bodyLayout.Controls.Add(parametersLabel, 0, rowIndex);
        _bodyLayout.Controls.Add(_parametersTextBox, 1, rowIndex + 1);
        _bodyLayout.RowCount += 2;

        _templateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _templateComboBox.Items.AddRange(_node.Kind == NodeKinds.Condition
            ? new object[]
            {
                new TemplateOption("switch", _localization.T("graph.template.switch")),
                new TemplateOption("variable", _localization.T("graph.template.variable")),
                new TemplateOption("state", _localization.T("graph.template.state")),
                new TemplateOption("actor", _localization.T("graph.template.actor"))
            }
            : new object[]
            {
                new TemplateOption("dialogue", _localization.T("graph.template.dialogue")),
                new TemplateOption("move", _localization.T("graph.template.move")),
                new TemplateOption("animation", _localization.T("graph.template.animation")),
                new TemplateOption("giveItem", _localization.T("graph.template.giveItem")),
                new TemplateOption("changeMap", _localization.T("graph.template.changeMap"))
            });
        _templateComboBox.SelectedIndexChanged += (_, _) => UpdateNodeFromEditors();
        _detailTextBox.TextChanged += (_, _) => UpdateNodeFromEditors();
    }

    private void AddRow(string labelText, Control control)
    {
        var rowIndex = _bodyLayout.RowCount;
        _bodyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        _bodyLayout.Controls.Add(new Label
        {
            Text = labelText,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(70, 74, 80)
        }, 0, rowIndex);

        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(0, 3, 0, 2);
        _bodyLayout.Controls.Add(control, 1, rowIndex);
        _bodyLayout.RowCount++;
    }

    private void ConfigureNumeric(NumericUpDown numeric)
    {
        numeric.Minimum = 1;
        numeric.Maximum = 9999;
        numeric.Dock = DockStyle.Fill;
        numeric.Margin = new Padding(0, 3, 0, 2);
        numeric.ValueChanged += (_, _) => UpdateNodeFromEditors();
    }

    private void LoadNode()
    {
        _suppressUpdate = true;
        try
        {
            _kindLabel.Text = GetKindLabel(_node.Kind);
            _titleTextBox.Text = _node.Title;
            _summaryLabel.Text = _localization.T(_node.Kind == NodeKinds.Trigger
                ? "graph.node.trigger"
                : _node.Kind == NodeKinds.Condition
                    ? "graph.node.condition"
                    : "graph.node.action");

            if (_node.Kind == NodeKinds.Trigger)
            {
                SelectCombo(_eventComboBox, GetParameter(_node.Parameters, "event", string.Empty));
                SelectCombo(_subjectComboBox, GetParameter(_node.Parameters, "subject", string.Empty));
                SelectCombo(_areaSourceComboBox, GetParameter(_node.Parameters, "areaSource", string.Empty));
                SelectCombo(_shapeComboBox, GetParameter(_node.Parameters, "shape", string.Empty));
                _widthNumericUpDown.Value = ParseNumeric(GetParameter(_node.Parameters, "width", string.Empty), 64);
                _heightNumericUpDown.Value = ParseNumeric(GetParameter(_node.Parameters, "height", string.Empty), 64);
                _radiusNumericUpDown.Value = ParseNumeric(GetParameter(_node.Parameters, "radius", string.Empty), 32);
                _onceCheckBox.Checked = string.Equals(GetParameter(_node.Parameters, "runOnce", GetParameter(_node.Parameters, "once", "false")), "true", StringComparison.OrdinalIgnoreCase);
                RefreshTriggerAreaFields();
            }
            else
            {
                SelectCombo(_templateComboBox, GetParameter(_node.Parameters, "template", string.Empty));
                _detailTextBox.Text = GetParameter(_node.Parameters, "detail", string.Empty);
                _parametersTextBox.Text = SerializeParameters(_node.Parameters.Where(pair => pair.Key is not "template" and not "detail").ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase));
            }
        }
        finally
        {
            _suppressUpdate = false;
        }
    }

    private void UpdateTitleFromEditor()
    {
        if (_suppressUpdate)
        {
            return;
        }

        _node.Title = _titleTextBox.Text.Trim();
        _onChanged(_node);
    }

    private void UpdateNodeFromEditors()
    {
        if (_suppressUpdate)
        {
            return;
        }

        if (_node.Kind == NodeKinds.Trigger)
        {
            _node.Parameters["event"] = SelectedValue(_eventComboBox);
            _node.Parameters["subject"] = SelectedValue(_subjectComboBox);
            _node.Parameters["areaSource"] = SelectedValue(_areaSourceComboBox);
            _node.Parameters["shape"] = SelectedValue(_shapeComboBox);
            _node.Parameters["width"] = ((int)_widthNumericUpDown.Value).ToString();
            _node.Parameters["height"] = ((int)_heightNumericUpDown.Value).ToString();
            _node.Parameters["radius"] = ((int)_radiusNumericUpDown.Value).ToString();
            _node.Parameters["once"] = _onceCheckBox.Checked ? "true" : "false";
            _node.Parameters["runOnce"] = _onceCheckBox.Checked ? "true" : "false";
            RefreshTriggerAreaFields();
        }
        else
        {
            _node.Parameters["template"] = SelectedValue(_templateComboBox);
            _node.Parameters["detail"] = _detailTextBox.Text.Trim();
            foreach (var key in _node.Parameters.Keys.Where(key => key is not "template" and not "detail").ToList())
            {
                _node.Parameters.Remove(key);
            }

            foreach (var pair in ParseParameters(_parametersTextBox.Text))
            {
                _node.Parameters[pair.Key] = pair.Value;
            }
            _node.Parameters["template"] = SelectedValue(_templateComboBox);
            _node.Parameters["detail"] = _detailTextBox.Text.Trim();
        }

        _onChanged(_node);
    }

    private void RefreshTriggerAreaFields()
    {
        var isArea = SelectedValue(_eventComboBox) is "OnEnterArea" or "OnTouch";
        var isCircle = SelectedValue(_shapeComboBox) == "circle";
        _areaSourceComboBox.Enabled = isArea;
        _shapeComboBox.Enabled = isArea;
        _widthNumericUpDown.Enabled = isArea && !isCircle;
        _heightNumericUpDown.Enabled = isArea && !isCircle;
        _radiusNumericUpDown.Enabled = isArea && isCircle;
        _onceCheckBox.Enabled = true;
    }

    private void Header_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        BeginDrag(e.Location);
        _onSelected(_node);
    }

    private void Header_MouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var dx = e.X - _dragStart.X;
        var dy = e.Y - _dragStart.Y;
        Location = new Point(_nodeStart.X + dx, _nodeStart.Y + dy);
        _node.X = Location.X;
        _node.Y = Location.Y;
    }

    private void Header_MouseUp(object? sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        _onChanged(_node);
    }

    private void BeginDrag(Point location)
    {
        _dragging = true;
        _dragStart = location;
        _nodeStart = Location;
    }

    private void DrawRounded(Graphics graphics, Rectangle bounds, int radius, Brush brush)
    {
        using var path = CreateRoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    private void DrawRounded(Graphics graphics, Rectangle bounds, int radius, Pen pen)
    {
        using var path = CreateRoundedPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private Color GetHeaderColor()
    {
        return _node.Kind switch
        {
            NodeKinds.Trigger => Color.FromArgb(255, 224, 180),
            NodeKinds.Condition => Color.FromArgb(214, 231, 255),
            NodeKinds.Action => Color.FromArgb(218, 242, 224),
            _ => Color.FromArgb(232, 232, 236)
        };
    }

    private string GetKindLabel(string kind)
    {
        return kind switch
        {
            NodeKinds.Trigger => _localization.T("graph.node.trigger"),
            NodeKinds.Condition => _localization.T("graph.node.condition"),
            NodeKinds.Action => _localization.T("graph.node.action"),
            _ => kind
        };
    }

    private static void SelectCombo(ComboBox comboBox, string value)
    {
        for (var i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is TemplateOption option && string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            comboBox.SelectedIndex = -1;
            return;
        }

        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private static string SelectedValue(ComboBox comboBox) => (comboBox.SelectedItem as TemplateOption)?.Value ?? string.Empty;

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static decimal ParseNumeric(string text, decimal fallback) => decimal.TryParse(text, out var value) ? value : fallback;

    private static string SerializeParameters(Dictionary<string, string> parameters)
    {
        if (parameters.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(Environment.NewLine, parameters.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static Dictionary<string, string> ParseParameters(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = text.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var idx = line.IndexOf('=');
            if (idx <= 0)
            {
                continue;
            }

            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            if (!string.IsNullOrWhiteSpace(key))
            {
                result[key] = value;
            }
        }

        return result;
    }

    private static int GetCardHeight(GraphNodeDefinition node)
    {
        return string.Equals(node.Kind, NodeKinds.Trigger, StringComparison.OrdinalIgnoreCase) ? 340 : 248;
    }

    private sealed record TemplateOption(string Value, string Text)
    {
        public override string ToString() => Text;
    }
}
