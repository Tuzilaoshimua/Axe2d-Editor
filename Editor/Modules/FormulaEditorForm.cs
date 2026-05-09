using System.Globalization;
using Axe2DEditor.Core.Effects;
using Axe2DEditor.Core.Projects;
using Axe2DEditor.Core.Rules;
using Axe2DEditor.Core.Stats;
using Axe2DEditor.Editor.Controls;
using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Editor.Modules;

public sealed class FormulaEditorForm : Form
{
    private const int ListWidth = 340;
    private const int ToolboxWidth = 360;

    private readonly ProjectContext _context;
    private readonly ProjectService _projectService;
    private readonly LocalizationService _localization;
    private readonly DataGridView _formulaGrid = new BufferedGrid();
    private readonly DataGridView _variableGrid = new BufferedGrid();
    private readonly TextBox _searchBox = new();
    private readonly TextBox _idBox = new();
    private readonly TextBox _nameBox = new();
    private readonly TextBox _descriptionBox = new();
    private readonly TextBox _expressionBox = new();
    private readonly TextBox _referencesBox = new();
    private readonly Label _resultLabel = new();
    private readonly Label _usageLabel = new();
    private readonly Button _newButton = new();
    private readonly Button _deleteButton = new();
    private readonly Button _applyButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _validateButton = new();
    private readonly ToolTip _toolTip = new();

    private FormulaDefinition? _selectedFormula;
    private string _selectedOriginalId = "";
    private bool _loading;

    public FormulaEditorForm(ProjectContext context, ProjectService projectService, LocalizationService localization)
    {
        _context = context;
        _projectService = projectService;
        _localization = localization;

        BuildUi();
        RefreshFormulaGrid();
    }

