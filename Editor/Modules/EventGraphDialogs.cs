using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Editor.Modules;

internal sealed class EventDefinitionDialog : Form
{
    private readonly ComboBox _eventComboBox = new();
    private readonly ComboBox _subjectComboBox = new();
    private readonly ComboBox _areaSourceComboBox = new();
    private readonly ComboBox _shapeComboBox = new();
    private readonly NumericUpDown _widthNumericUpDown = new();
    private readonly NumericUpDown _heightNumericUpDown = new();
    private readonly NumericUpDown _radiusNumericUpDown = new();
    private readonly CheckBox _onceCheckBox = new();

    public EventDefinitionDialog(LocalizationService localization, Dictionary<string, string> current)
    {
        Text = localization.T("graph.dialog.eventTitle");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(500, 390);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 9
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (var i = 0; i < 8; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        Controls.Add(layout);

        AddRow(layout, 0, localization.T("graph.trigger.eventType"), _eventComboBox);
        AddRow(layout, 1, localization.T("graph.trigger.subject"), _subjectComboBox);
        AddRow(layout, 2, localization.T("graph.trigger.areaSource"), _areaSourceComboBox);
        AddRow(layout, 3, localization.T("graph.trigger.shape"), _shapeComboBox);
        AddRow(layout, 4, localization.T("graph.trigger.width"), _widthNumericUpDown);
        AddRow(layout, 5, localization.T("graph.trigger.height"), _heightNumericUpDown);
        AddRow(layout, 6, localization.T("graph.trigger.radius"), _radiusNumericUpDown);

        _onceCheckBox.Text = localization.T("graph.trigger.once");
        _onceCheckBox.Dock = DockStyle.Fill;
        layout.Controls.Add(_onceCheckBox, 1, 7);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0, 6, 0, 0) };
        var okButton = new Button { Text = localization.T("common.ok"), DialogResult = DialogResult.OK, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 32), Margin = new Padding(6, 0, 0, 0) };
        var cancelButton = new Button { Text = localization.T("common.cancel"), DialogResult = DialogResult.Cancel, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 32), Margin = new Padding(6, 0, 0, 0) };
        buttons.Controls.Add(okButton);
        buttons.Controls.Add(cancelButton);
        layout.Controls.Add(buttons, 0, 8);
        layout.SetColumnSpan(buttons, 2);
        AcceptButton = okButton;
        CancelButton = cancelButton;

        _widthNumericUpDown.Minimum = 1;
        _widthNumericUpDown.Maximum = 9999;
        _heightNumericUpDown.Minimum = 1;
        _heightNumericUpDown.Maximum = 9999;
        _radiusNumericUpDown.Minimum = 1;
        _radiusNumericUpDown.Maximum = 9999;

        _eventComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _subjectComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _areaSourceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _shapeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        _eventComboBox.Items.AddRange(GraphNodeCatalog.GetFieldOptions(NodeKinds.Trigger, "event").Cast<object>().ToArray());
        _subjectComboBox.Items.AddRange(GraphNodeCatalog.GetFieldOptions(NodeKinds.Trigger, "subject").Cast<object>().ToArray());
        _areaSourceComboBox.Items.AddRange(GraphNodeCatalog.GetFieldOptions(NodeKinds.Trigger, "areaSource").Cast<object>().ToArray());
        _shapeComboBox.Items.AddRange(GraphNodeCatalog.GetFieldOptions(NodeKinds.Trigger, "shape").Cast<object>().ToArray());

        SelectCombo(_eventComboBox, GetParameter(current, "event", string.Empty));
        SelectCombo(_subjectComboBox, GetParameter(current, "subject", string.Empty));
        SelectCombo(_areaSourceComboBox, GetParameter(current, "areaSource", string.Empty));
        SelectCombo(_shapeComboBox, GetParameter(current, "shape", string.Empty));
        _widthNumericUpDown.Value = ParseNumeric(GetParameter(current, "width", string.Empty), 64);
        _heightNumericUpDown.Value = ParseNumeric(GetParameter(current, "height", string.Empty), 64);
        _radiusNumericUpDown.Value = ParseNumeric(GetParameter(current, "radius", string.Empty), 32);
        _onceCheckBox.Checked = string.Equals(GetParameter(current, "runOnce", GetParameter(current, "once", "false")), "true", StringComparison.OrdinalIgnoreCase);

        _eventComboBox.SelectedIndexChanged += (_, _) => RefreshAreaFields();
        _shapeComboBox.SelectedIndexChanged += (_, _) => RefreshAreaFields();
        RefreshAreaFields();
    }

    public void ApplyTo(Dictionary<string, string> parameters)
    {
        parameters["event"] = SelectedValue(_eventComboBox);
        parameters["subject"] = SelectedValue(_subjectComboBox);
        parameters["areaSource"] = SelectedValue(_areaSourceComboBox);
        parameters["shape"] = SelectedValue(_shapeComboBox);
        parameters["width"] = ((int)_widthNumericUpDown.Value).ToString();
        parameters["height"] = ((int)_heightNumericUpDown.Value).ToString();
        parameters["radius"] = ((int)_radiusNumericUpDown.Value).ToString();
        parameters["once"] = _onceCheckBox.Checked ? "true" : "false";
        parameters["runOnce"] = _onceCheckBox.Checked ? "true" : "false";
    }

    private void RefreshAreaFields()
    {
        var isArea = SelectedValue(_eventComboBox) is "OnEnterArea" or "OnTouch";
        var isCircle = SelectedValue(_shapeComboBox) == "circle";
        _areaSourceComboBox.Enabled = isArea;
        _shapeComboBox.Enabled = isArea;
        _widthNumericUpDown.Enabled = isArea && !isCircle;
        _heightNumericUpDown.Enabled = isArea && !isCircle;
        _radiusNumericUpDown.Enabled = isArea && isCircle;
    }

    private static void AddRow(TableLayoutPanel layout, int row, string labelText, Control control)
    {
        var label = new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void SelectCombo(ComboBox comboBox, string value)
    {
        for (var i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is GraphFieldOptionDefinition option && string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
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

    private static string SelectedValue(ComboBox comboBox) => (comboBox.SelectedItem as GraphFieldOptionDefinition)?.Value ?? string.Empty;

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static decimal ParseNumeric(string text, decimal fallback) => decimal.TryParse(text, out var value) ? value : fallback;
}

internal sealed class TemplateNodeDialog : Form
{
    private readonly ComboBox _templateComboBox = new();
    private readonly TextBox _titleTextBox = new();
    private readonly TextBox _detailTextBox = new();
    private readonly DataGridView _parameterGrid = new();
    private readonly Button _addParameterButton = new();
    private readonly Button _removeParameterButton = new();

    public TemplateNodeDialog(LocalizationService localization, string kind, GraphNodeDefinition? existing = null)
    {
        Text = kind == NodeKinds.Condition ? localization.T("graph.dialog.conditionTitle") : localization.T("graph.dialog.actionTitle");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoScaleMode = AutoScaleMode.Dpi;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(640, 520);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        Controls.Add(layout);

        _templateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _templateComboBox.Items.AddRange(GraphNodeCatalog.GetFieldOptions(kind, "template").Cast<object>().ToArray());

        AddRow(layout, 0, localization.T("graph.field.template"), _templateComboBox);
        AddRow(layout, 1, localization.T("graph.field.title"), _titleTextBox);
        AddRow(layout, 2, localization.T("graph.field.detail"), _detailTextBox);

        var parametersLabel = new Label { Text = localization.T("graph.field.parameters"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.TopLeft };
        layout.Controls.Add(parametersLabel, 0, 3);
        _parameterGrid.AllowUserToAddRows = false;
        _parameterGrid.AllowUserToDeleteRows = false;
        _parameterGrid.AllowUserToResizeRows = false;
        _parameterGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _parameterGrid.BackgroundColor = SystemColors.Window;
        _parameterGrid.BorderStyle = BorderStyle.FixedSingle;
        _parameterGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _parameterGrid.Dock = DockStyle.Fill;
        _parameterGrid.MultiSelect = false;
        _parameterGrid.RowHeadersVisible = false;
        _parameterGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _parameterGrid.StandardTab = true;
        _parameterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "参数名",
            SortMode = DataGridViewColumnSortMode.NotSortable,
            FillWeight = 40
        });
        _parameterGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "参数值",
            SortMode = DataGridViewColumnSortMode.NotSortable,
            FillWeight = 60
        });
        layout.Controls.Add(_parameterGrid, 0, 4);
        layout.SetColumnSpan(_parameterGrid, 2);

        var buttons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0, 6, 0, 0)
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var parameterButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _addParameterButton.Text = localization.T("common.add");
        _addParameterButton.AutoSize = true;
        _addParameterButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _addParameterButton.MinimumSize = new Size(96, 32);
        _addParameterButton.Margin = new Padding(0, 0, 6, 0);
        _addParameterButton.Click += (_, _) => AddParameterRow(string.Empty, string.Empty);

        _removeParameterButton.Text = localization.T("common.delete");
        _removeParameterButton.AutoSize = true;
        _removeParameterButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _removeParameterButton.MinimumSize = new Size(96, 32);
        _removeParameterButton.Margin = new Padding(0);
        _removeParameterButton.Click += (_, _) => RemoveSelectedParameterRows();

        parameterButtons.Controls.Add(_addParameterButton);
        parameterButtons.Controls.Add(_removeParameterButton);

        var actionButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        var okButton = new Button { Text = localization.T("common.ok"), DialogResult = DialogResult.OK, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 32), Margin = new Padding(6, 0, 0, 0) };
        var cancelButton = new Button { Text = localization.T("common.cancel"), DialogResult = DialogResult.Cancel, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(96, 32), Margin = new Padding(6, 0, 0, 0) };
        actionButtons.Controls.Add(okButton);
        actionButtons.Controls.Add(cancelButton);

        buttons.Controls.Add(parameterButtons, 0, 0);
        buttons.Controls.Add(actionButtons, 1, 0);
        layout.Controls.Add(buttons, 0, 5);
        layout.SetColumnSpan(buttons, 2);
        AcceptButton = okButton;
        CancelButton = cancelButton;

        if (existing is not null)
        {
            SelectCombo(_templateComboBox, GetParameter(existing.Parameters, "template", string.Empty));
            _titleTextBox.Text = existing.Title;
            _detailTextBox.Text = GetParameter(existing.Parameters, "detail", string.Empty);
            LoadParameterRows(existing.Parameters);
        }
        else if (_templateComboBox.Items.Count > 0)
        {
            _templateComboBox.SelectedIndex = 0;
            AddParameterRow(string.Empty, string.Empty);
        }

        _parameterGrid.SelectionChanged += (_, _) => UpdateParameterButtons();
        _parameterGrid.RowsAdded += (_, _) => UpdateParameterButtons();
        _parameterGrid.RowsRemoved += (_, _) => UpdateParameterButtons();
        UpdateParameterButtons();
    }

    public string NodeTitle => _titleTextBox.Text.Trim();

    public Dictionary<string, string> BuildParameters()
    {
        var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in _parameterGrid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            var key = Convert.ToString(row.Cells[0].Value)?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = Convert.ToString(row.Cells[1].Value)?.Trim() ?? string.Empty;
            parameters[key] = value;
        }

        parameters["template"] = (_templateComboBox.SelectedItem as GraphFieldOptionDefinition)?.Value ?? string.Empty;
        parameters["detail"] = _detailTextBox.Text.Trim();
        return parameters;
    }

    private void LoadParameterRows(Dictionary<string, string> parameters)
    {
        _parameterGrid.Rows.Clear();
        foreach (var pair in parameters)
        {
            if (pair.Key is "template" or "detail")
            {
                continue;
            }

            AddParameterRow(pair.Key, pair.Value);
        }

        if (_parameterGrid.Rows.Count == 0)
        {
            AddParameterRow(string.Empty, string.Empty);
        }
    }

    private void AddParameterRow(string key, string value)
    {
        _parameterGrid.Rows.Add(key, value);
        UpdateParameterButtons();
    }

    private void RemoveSelectedParameterRows()
    {
        if (_parameterGrid.SelectedRows.Count == 0)
        {
            return;
        }

        var rows = _parameterGrid.SelectedRows.Cast<DataGridViewRow>()
            .Where(row => !row.IsNewRow)
            .OrderByDescending(row => row.Index)
            .ToList();

        foreach (var row in rows)
        {
            _parameterGrid.Rows.RemoveAt(row.Index);
        }

        if (_parameterGrid.Rows.Count == 0)
        {
            AddParameterRow(string.Empty, string.Empty);
        }

        UpdateParameterButtons();
    }

    private void UpdateParameterButtons()
    {
        _removeParameterButton.Enabled = _parameterGrid.SelectedRows.Count > 0;
    }

    private static void AddRow(TableLayoutPanel layout, int row, string labelText, Control control)
    {
        var label = new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void SelectCombo(ComboBox comboBox, string value)
    {
        for (var i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is GraphFieldOptionDefinition option && string.Equals(option.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }

        if (comboBox.Items.Count > 0)
        {
            comboBox.SelectedIndex = 0;
        }
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }
}