    private void BuildUi()
    {
        Text = T("module.formulaEditor", "公式编辑器");
        MinimumSize = new Size(1180, 760);
        Size = new Size(1460, 900);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F);

        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            Padding = new Padding(16, 10, 16, 10)
        };
        var title = new Label
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            Text = T("formulaEditor.title", "公式编辑器"),
            Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        topPanel.Controls.Add(title);

        ConfigureTopButton(_saveButton, T("inspector.save", "保存"), SaveProject);
        ConfigureTopButton(_applyButton, T("dataEditor.action.apply", "应用修改"), ApplyAndRefresh);
        ConfigureTopButton(_validateButton, T("formulaEditor.validate", "验证公式"), RefreshPreview);
        ConfigureTopButton(_deleteButton, T("common.delete", "删除"), DeleteFormula);
        ConfigureTopButton(_newButton, T("menu.create", "新建"), CreateFormula);
        topPanel.Controls.Add(_saveButton);
        topPanel.Controls.Add(_applyButton);
        topPanel.Controls.Add(_validateButton);
        topPanel.Controls.Add(_deleteButton);
        topPanel.Controls.Add(_newButton);

        var mainSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };

        var listPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        var listTitle = new Label
        {
            Dock = DockStyle.Top,
            Height = 32,
            Text = T("formulaEditor.listTitle", "公式列表"),
            Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _searchBox.Dock = DockStyle.Top;
        _searchBox.Height = 30;
        _searchBox.PlaceholderText = T("formulaEditor.search", "搜索名称、ID 或表达式");
        _searchBox.TextChanged += (_, _) => RefreshFormulaGrid(_selectedFormula?.Id);
        ConfigureFormulaGrid();
        listPanel.Controls.Add(_formulaGrid);
        listPanel.Controls.Add(_searchBox);
        listPanel.Controls.Add(listTitle);
        mainSplit.Panel1.Controls.Add(listPanel);

        var rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 5,
            Panel1MinSize = 0,
            Panel2MinSize = 0
        };
        rightSplit.Panel1.Controls.Add(BuildEditorPanel());
        rightSplit.Panel2.Controls.Add(BuildToolboxPanel());
        mainSplit.Panel2.Controls.Add(rightSplit);

        Controls.Add(mainSplit);
        Controls.Add(topPanel);
        Shown += (_, _) => BeginInvoke(new Action(() => ApplySplitLayout(mainSplit, rightSplit)));
        SizeChanged += (_, _) => ApplySplitLayout(mainSplit, rightSplit);
    }

    private static void ApplySplitLayout(SplitContainer mainSplit, SplitContainer rightSplit)
    {
        SplitContainerLayout.ApplySafe(mainSplit, ListWidth, 260, 760);
        var desired = rightSplit.Width - ToolboxWidth - rightSplit.SplitterWidth;
        SplitContainerLayout.ApplySafe(rightSplit, Math.Max(560, desired), 520, 300);
    }

    private Control BuildEditorPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            AutoScroll = true
        };
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };
        flow.Resize += (_, _) =>
        {
            foreach (Control child in flow.Controls)
            {
                child.Width = Math.Max(520, flow.ClientSize.Width - 24);
            }
        };

        flow.Controls.Add(BuildInfoGroup());
        flow.Controls.Add(BuildExpressionGroup());
        flow.Controls.Add(BuildResultGroup());
        flow.Controls.Add(BuildReferencesGroup());
        panel.Controls.Add(flow);
        return panel;
    }

    private Control BuildInfoGroup()
    {
        var group = CreateGroup("formulaEditor.info", "公式信息", 170);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(10, 8, 10, 10)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddLabeledControl(layout, 0, T("table.assetId", "ID"), _idBox, 30);
        AddLabeledControl(layout, 1, T("inspector.entity.name", "名称"), _nameBox, 30);
        _descriptionBox.Multiline = true;
        _descriptionBox.ScrollBars = ScrollBars.Vertical;
        AddLabeledControl(layout, 2, T("inspector.entity.description", "描述"), _descriptionBox, 64);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildExpressionGroup()
    {
        var group = CreateGroup("formulaEditor.expression", "公式组合", 235);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 8, 10, 10)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var hint = new Label
        {
            Dock = DockStyle.Fill,
            Text = T("formulaEditor.expressionHint", "从右侧插入数值变量和运算符，也可以直接输入。公式保存后由其他资产通过 ID 引用。"),
            ForeColor = SystemColors.GrayText,
            AutoEllipsis = true
        };
        _expressionBox.Dock = DockStyle.Fill;
        _expressionBox.Multiline = true;
        _expressionBox.ScrollBars = ScrollBars.Both;
        _expressionBox.WordWrap = false;
        _expressionBox.Font = new Font(FontFamily.GenericMonospace, 10F);
        _expressionBox.TextChanged += (_, _) =>
        {
            if (!_loading)
            {
                RefreshPreview();
            }
        };
        layout.Controls.Add(hint, 0, 0);
        layout.Controls.Add(_expressionBox, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildResultGroup()
    {
        var group = CreateGroup("formulaEditor.result", "测试结果", 118);
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 8, 10, 10)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _resultLabel.Dock = DockStyle.Fill;
        _resultLabel.TextAlign = ContentAlignment.MiddleLeft;
        _resultLabel.Font = new Font(Font.FontFamily, 11F, FontStyle.Bold);
        _usageLabel.Dock = DockStyle.Fill;
        _usageLabel.ForeColor = SystemColors.GrayText;
        _usageLabel.AutoEllipsis = true;
        layout.Controls.Add(_resultLabel, 0, 0);
        layout.Controls.Add(_usageLabel, 0, 1);
        group.Controls.Add(layout);
        return group;
    }

    private Control BuildReferencesGroup()
    {
        var group = CreateGroup("formulaEditor.references", "引用位置", 150);
        _referencesBox.Dock = DockStyle.Fill;
        _referencesBox.Multiline = true;
        _referencesBox.ReadOnly = true;
        _referencesBox.ScrollBars = ScrollBars.Vertical;
        _referencesBox.BorderStyle = BorderStyle.None;
        group.Controls.Add(_referencesBox);
        return group;
    }

    private Control BuildToolboxPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 175));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(BuildOperatorGroup("formulaEditor.basicOperators", "基础运算符",
        [
            ("+", " + ", "加法"),
            ("-", " - ", "减法"),
            ("*", " * ", "乘法"),
            ("/", " / ", "除法"),
            ("(", "(", "左括号"),
            (")", ")", "右括号")
        ]), 0, 0);
        layout.Controls.Add(BuildOperatorGroup("formulaEditor.advancedOperators", "高级函数",
        [
            ("max", "max(|, )", "最大值"),
            ("min", "min(|, )", "最小值"),
            ("clamp", "clamp(|, , )", "限制范围"),
            ("abs", "abs(|)", "绝对值"),
            ("round", "round(|)", "四舍五入"),
            ("floor", "floor(|)", "向下取整"),
            ("ceil", "ceil(|)", "向上取整"),
            ("sqrt", "sqrt(|)", "平方根"),
            ("pow", "pow(|, )", "幂运算")
        ]), 0, 1);
        layout.Controls.Add(BuildVariableGroup(), 0, 2);
        panel.Controls.Add(layout);
        return panel;
    }

    private Control BuildOperatorGroup(string titleKey, string titleFallback, IReadOnlyList<(string Text, string Token, string Hint)> buttons)
    {
        var group = CreateGroup(titleKey, titleFallback, 100, true);
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8),
            AutoScroll = true,
            WrapContents = true
        };
        foreach (var (text, token, hint) in buttons)
        {
            var button = new Button
            {
                Text = text,
                Width = 72,
                Height = 30,
                UseVisualStyleBackColor = true
            };
            _toolTip.SetToolTip(button, hint);
            button.Click += (_, _) => InsertExpressionToken(token);
            flow.Controls.Add(button);
        }
        group.Controls.Add(flow);
        return group;
    }

    private Control BuildVariableGroup()
    {
        var group = CreateGroup("formulaEditor.numericVariables", "数值变量", 200, true);
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        var insertButton = new Button
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            Text = T("formulaEditor.insertVariable", "插入选中变量"),
            UseVisualStyleBackColor = true
        };
        insertButton.Click += (_, _) => InsertSelectedVariable();
        ConfigureVariableGrid();
        panel.Controls.Add(_variableGrid);
        panel.Controls.Add(insertButton);
        group.Controls.Add(panel);
        return group;
    }

    private GroupBox CreateGroup(string titleKey, string fallback, int height, bool fillWidth = false)
    {
        var group = new GroupBox
        {
            Text = T(titleKey, fallback),
            Width = fillWidth ? 0 : 620,
            Height = height,
            Margin = new Padding(0, 0, 0, 10)
        };
        if (fillWidth)
        {
            group.Dock = DockStyle.Fill;
        }

        return group;
    }

    private static void AddLabeledControl(TableLayoutPanel layout, int row, string labelText, Control control, int height)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
        var label = new Label
        {
            Dock = DockStyle.Fill,
            Text = labelText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(label, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static void ConfigureTopButton(Button button, string text, Action action)
    {
        button.Dock = DockStyle.Right;
        button.Text = text;
        button.Width = 108;
        button.Margin = new Padding(8, 0, 0, 0);
        button.UseVisualStyleBackColor = true;
        button.Click += (_, _) => action();
    }

    private void ConfigureFormulaGrid()
    {
        _formulaGrid.Dock = DockStyle.Fill;
        _formulaGrid.AllowUserToAddRows = false;
        _formulaGrid.AllowUserToDeleteRows = false;
        _formulaGrid.AllowUserToResizeRows = false;
        _formulaGrid.ReadOnly = true;
        _formulaGrid.MultiSelect = false;
        _formulaGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _formulaGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _formulaGrid.RowHeadersVisible = false;
        _formulaGrid.Columns.Add("name", T("table.assetName", "名称"));
        _formulaGrid.Columns.Add("id", T("table.assetId", "ID"));
        _formulaGrid.Columns["id"].FillWeight = 130;
        _formulaGrid.SelectionChanged += (_, _) => SelectCurrentFormula();
    }

    private void ConfigureVariableGrid()
    {
        _variableGrid.Dock = DockStyle.Fill;
        _variableGrid.AllowUserToAddRows = false;
        _variableGrid.AllowUserToDeleteRows = false;
        _variableGrid.AllowUserToResizeRows = false;
        _variableGrid.MultiSelect = false;
        _variableGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _variableGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _variableGrid.RowHeadersVisible = false;
        _variableGrid.Columns.Add("token", T("formulaEditor.variableToken", "变量"));
        _variableGrid.Columns.Add("source", T("formulaEditor.variableSource", "来源"));
        _variableGrid.Columns.Add("value", T("formulaEditor.variableValue", "测试值"));
        _variableGrid.Columns["token"].ReadOnly = true;
        _variableGrid.Columns["source"].ReadOnly = true;
        _variableGrid.Columns["value"].FillWeight = 70;
        _variableGrid.CellDoubleClick += (_, _) => InsertSelectedVariable();
        _variableGrid.CellValueChanged += (_, _) =>
        {
            if (!_loading)
            {
                RefreshPreview();
            }
        };
        _variableGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_variableGrid.IsCurrentCellDirty)
            {
                _variableGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
    }

    private void RefreshFormulaGrid(string? selectedId = null)
    {
        _loading = true;
        _formulaGrid.SuspendLayout();
        try
        {
            _formulaGrid.Rows.Clear();
            var filter = _searchBox.Text.Trim();
            foreach (var formula in _context.Project.AssetLibrary.Formulas)
            {
                if (!string.IsNullOrWhiteSpace(filter)
                    && !formula.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
                    && !Localized(formula).Contains(filter, StringComparison.OrdinalIgnoreCase)
                    && !formula.Expression.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var rowIndex = _formulaGrid.Rows.Add(Localized(formula), formula.Id);
                var row = _formulaGrid.Rows[rowIndex];
                row.Tag = formula;
                if (!string.IsNullOrWhiteSpace(selectedId) && string.Equals(formula.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    _formulaGrid.CurrentCell = row.Cells[0];
                }
            }

            if (_formulaGrid.Rows.Count > 0 && _formulaGrid.SelectedRows.Count == 0)
            {
                _formulaGrid.Rows[0].Selected = true;
                _formulaGrid.CurrentCell = _formulaGrid.Rows[0].Cells[0];
            }
        }
        finally
        {
            _formulaGrid.ResumeLayout(true);
            _loading = false;
        }

        SelectCurrentFormula();
    }

    private void SelectCurrentFormula()
    {
        if (_loading)
        {
            return;
        }

        ApplySelectedFormula(showErrors: false);
        _selectedFormula = _formulaGrid.SelectedRows.Count > 0 ? _formulaGrid.SelectedRows[0].Tag as FormulaDefinition : null;
        _selectedOriginalId = _selectedFormula?.Id ?? "";
        LoadSelectedFormula();
    }

    private void LoadSelectedFormula()
    {
        _loading = true;
        try
        {
            var formula = _selectedFormula;
            _idBox.Text = formula?.Id ?? "";
            _idBox.ReadOnly = formula?.BuiltIn == true;
            _nameBox.Text = formula?.DisplayName ?? "";
            _descriptionBox.Text = formula?.Description ?? "";
            _expressionBox.Text = formula?.Expression ?? "";
            _deleteButton.Enabled = formula is not null && !formula.BuiltIn;
            _applyButton.Enabled = formula is not null;
            _saveButton.Enabled = formula is not null;
            _validateButton.Enabled = formula is not null;
            RefreshVariableGrid();
            RefreshReferences();
        }
        finally
        {
            _loading = false;
        }

        RefreshPreview();
    }

    private void RefreshVariableGrid()
    {
        _variableGrid.Rows.Clear();
        AddVariableRow("basePower", T("formulaEditor.variable.basePower", "技能或效果基础值"), 20.ToString(CultureInfo.CurrentCulture));
        AddVariableRow("multiplier", T("formulaEditor.variable.multiplier", "倍率"), 1.2.ToString(CultureInfo.CurrentCulture));
        foreach (var stat in _context.Project.AssetLibrary.Stats.Where(IsNumericStat).OrderBy(v => v.Key, StringComparer.OrdinalIgnoreCase))
        {
            AddVariableRow($"caster.{stat.Key}", string.Format(CultureInfo.CurrentCulture, T("formulaEditor.variable.casterStat", "施放者：{0}"), LocalizedStat(stat)), FormatStatValue(stat, stat.DefaultValue));
            AddVariableRow($"target.{stat.Key}", string.Format(CultureInfo.CurrentCulture, T("formulaEditor.variable.targetStat", "目标：{0}"), LocalizedStat(stat)), FormatStatValue(stat, stat.DefaultValue));
        }
    }

    private void AddVariableRow(string token, string source, string value)
    {
        _variableGrid.Rows.Add(token, source, value);
    }

    private string FormatStatValue(StatDefinition stat, double value)
    {
        if (string.Equals(stat.ValueType, "Integer", StringComparison.OrdinalIgnoreCase))
        {
            return ((int)Math.Round(value, 0, MidpointRounding.AwayFromZero)).ToString(CultureInfo.CurrentCulture);
        }

        return value.ToString("0.###", CultureInfo.CurrentCulture);
    }

    private static bool IsNumericStat(StatDefinition stat)
    {
        return string.Equals(stat.ValueType, "Number", StringComparison.OrdinalIgnoreCase)
            || string.Equals(stat.ValueType, "Integer", StringComparison.OrdinalIgnoreCase);
    }

    private void InsertSelectedVariable()
    {
        if (_variableGrid.SelectedRows.Count == 0)
        {
            return;
        }

        var token = _variableGrid.SelectedRows[0].Cells["token"].Value?.ToString();
        if (!string.IsNullOrWhiteSpace(token))
        {
            InsertExpressionToken(token);
        }
    }

    private void InsertExpressionToken(string template)
    {
        _expressionBox.Focus();
        var start = _expressionBox.SelectionStart;
        var selected = _expressionBox.SelectedText;
        var insert = template;
        var marker = insert.IndexOf('|');
        var caretOffset = insert.Length;
        if (marker >= 0)
        {
            insert = insert.Remove(marker, 1).Insert(marker, selected);
            caretOffset = marker + selected.Length;
        }

        _expressionBox.SelectedText = insert;
        _expressionBox.SelectionStart = Math.Min(_expressionBox.TextLength, start + caretOffset);
        _expressionBox.SelectionLength = 0;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_selectedFormula is null)
        {
            _resultLabel.Text = T("formulaEditor.noSelection", "未选择公式");
            _usageLabel.Text = "";
            return;
        }

        try
        {
            var result = FormulaExpressionEvaluator.Evaluate(_expressionBox.Text, ReadPreviewVariables());
            _resultLabel.Text = string.Format(CultureInfo.CurrentCulture, T("formulaEditor.resultOk", "结果：{0}"), result.ToString("0.###", CultureInfo.CurrentCulture));
            _usageLabel.Text = T("formulaEditor.resultHint", "修改测试值后会立即重新计算。保存后，技能和玩法效果通过公式 ID 调用该公式。");
        }
        catch (Exception ex)
        {
            _resultLabel.Text = string.Format(CultureInfo.CurrentCulture, T("formulaEditor.resultError", "无法计算：{0}"), ex.Message);
            _usageLabel.Text = T("formulaEditor.resultErrorHint", "请检查变量是否存在、括号是否闭合、函数参数数量是否正确。");
        }
    }

    private Dictionary<string, double> ReadPreviewVariables()
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (DataGridViewRow row in _variableGrid.Rows)
        {
            var token = row.Cells["token"].Value?.ToString();
            var valueText = row.Cells["value"].Value?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (!double.TryParse(valueText, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
                && !double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                throw new FormatException($"变量 {token} 的测试值不是数字。");
            }

            values[token] = value;
        }

        return values;
    }

    private bool ApplySelectedFormula(bool showErrors)
    {
        if (_selectedFormula is null || _loading)
        {
            return true;
        }

        var newId = _idBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(newId))
        {
            return ShowApplyError(showErrors, T("formulaEditor.error.emptyId", "公式 ID 不能为空。"));
        }

        if (_context.Project.AssetLibrary.Formulas.Any(v => !ReferenceEquals(v, _selectedFormula) && string.Equals(v.Id, newId, StringComparison.OrdinalIgnoreCase)))
        {
            return ShowApplyError(showErrors, T("formulaEditor.error.duplicateId", "公式 ID 已存在。"));
        }

        if (string.IsNullOrWhiteSpace(_expressionBox.Text))
        {
            return ShowApplyError(showErrors, T("formulaEditor.error.emptyExpression", "表达式不能为空。"));
        }

        var oldId = _selectedOriginalId;
        _selectedFormula.Id = newId;
        _selectedFormula.DisplayName = _nameBox.Text.Trim();
        _selectedFormula.Description = _descriptionBox.Text.Trim();
        _selectedFormula.Expression = _expressionBox.Text.Trim();
        _selectedFormula.FormulaKind = "expression";
        _selectedFormula.GraphId = "";

        if (!string.IsNullOrWhiteSpace(oldId) && !string.Equals(oldId, newId, StringComparison.OrdinalIgnoreCase))
        {
            UpdateFormulaReferences(oldId, newId);
            _selectedOriginalId = newId;
        }

        RefreshReferences();
        RefreshPreview();
        return true;
    }

    private bool ShowApplyError(bool showErrors, string message)
    {
        if (showErrors)
        {
            MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        return false;
    }

    private void UpdateFormulaReferences(string oldId, string newId)
    {
        foreach (var skill in _context.Project.AssetLibrary.Skills.Where(v => string.Equals(v.FormulaId, oldId, StringComparison.OrdinalIgnoreCase)))
        {
            skill.FormulaId = newId;
        }

        foreach (var effect in _context.Project.AssetLibrary.GameplayEffects.Where(v => string.Equals(v.FormulaId, oldId, StringComparison.OrdinalIgnoreCase)))
        {
            effect.FormulaId = newId;
        }

        foreach (var effect in _context.Project.AssetLibrary.GameplayEffects)
        {
            UpdateFormulaParameterReferences(effect.Parameters, oldId, newId);
        }

        foreach (var skill in _context.Project.AssetLibrary.Skills)
        {
            UpdateFormulaReferenceValues(skill.Effects, oldId, newId);
        }

        foreach (var item in _context.Project.AssetLibrary.Items)
        {
            UpdateFormulaReferenceValues(item.Effects, oldId, newId);
        }

        foreach (var projectile in _context.Project.AssetLibrary.Projectiles)
        {
            UpdateFormulaReferenceValues(projectile.Effects, oldId, newId);
        }

        foreach (var status in _context.Project.AssetLibrary.Statuses)
        {
            UpdateFormulaReferenceValues(status.OnApplyEffects, oldId, newId);
            UpdateFormulaReferenceValues(status.PeriodicEffects, oldId, newId);
        }
    }

    private void UpdateFormulaReferenceValues(IEnumerable<GameplayEffectReference> references, string oldId, string newId)
    {
        foreach (var reference in references)
        {
            var effect = _context.Project.AssetLibrary.GameplayEffects.FirstOrDefault(v => string.Equals(v.Id, reference.EffectId, StringComparison.OrdinalIgnoreCase));
            if (effect is null)
            {
                continue;
            }

            foreach (var parameter in effect.Parameters.Where(IsFormulaReferenceParameter))
            {
                var value = reference.Parameters.FirstOrDefault(v => string.Equals(v.Key, parameter.Key, StringComparison.OrdinalIgnoreCase));
                if (value is not null && string.Equals(value.Value, oldId, StringComparison.OrdinalIgnoreCase))
                {
                    value.Value = newId;
                }
            }
        }
    }

    private static void UpdateFormulaParameterReferences(IEnumerable<EffectParameterDefinition> parameters, string oldId, string newId)
    {
        foreach (var parameter in parameters.Where(IsFormulaReferenceParameter))
        {
            if (string.Equals(parameter.DefaultValue, oldId, StringComparison.OrdinalIgnoreCase))
            {
                parameter.DefaultValue = newId;
            }
        }
    }

    private static bool IsFormulaReferenceParameter(EffectParameterDefinition parameter)
    {
        return parameter.ValueType is EffectParameterValueType.Choice or EffectParameterValueType.AssetRef
            && string.Equals(parameter.OptionSourceId, "formula", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyAndRefresh()
    {
        if (!ApplySelectedFormula(showErrors: true))
        {
            return;
        }

        RefreshFormulaGrid(_selectedFormula?.Id);
    }

    private void SaveProject()
    {
        if (!ApplySelectedFormula(showErrors: true))
        {
            return;
        }

        _projectService.SaveProject(_context);
        RefreshFormulaGrid(_selectedFormula?.Id);
        MessageBox.Show(this, T("formulaEditor.saveSuccess", "公式已保存。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void CreateFormula()
    {
        ApplySelectedFormula(showErrors: false);
        var formula = new FormulaDefinition
        {
            Id = UniqueId("formula.custom", _context.Project.AssetLibrary.Formulas.Select(v => v.Id)),
            DisplayName = T("formulaEditor.newName", "新公式"),
            Description = T("formulaEditor.newDescription", "自定义数值计算公式。"),
            FormulaKind = "expression",
            Expression = "basePower"
        };
        _context.Project.AssetLibrary.Formulas.Add(formula);
        RefreshFormulaGrid(formula.Id);
    }

    private void DeleteFormula()
    {
        if (_selectedFormula is null)
        {
            return;
        }

        if (_selectedFormula.BuiltIn)
        {
            MessageBox.Show(this, T("common.cannotDeleteBuiltin", "内置内容不能删除。"), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var references = GetFormulaReferenceLines(_selectedFormula.Id).ToList();
        if (references.Count > 0)
        {
            MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("formulaEditor.deleteBlocked", "该公式仍被 {0} 处资产引用，请先解除引用。"), references.Count), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(this, string.Format(CultureInfo.CurrentCulture, T("common.deleteConfirm", "确定删除“{0}”吗？"), Localized(_selectedFormula)), Text, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
        if (confirm != DialogResult.OK)
        {
            return;
        }

        _context.Project.AssetLibrary.Formulas.Remove(_selectedFormula);
        _selectedFormula = null;
        RefreshFormulaGrid();
    }

    private void RefreshReferences()
    {
        if (_selectedFormula is null)
        {
            _referencesBox.Text = "";
            return;
        }

        var lines = GetFormulaReferenceLines(_selectedFormula.Id).ToList();
        _referencesBox.Text = lines.Count == 0
            ? T("formulaEditor.noReferences", "当前没有资产引用这个公式。")
            : string.Join(Environment.NewLine, lines);
    }

    private IEnumerable<string> GetFormulaReferenceLines(string formulaId)
    {
        foreach (var effect in _context.Project.AssetLibrary.GameplayEffects.Where(v => string.Equals(v.FormulaId, formulaId, StringComparison.OrdinalIgnoreCase)))
        {
            yield return string.Format(CultureInfo.CurrentCulture, T("formulaEditor.reference.effect", "玩法效果：{0} [{1}]"), Localized(effect.DisplayName, effect.DisplayNameKey, effect.Id), effect.Id);
        }

        foreach (var skill in _context.Project.AssetLibrary.Skills.Where(v => string.Equals(v.FormulaId, formulaId, StringComparison.OrdinalIgnoreCase)))
        {
            yield return string.Format(CultureInfo.CurrentCulture, T("formulaEditor.reference.skill", "技能直接引用：{0} [{1}]"), Localized(skill.DisplayName, skill.DisplayNameKey, skill.Id), skill.Id);
        }
    }

    private string Localized(FormulaDefinition formula)
    {
        return Localized(formula.DisplayName, formula.DisplayNameKey, formula.Id);
    }

    private string LocalizedStat(StatDefinition stat)
    {
        return Localized(stat.DisplayName, stat.DisplayNameKey, stat.Key);
    }

    private string Localized(string text, string key, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            var value = _localization.T(key);
            if (!string.Equals(value, key, StringComparison.Ordinal))
            {
                return value;
            }
        }

        return string.IsNullOrWhiteSpace(text) ? fallback : text;
    }

    private string T(string key, string fallback)
    {
        var value = _localization.T(key);
        return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
    }

    private static string UniqueId(string prefix, IEnumerable<string> existingIds)
    {
        var existing = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!existing.Contains(prefix))
        {
            return prefix;
        }

        for (var i = 1; i < 10_000; i++)
        {
            var candidate = $"{prefix}.{i}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{prefix}.{Guid.NewGuid():N}";
    }

    private sealed class BufferedGrid : DataGridView
    {
        public BufferedGrid()
        {
            DoubleBuffered = true;
        }
    }
}
