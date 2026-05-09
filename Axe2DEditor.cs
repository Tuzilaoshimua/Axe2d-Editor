using Axe2DEditor.Core.Projects;
using Axe2DEditor.Editor.Controls;
using Axe2DEditor.Editor.Localization;
using Axe2DEditor.Editor.Logging;
using Axe2DEditor.Editor.Modules;
using Axe2DEditor.Runtime;
using System.Text.Json;

namespace Axe2DEditor;

public partial class MainForm : Form
{
    private const string NodeKindFolder = "folder";
    private const string NodeKindItem = "item";
    private const string NodeTypeObject = "object";
    private const string NodeTypeUi = "ui";
    private const string NodeTypeCamera = "camera";
    private const string NodeTypeEmpty = "empty";
    private const int MaxHistoryEntries = 100;

    private readonly ProjectService _projectService = new();
    private readonly EditorLogger _logger = new();
    private readonly LocalizationService _localization = new();
    private readonly EditorSettingsService _settingsService = new();
    private readonly Dictionary<string, Form> _openModules = new(StringComparer.OrdinalIgnoreCase);
    private RuntimePreviewForm? _runtimeWindow;
    private readonly ContextMenuStrip _hierarchyNodeMenu = new();
    private readonly ContextMenuStrip _hierarchyBlankMenu = new();
    private readonly ToolStripMenuItem _hierarchyCreateRootMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateRootObjectMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateRootUiMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateRootCameraMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateRootEmptyMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateNodeMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateNodeObjectMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateNodeUiMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateNodeCameraMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyCreateNodeEmptyMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyDuplicateMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyRenameMenuItem = new();
    private readonly ToolStripMenuItem _hierarchyDeleteMenuItem = new();
    private readonly ContextMenuStrip _assetsNodeMenu = new();
    private readonly ContextMenuStrip _assetsBlankMenu = new();
    private readonly ToolStripMenuItem _assetsCreateNodeMenuItem = new();
    private readonly ToolStripMenuItem _assetsCreateFolderMenuItem = new();
    private readonly ToolStripMenuItem _assetsCreateSceneMenuItem = new();
    private readonly ToolStripMenuItem _assetsCreatePrefabMenuItem = new();
    private readonly ToolStripMenuItem _assetsCreateMaterialMenuItem = new();
    private readonly ToolStripMenuItem _assetsCreateScriptMenuItem = new();
    private readonly ToolStripMenuItem _assetsCreateAnimationMenuItem = new();
    private readonly ToolStripMenuItem _assetsImportMenuItem = new();
    private readonly ToolStripMenuItem _assetsExportMenuItem = new();
    private readonly ToolStripMenuItem _assetsOpenInExplorerMenuItem = new();
    private readonly ToolStripMenuItem _assetsRenameMenuItem = new();
    private readonly ToolStripMenuItem _assetsDeleteMenuItem = new();
    private readonly Panel _alignPopupPanel = new();
    private readonly Label _alignPopupAlignLabel = new();
    private readonly Label _alignPopupDistributeLabel = new();
    private readonly Stack<List<ProjectTreeNode>> _undoStack = new();
    private readonly Stack<List<ProjectTreeNode>> _redoStack = new();

    private EditorSettings _settings = new();
    private ProjectContext? _currentProject;
    private TreeView? _contextTreeView;
    private TreeView? _dragSourceTreeView;
    private int _newNodeCounter = 1;
    private bool _suppressInspectorEvents;
    private TreeNode? _inspectorNode;
    private readonly List<SceneObjectPoint> _scenePoints = [];
    private readonly List<TreeNode> _scenePointNodes = [];
    private int _selectedScenePointIndex = -1;
    private bool _sceneDragHistoryCaptured;
    private bool _suppressHistoryTracking;
    private float _sceneDragStartRotation;
    private float _sceneDragStartScale = 1f;
    private float _sceneDragStartX;
    private float _sceneDragStartY;
    private float _sceneDragStartMouseAngle;
    private float _sceneDragStartMouseDistance = 1f;
    private bool _sceneTransformDragInitialized;
    private SceneGizmoHitTarget _sceneHoverTarget = SceneGizmoHitTarget.None;
    private readonly Label _inspectorNameLabel = new();
    private readonly TextBox _inspectorNameBox = new();
    private readonly Label _inspectorTypeLabel = new();
    private readonly ComboBox _inspectorTypeCombo = new();
    private readonly Label _inspectorXLabel = new();
    private readonly NumericUpDown _inspectorXBox = new();
    private readonly Label _inspectorYLabel = new();
    private readonly NumericUpDown _inspectorYBox = new();
    private readonly Label _inspectorPathLabel = new();
    private readonly TextBox _inspectorPathBox = new();
    private readonly Label _inspectorRotationLabel = new();
    private readonly NumericUpDown _inspectorRotationBox = new();
    private readonly Label _inspectorScaleLabel = new();
    private readonly NumericUpDown _inspectorScaleBox = new();
    private readonly Label _inspectorCameraModeLabel = new();
    private readonly ComboBox _inspectorCameraModeCombo = new();
    private readonly Label _inspectorCameraTargetLabel = new();
    private readonly ComboBox _inspectorCameraTargetBox = new();
    private readonly Label _inspectorCameraSmoothLabel = new();
    private readonly NumericUpDown _inspectorCameraSmoothBox = new();
    private readonly Label _inspectorCameraZoomLabel = new();
    private readonly NumericUpDown _inspectorCameraZoomBox = new();
    private SceneTool _activeSceneTool = SceneTool.Select;
    private string _lastHistorySnapshotJson = string.Empty;

    private enum SceneTool
    {
        Select,
        Move,
        Rotate,
        Scale,
        Rect
    }

    public MainForm()
    {
        InitializeComponent();
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        _logger.EntryAdded += Logger_EntryAdded;
        InitializeInspectorEditor();
        InitializeTreeMenus();
        InitializeSceneTools();
        LoadEditorSettings();
        KeyDown += MainForm_KeyDown;
        Shown += (_, _) => BeginInvoke(new Action(NormalizeSplitters));
    }

    private void Form1_Load(object sender, EventArgs e)
    {
        ApplyLocalization();
        ApplyTheme();
        RefreshProjectState();
        _logger.Info(_localization.T("log.started"));
    }

    private void NewProjectMenuItem_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = _localization.T("dialog.newProject.description"),
            UseDescriptionForTitle = true
        };
        ApplyFolderDialogDirectory(dialog, _settings.LastProjectDirectory);
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        var selectedPath = dialog.SelectedPath;
        SaveLastProjectDirectory(selectedPath);
        var projectName = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(projectName))
        {
            projectName = "Untitled Axe2D Project";
        }

        try
        {
            StopRuntime();
            CloseAllModules();
            _currentProject = _projectService.CreateProject(selectedPath, projectName);
            RefreshProjectState();
            _logger.Info(_localization.Format("log.createdProject", _currentProject.Project.Name));
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to create project: {ex.Message}");
            System.Windows.Forms.MessageBox.Show(
                this,
                $"{_localization.T("dialog.createProjectFailed")}: {ex.Message}",
                _localization.T("app.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OpenProjectMenuItem_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Axe2D Project (project.json)|project.json|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            Title = _localization.T("dialog.openProject.title")
        };
        ApplyFileDialogDirectory(dialog, _settings.LastProjectDirectory);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        SaveLastProjectDirectory(Path.GetDirectoryName(dialog.FileName));
        try
        {
            StopRuntime();
            CloseAllModules();
            _currentProject = _projectService.OpenProject(dialog.FileName);
            RefreshProjectState();
            _logger.Info(_localization.Format("log.openedProject", _currentProject.Project.Name));
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to open project: {ex.Message}");
            System.Windows.Forms.MessageBox.Show(
                this,
                $"{_localization.T("dialog.openProjectFailed")}: {ex.Message}",
                _localization.T("app.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void SaveProjectMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentProject is null)
        {
            _logger.Warning(_localization.T("log.noProject"));
            return;
        }

        try
        {
            SaveTreeData();
            _logger.Info(_localization.Format("log.savedProject", _currentProject.Project.Name));
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save project: {ex.Message}");
            System.Windows.Forms.MessageBox.Show(
                this,
                $"{_localization.T("dialog.saveProjectFailed")}: {ex.Message}",
                _localization.T("app.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void OpenDataEditorMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentProject is null)
        {
            _logger.Warning(_localization.T("log.noProject"));
            return;
        }

        const string key = "module:data-editor";
        if (_openModules.TryGetValue(key, out var opened) && !opened.IsDisposed)
        {
            opened.Activate();
            return;
        }

        var form = new DataEditorV2Form(_currentProject, _projectService, _localization) { Owner = this };
        form.FormClosed += (_, _) => _openModules.Remove(key);
        _openModules[key] = form;
        form.Show(this);
    }

    private void OpenFormulaEditorMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentProject is null)
        {
            _logger.Warning(_localization.T("log.noProject"));
            return;
        }

        const string key = "module:formula-editor";
        if (_openModules.TryGetValue(key, out var opened) && !opened.IsDisposed)
        {
            opened.Activate();
            return;
        }

        var form = new FormulaEditorForm(_currentProject, _projectService, _localization) { Owner = this };
        form.FormClosed += (_, _) => _openModules.Remove(key);
        _openModules[key] = form;
        form.Show(this);
    }

    private void OpenMapEditorMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentProject is null)
        {
            _logger.Warning(_localization.T("log.noProject"));
            return;
        }

        const string key = "module:map-editor";
        if (_openModules.TryGetValue(key, out var opened) && !opened.IsDisposed)
        {
            opened.Activate();
            return;
        }

        var form = new MapEditorForm(_currentProject, _projectService, _localization, _settings, _settingsService) { Owner = this };
        form.FormClosed += (_, _) => _openModules.Remove(key);
        _openModules[key] = form;
        form.Show(this);
    }

    private static void ApplyFileDialogDirectory(FileDialog dialog, string? directory)
    {
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            dialog.InitialDirectory = directory;
        }
    }

    private static void ApplyFolderDialogDirectory(FolderBrowserDialog dialog, string? directory)
    {
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            dialog.SelectedPath = directory;
        }
    }

    private void SaveLastProjectDirectory(string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        _settings.LastProjectDirectory = directory;
        _settingsService.Save(_settings);
    }

    private void OpenEventGraphEditorMenuItem_Click(object? sender, EventArgs e)
    {
        if (_currentProject is null)
        {
            _logger.Warning(_localization.T("log.noProject"));
            return;
        }

        const string key = "module:event-graph-editor";
        if (_openModules.TryGetValue(key, out var opened) && !opened.IsDisposed)
        {
            opened.Activate();
            return;
        }

        var form = new EventGraphEditorForm(_currentProject, _projectService, _localization) { Owner = this };
        form.FormClosed += (_, _) => _openModules.Remove(key);
        _openModules[key] = form;
        form.Show(this);
    }

    private void RunProjectButton_Click(object? sender, EventArgs e)
    {
        StartRuntime("run");
    }

    private void TestProjectButton_Click(object? sender, EventArgs e)
    {
        StartRuntime("test");
    }

    private void StopRuntimeButton_Click(object? sender, EventArgs e)
    {
        StopRuntime();
    }

    private void LanguageZhCnMenuItem_Click(object? sender, EventArgs e) => ChangeLanguage("zh-CN");

    private void LanguageEnMenuItem_Click(object? sender, EventArgs e) => ChangeLanguage("en");

    private void LanguageJaJpMenuItem_Click(object? sender, EventArgs e) => ChangeLanguage("ja-JP");

    private void LightThemeMenuItem_Click(object? sender, EventArgs e)
    {
        _settings.Theme = "light";
        _settingsService.Save(_settings);
        ApplyTheme();
    }

    private void DarkThemeMenuItem_Click(object? sender, EventArgs e)
    {
        _settings.Theme = "dark";
        _settingsService.Save(_settings);
        ApplyTheme();
    }

    private void ChangeLanguage(string language)
    {
        try
        {
            _localization.Load(language);
            _settings.Language = _localization.CurrentLanguage;
            _settingsService.Save(_settings);
            ApplyLocalization();
            RefreshProjectState();
            _logger.Info(_localization.Format("log.languageChanged", GetLanguageDisplayName(language)));
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to change language: {ex.Message}");
        }
    }

    private void LoadEditorSettings()
    {
        _settings = _settingsService.Load();
        try
        {
            _localization.Load(_settings.Language);
        }
        catch
        {
            _settings.Language = "zh-CN";
            _localization.Load(_settings.Language);
        }
    }

    private void ApplyLocalization()
    {
        fileToolStripMenuItem.Text = _localization.T("menu.file");
        newProjectToolStripMenuItem.Text = WithEmojiPrefix("📁", _localization.T("menu.file.newProject"));
        openProjectToolStripMenuItem.Text = WithEmojiPrefix("📂", _localization.T("menu.file.openProject"));
        saveProjectToolStripMenuItem.Text = WithEmojiPrefix("💾", _localization.T("menu.file.saveProject"));
        exitToolStripMenuItem.Text = WithEmojiPrefix("⏻", _localization.T("menu.file.exit"));

        modulesToolStripMenuItem.Text = _localization.T("menu.modules");
        dataEditorToolStripMenuItem.Text = WithEmojiPrefix("🧩", _localization.T("module.dataEditor"));
        formulaEditorToolStripMenuItem.Text = WithEmojiPrefix("ƒ", _localization.T("module.formulaEditor"));
        mapEditorToolStripMenuItem.Text = WithEmojiPrefix("🗺", _localization.T("module.mapEditor"));
        eventGraphEditorToolStripMenuItem.Text = WithEmojiPrefix("🧠", _localization.T("module.eventGraphEditor"));

        optionsToolStripMenuItem.Text = _localization.T("menu.options");
        languageToolStripMenuItem.Text = WithEmojiPrefix("🌐", _localization.T("menu.language"));
        themeToolStripMenuItem.Text = WithEmojiPrefix("🎨", _localization.T("menu.theme"));
        languageZhCnToolStripMenuItem.Text = _localization.T("language.chinese");
        languageEnToolStripMenuItem.Text = _localization.T("language.english");
        languageJaJpToolStripMenuItem.Text = _localization.T("language.japanese");
        lightThemeToolStripMenuItem.Text = _localization.T("theme.light");
        darkThemeToolStripMenuItem.Text = _localization.T("theme.dark");

        toolNewProjectButton.Text = "📁";
        toolOpenProjectButton.Text = "📂";
        toolSaveProjectButton.Text = "💾";
        toolDataEditorButton.Text = "🧩";
        toolFormulaEditorButton.Text = "ƒ";
        toolMapEditorButton.Text = "🗺";
        toolEventGraphEditorButton.Text = "🧠";
        toolRunButton.Text = "▶";
        toolTestButton.Text = "🧪";
        toolStopButton.Text = "■";
        toolNewProjectButton.ToolTipText = _localization.T("menu.file.newProject");
        toolOpenProjectButton.ToolTipText = _localization.T("menu.file.openProject");
        toolSaveProjectButton.ToolTipText = _localization.T("menu.file.saveProject");
        toolDataEditorButton.ToolTipText = _localization.T("module.dataEditor");
        toolFormulaEditorButton.ToolTipText = _localization.T("module.formulaEditor");
        toolMapEditorButton.ToolTipText = _localization.T("module.mapEditor");
        toolEventGraphEditorButton.ToolTipText = _localization.T("module.eventGraphEditor");
        toolRunButton.ToolTipText = _localization.T("toolbar.run");
        toolTestButton.ToolTipText = _localization.T("toolbar.test");
        toolStopButton.ToolTipText = _localization.T("toolbar.stop");
        _toolSelectSceneButton.Text = "🖱";
        _toolMoveSceneButton.Text = "✥";
        _toolRotateSceneButton.Text = "↻";
        _toolScaleSceneButton.Text = "⤢";
        _toolRectSceneButton.Text = "⌖";
        _toolAlignSceneButton.Text = "☰";
        _toolUndoSceneButton.Text = "↶";
        _toolRedoSceneButton.Text = "↷";
        _toolToggleGridButton.Text = "⊞";
        _toolSelectSceneButton.ToolTipText = _localization.T("tool.scene.select");
        _toolMoveSceneButton.ToolTipText = _localization.T("tool.scene.move");
        _toolRotateSceneButton.ToolTipText = _localization.T("tool.scene.rotate");
        _toolScaleSceneButton.ToolTipText = _localization.T("tool.scene.scale");
        _toolRectSceneButton.ToolTipText = _localization.T("tool.scene.rect");
        _toolAlignSceneButton.ToolTipText = _localization.T("tool.scene.align");
        _toolUndoSceneButton.ToolTipText = _localization.T("tool.scene.undo");
        _toolRedoSceneButton.ToolTipText = _localization.T("tool.scene.redo");
        _toolToggleGridButton.ToolTipText = _localization.T("panel.scene");

        hierarchyGroupBox.Text = _localization.T("panel.hierarchy");
        assetsGroupBox.Text = _localization.T("panel.assets");
        sceneGroupBox.Text = _localization.T("panel.scene");
        inspectorGroupBox.Text = _localization.T("panel.inspector");
        logGroupBox.Text = _localization.T("dashboard.logTitle");
        previewGroupBox.Text = _localization.T("panel.preview");
        _hierarchyCreateRootMenuItem.Text = _localization.T("tree.menu.hierarchy.create");
        _hierarchyCreateRootObjectMenuItem.Text = _localization.T("tree.menu.hierarchy.create.object");
        _hierarchyCreateRootUiMenuItem.Text = _localization.T("tree.menu.hierarchy.create.ui");
        _hierarchyCreateRootCameraMenuItem.Text = _localization.T("tree.menu.hierarchy.create.camera");
        _hierarchyCreateRootEmptyMenuItem.Text = _localization.T("tree.menu.hierarchy.create.empty");
        _hierarchyCreateNodeMenuItem.Text = _localization.T("tree.menu.hierarchy.create");
        _hierarchyCreateNodeObjectMenuItem.Text = _localization.T("tree.menu.hierarchy.create.object");
        _hierarchyCreateNodeUiMenuItem.Text = _localization.T("tree.menu.hierarchy.create.ui");
        _hierarchyCreateNodeCameraMenuItem.Text = _localization.T("tree.menu.hierarchy.create.camera");
        _hierarchyCreateNodeEmptyMenuItem.Text = _localization.T("tree.menu.hierarchy.create.empty");
        _hierarchyCreateRootObjectMenuItem.ShortcutKeyDisplayString = "Ctrl+1";
        _hierarchyCreateRootUiMenuItem.ShortcutKeyDisplayString = "Ctrl+2";
        _hierarchyCreateRootCameraMenuItem.ShortcutKeyDisplayString = "Ctrl+3";
        _hierarchyCreateRootEmptyMenuItem.ShortcutKeyDisplayString = "Ctrl+4";
        _hierarchyCreateNodeObjectMenuItem.ShortcutKeyDisplayString = "Ctrl+1";
        _hierarchyCreateNodeUiMenuItem.ShortcutKeyDisplayString = "Ctrl+2";
        _hierarchyCreateNodeCameraMenuItem.ShortcutKeyDisplayString = "Ctrl+3";
        _hierarchyCreateNodeEmptyMenuItem.ShortcutKeyDisplayString = "Ctrl+4";
        _hierarchyDuplicateMenuItem.Text = _localization.T("tree.menu.hierarchy.duplicate");
        _hierarchyRenameMenuItem.Text = _localization.T("tree.menu.rename");
        _hierarchyDeleteMenuItem.Text = _localization.T("tree.menu.delete");
        _assetsCreateNodeMenuItem.Text = _localization.T("tree.menu.asset.create");
        _assetsCreateFolderMenuItem.Text = _localization.T("tree.menu.asset.create.folder");
        _assetsCreateSceneMenuItem.Text = _localization.T("tree.menu.asset.create.scene");
        _assetsCreatePrefabMenuItem.Text = _localization.T("tree.menu.asset.create.prefab");
        _assetsCreateMaterialMenuItem.Text = _localization.T("tree.menu.asset.create.material");
        _assetsCreateScriptMenuItem.Text = _localization.T("tree.menu.asset.create.script");
        _assetsCreateAnimationMenuItem.Text = _localization.T("tree.menu.asset.create.animation");
        _assetsImportMenuItem.Text = _localization.T("tree.menu.asset.import");
        _assetsExportMenuItem.Text = _localization.T("tree.menu.asset.export");
        _assetsOpenInExplorerMenuItem.Text = _localization.T("tree.menu.asset.openInExplorer");
        _assetsRenameMenuItem.Text = _localization.T("tree.menu.rename");
        _assetsDeleteMenuItem.Text = _localization.T("tree.menu.delete");
        _inspectorNameLabel.Text = _localization.T("tree.details.name");
        _inspectorTypeLabel.Text = _localization.T("tree.details.type");
        _inspectorXLabel.Text = "X";
        _inspectorYLabel.Text = "Y";
        _inspectorRotationLabel.Text = _localization.T("inspector.rotation");
        _inspectorScaleLabel.Text = _localization.T("inspector.scale");
        _inspectorCameraModeLabel.Text = "相机模式";
        _inspectorCameraTargetLabel.Text = "跟随目标";
        _inspectorCameraSmoothLabel.Text = "平滑系数";
        _inspectorCameraZoomLabel.Text = "相机缩放";
        _alignPopupAlignLabel.Text = _localization.T("tool.scene.align");
        _alignPopupDistributeLabel.Text = _localization.T("tool.scene.distribute");
        _inspectorPathLabel.Text = _localization.T("tree.details.path");
        var selectedKind = _inspectorTypeCombo.SelectedItem?.ToString();
        _inspectorTypeCombo.Items.Clear();
        _inspectorTypeCombo.Items.Add(_localization.T("tree.nodeType.folder"));
        _inspectorTypeCombo.Items.Add(_localization.T("tree.nodeType.item"));
        if (!string.IsNullOrWhiteSpace(selectedKind) && _inspectorTypeCombo.Items.Contains(selectedKind))
        {
            _inspectorTypeCombo.SelectedItem = selectedKind;
        }
        else
        {
            _inspectorTypeCombo.SelectedIndex = 1;
        }
        UpdateInspectorCoordinateState();

        languageZhCnToolStripMenuItem.Checked = _localization.CurrentLanguage == "zh-CN";
        languageEnToolStripMenuItem.Checked = _localization.CurrentLanguage == "en";
        languageJaJpToolStripMenuItem.Checked = _localization.CurrentLanguage == "ja-JP";

        if (_contextTreeView?.SelectedNode is not null)
        {
            PopulateInspectorForNode(_contextTreeView.SelectedNode);
        }
        else if (_currentProject is null)
        {
            inspectorTextBox.Text = _localization.T("tree.noProject.detail");
        }
    }

    private void ApplyTheme()
    {
        if (_settings.Theme == "dark")
        {
            ApplyDarkTheme();
        }
        else
        {
            ApplyLightTheme();
        }

        lightThemeToolStripMenuItem.Checked = _settings.Theme == "light";
        darkThemeToolStripMenuItem.Checked = _settings.Theme == "dark";
    }

    private void ApplyLightTheme()
    {
        var back = Color.FromArgb(245, 245, 245);
        var panel = Color.White;
        var fore = Color.FromArgb(30, 30, 30);

        BackColor = back;
        ForeColor = fore;
        ApplyControlTheme(this, back, panel, fore, false);
    }

    private void ApplyDarkTheme()
    {
        var back = Color.FromArgb(31, 31, 31);
        var panel = Color.FromArgb(41, 41, 41);
        var fore = Color.FromArgb(230, 230, 230);

        BackColor = back;
        ForeColor = fore;
        ApplyControlTheme(this, back, panel, fore, true);
    }

    private static void ApplyControlTheme(Control parent, Color back, Color panel, Color fore, bool dark)
    {
        foreach (Control control in parent.Controls)
        {
            switch (control)
            {
                case TableLayoutPanel:
                case FlowLayoutPanel:
                    control.BackColor = back;
                    control.ForeColor = fore;
                    break;
                case GroupBox:
                case SplitContainer:
                    control.BackColor = panel;
                    control.ForeColor = fore;
                    break;
                case TreeView tree:
                    tree.BackColor = dark ? Color.FromArgb(39, 39, 39) : Color.White;
                    tree.ForeColor = fore;
                    tree.LineColor = dark ? Color.FromArgb(95, 95, 95) : Color.Gray;
                    break;
                case TextBox box:
                    box.BackColor = dark ? Color.FromArgb(32, 32, 32) : Color.White;
                    box.ForeColor = fore;
                    break;
                case MenuStrip:
                case StatusStrip:
                    control.BackColor = dark ? Color.FromArgb(45, 45, 45) : Color.WhiteSmoke;
                    control.ForeColor = fore;
                    break;
                default:
                    control.BackColor = back;
                    control.ForeColor = fore;
                    break;
            }

            if (control.HasChildren)
            {
                ApplyControlTheme(control, back, panel, fore, dark);
            }
        }
    }

    private void RefreshProjectState()
    {
        var hasProject = _currentProject is not null;
        saveProjectToolStripMenuItem.Enabled = hasProject;
        modulesToolStripMenuItem.Enabled = hasProject;
        toolSaveProjectButton.Enabled = hasProject;
        toolDataEditorButton.Enabled = hasProject;
        toolFormulaEditorButton.Enabled = hasProject;
        toolMapEditorButton.Enabled = hasProject;
        toolEventGraphEditorButton.Enabled = hasProject;
        toolRunButton.Enabled = hasProject;
        toolTestButton.Enabled = hasProject;
        toolStopButton.Enabled = _runtimeWindow is not null && !_runtimeWindow.IsDisposed;
        _toolSelectSceneButton.Enabled = hasProject;
        _toolMoveSceneButton.Enabled = hasProject;
        _toolRotateSceneButton.Enabled = hasProject;
        _toolScaleSceneButton.Enabled = hasProject;
        _toolRectSceneButton.Enabled = hasProject;
        _toolAlignSceneButton.Enabled = hasProject;
        _toolToggleGridButton.Enabled = hasProject;

        if (_currentProject is null)
        {
            Text = _localization.T("app.title");
            projectStatusLabel.Text = _localization.T("status.noProject");
            stateStatusLabel.Text = _localization.T("status.noProject");
            PopulateEmptyShell();
            UpdateHistoryButtonState();
            return;
        }

        var project = _currentProject.Project;
        Text = $"{_localization.T("app.title")} - {project.Name}";
        projectStatusLabel.Text = $"{project.Name} | {_currentProject.RootDirectory}";
        stateStatusLabel.Text = _localization.T("status.ready");
        PopulateProjectShell(project.Name);
        UpdateHistoryButtonState();
    }

    private void PopulateEmptyShell()
    {
        _suppressHistoryTracking = true;
        RenderTree(hierarchyTreeView, [new ProjectTreeNode { Name = _localization.T("tree.noProject"), Kind = NodeKindFolder }]);
        RenderTree(assetsTreeView, [new ProjectTreeNode { Name = _localization.T("tree.resources"), Kind = NodeKindFolder }]);

        inspectorTextBox.Text = _localization.T("tree.noProject.detail");
        _inspectorNode = null;
        _suppressInspectorEvents = true;
        _inspectorNameBox.Text = "";
        _inspectorTypeCombo.SelectedIndex = 1;
        _inspectorXBox.Value = 0;
        _inspectorYBox.Value = 0;
        _inspectorPathBox.Text = "";
        UpdateInspectorCoordinateState();
        _suppressInspectorEvents = false;
        previewBox.Image?.Dispose();
        previewBox.Image = null;
        _scenePoints.Clear();
        _scenePointNodes.Clear();
        _selectedScenePointIndex = -1;
        sceneCanvasPanel.SetObjects(_scenePoints);
        sceneCanvasPanel.SelectedIndex = -1;
        _suppressHistoryTracking = false;
        _undoStack.Clear();
        _redoStack.Clear();
        _sceneDragHistoryCaptured = false;
        _lastHistorySnapshotJson = string.Empty;
        UpdateHistoryButtonState();
    }

    private void PopulateProjectShell(string projectName)
    {
        _suppressHistoryTracking = true;
        RenderTree(hierarchyTreeView, _currentProject?.Project.HierarchyTree ?? []);
        RenderTree(assetsTreeView, _currentProject?.Project.ResourceTree ?? []);
        hierarchyTreeView.ExpandAll();
        assetsTreeView.ExpandAll();
        RefreshSceneFromHierarchyTree();

        inspectorTextBox.Text = _localization.Format("welcome.project", projectName, _currentProject?.RootDirectory ?? string.Empty);
        previewBox.Image?.Dispose();
        previewBox.Image = null;
        _undoStack.Clear();
        _redoStack.Clear();
        _suppressHistoryTracking = false;
        _sceneDragHistoryCaptured = false;
        _lastHistorySnapshotJson = JsonSerializer.Serialize(SerializeTree(hierarchyTreeView));
        UpdateHistoryButtonState();
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (!IsHandleCreated || !Visible)
        {
            return;
        }

        NormalizeSplitters();
    }

    private void NormalizeSplitters()
    {
        ClampSplitter(mainSplit, 220, 560);
        ClampSplitter(leftSplit, 120, 120);
        ClampSplitter(centerRightSplit, 420, 220);
        ClampSplitter(centerSplit, 320, 160);
        ClampSplitter(rightSplit, 360, 140);
    }

    private static void ClampSplitter(SplitContainer split, int panel1Min, int panel2Min)
    {
        SplitContainerLayout.ClampCurrentSafe(split, panel1Min, panel2Min);
    }

    private void InitializeTreeMenus()
    {
        hierarchyTreeView.AllowDrop = true;
        assetsTreeView.AllowDrop = true;
        hierarchyTreeView.HideSelection = false;
        assetsTreeView.HideSelection = false;
        hierarchyTreeView.LabelEdit = true;
        assetsTreeView.LabelEdit = true;

        hierarchyTreeView.NodeMouseClick += TreeView_NodeMouseClick;
        assetsTreeView.NodeMouseClick += TreeView_NodeMouseClick;
        hierarchyTreeView.MouseDown += TreeView_MouseDown;
        assetsTreeView.MouseDown += TreeView_MouseDown;
        hierarchyTreeView.AfterExpand += TreeView_AfterExpandOrCollapse;
        hierarchyTreeView.AfterCollapse += TreeView_AfterExpandOrCollapse;
        assetsTreeView.AfterExpand += TreeView_AfterExpandOrCollapse;
        assetsTreeView.AfterCollapse += TreeView_AfterExpandOrCollapse;
        hierarchyTreeView.AfterSelect += TreeView_AfterSelect;
        assetsTreeView.AfterSelect += TreeView_AfterSelect;
        hierarchyTreeView.ItemDrag += TreeView_ItemDrag;
        assetsTreeView.ItemDrag += TreeView_ItemDrag;
        hierarchyTreeView.KeyDown += HierarchyTreeView_KeyDown;
        hierarchyTreeView.DragEnter += TreeView_DragEnter;
        assetsTreeView.DragEnter += TreeView_DragEnter;
        hierarchyTreeView.DragOver += TreeView_DragOver;
        assetsTreeView.DragOver += TreeView_DragOver;
        hierarchyTreeView.DragDrop += TreeView_DragDrop;
        assetsTreeView.DragDrop += TreeView_DragDrop;

        _hierarchyCreateRootObjectMenuItem.Click += (_, _) => CreateHierarchyNodeFromMenu(_localization.T("tree.newHierarchy.object"), NodeKindItem, NodeTypeObject, false);
        _hierarchyCreateRootUiMenuItem.Click += (_, _) => CreateHierarchyNodeFromMenu(_localization.T("tree.newHierarchy.ui"), NodeKindItem, NodeTypeUi, false);
        _hierarchyCreateRootCameraMenuItem.Click += (_, _) => CreateHierarchyNodeFromMenu(_localization.T("tree.newHierarchy.camera"), NodeKindItem, NodeTypeCamera, false);
        _hierarchyCreateRootEmptyMenuItem.Click += (_, _) => CreateHierarchyNodeFromMenu(_localization.T("tree.newHierarchy.empty"), NodeKindItem, NodeTypeEmpty, false);
        _hierarchyCreateNodeObjectMenuItem.Click += (_, _) => CreateHierarchyNodeFromMenu(_localization.T("tree.newHierarchy.object"), NodeKindItem, NodeTypeObject, true);
        _hierarchyCreateNodeUiMenuItem.Click += (_, _) => CreateHierarchyNodeFromMenu(_localization.T("tree.newHierarchy.ui"), NodeKindItem, NodeTypeUi, true);
        _hierarchyCreateNodeCameraMenuItem.Click += (_, _) => CreateHierarchyNodeFromMenu(_localization.T("tree.newHierarchy.camera"), NodeKindItem, NodeTypeCamera, true);
        _hierarchyCreateNodeEmptyMenuItem.Click += (_, _) => CreateHierarchyNodeFromMenu(_localization.T("tree.newHierarchy.empty"), NodeKindItem, NodeTypeEmpty, true);
        _hierarchyDuplicateMenuItem.Click += (_, _) => DuplicateHierarchyNode();
        _hierarchyRenameMenuItem.Click += (_, _) => RenameNode();
        _hierarchyDeleteMenuItem.Click += (_, _) =>
        {
            _contextTreeView = hierarchyTreeView;
            DeleteNode();
        };
        _hierarchyCreateRootMenuItem.DropDownItems.Clear();
        _hierarchyCreateRootMenuItem.DropDownItems.AddRange([_hierarchyCreateRootObjectMenuItem, _hierarchyCreateRootUiMenuItem, _hierarchyCreateRootCameraMenuItem, _hierarchyCreateRootEmptyMenuItem]);

        _hierarchyCreateNodeMenuItem.DropDownItems.Clear();
        _hierarchyCreateNodeMenuItem.DropDownItems.AddRange([_hierarchyCreateNodeObjectMenuItem, _hierarchyCreateNodeUiMenuItem, _hierarchyCreateNodeCameraMenuItem, _hierarchyCreateNodeEmptyMenuItem]);

        _hierarchyBlankMenu.Items.Clear();
        _hierarchyBlankMenu.Items.AddRange([_hierarchyCreateRootMenuItem]);

        _hierarchyNodeMenu.Items.Clear();
        _hierarchyNodeMenu.Items.AddRange([_hierarchyCreateNodeMenuItem, _hierarchyDuplicateMenuItem, _hierarchyRenameMenuItem, _hierarchyDeleteMenuItem]);

        _assetsCreateFolderMenuItem.Click += (_, _) => AddAssetNodeByType(_localization.T("tree.newAssetFolder"), NodeKindFolder);
        _assetsCreateSceneMenuItem.Click += (_, _) => AddAssetNodeByType(_localization.T("tree.newAssetScene"), NodeKindItem);
        _assetsCreatePrefabMenuItem.Click += (_, _) => AddAssetNodeByType(_localization.T("tree.newAssetPrefab"), NodeKindItem);
        _assetsCreateMaterialMenuItem.Click += (_, _) => AddAssetNodeByType(_localization.T("tree.newAssetMaterial"), NodeKindItem);
        _assetsCreateScriptMenuItem.Click += (_, _) => AddAssetNodeByType(_localization.T("tree.newAssetScript"), NodeKindItem);
        _assetsCreateAnimationMenuItem.Click += (_, _) => AddAssetNodeByType(_localization.T("tree.newAssetAnimation"), NodeKindItem);
        _assetsImportMenuItem.Click += (_, _) => ImportAssets();
        _assetsExportMenuItem.Click += (_, _) => ExportSelectedAsset();
        _assetsOpenInExplorerMenuItem.Click += (_, _) => OpenAssetInExplorer();
        _assetsRenameMenuItem.Click += (_, _) => RenameNode();
        _assetsDeleteMenuItem.Click += (_, _) =>
        {
            _contextTreeView = assetsTreeView;
            DeleteNode();
        };
        _assetsCreateNodeMenuItem.DropDownItems.Clear();
        _assetsCreateNodeMenuItem.DropDownItems.AddRange([
            _assetsCreateFolderMenuItem,
            _assetsCreateSceneMenuItem,
            _assetsCreatePrefabMenuItem,
            _assetsCreateMaterialMenuItem,
            _assetsCreateScriptMenuItem,
            _assetsCreateAnimationMenuItem
        ]);

        _assetsBlankMenu.Items.Clear();
        _assetsBlankMenu.Items.AddRange([_assetsCreateNodeMenuItem, _assetsImportMenuItem, _assetsOpenInExplorerMenuItem]);

        _assetsNodeMenu.Items.Clear();
        _assetsNodeMenu.Items.AddRange([_assetsCreateNodeMenuItem, _assetsImportMenuItem, _assetsExportMenuItem, _assetsOpenInExplorerMenuItem, _assetsRenameMenuItem, _assetsDeleteMenuItem]);
    }

    private void HierarchyTreeView_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Delete)
        {
            return;
        }

        if (hierarchyTreeView.SelectedNode is null)
        {
            return;
        }

        _contextTreeView = hierarchyTreeView;
        DeleteNode();
        e.Handled = true;
    }

    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node is null)
        {
            return;
        }

        _contextTreeView = sender as TreeView;
        PopulateInspectorForNode(e.Node);
        UpdatePreviewForNode(_contextTreeView, e.Node);
        SyncSceneSelectionFromNode(e.Node);
        UpdateSceneStatusBar(e.Node);
    }

    private static void TreeView_AfterExpandOrCollapse(object? sender, TreeViewEventArgs e)
    {
        if (e.Node is not null)
        {
            UpdateNodeText(e.Node);
        }
    }

    private void TreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        if (sender is not TreeView tree)
        {
            return;
        }

        tree.SelectedNode = e.Node;
        _contextTreeView = tree;

        if (e.Button == MouseButtons.Right)
        {
            ShowTreeContextMenu(tree, e.Location);
        }
    }

    private void TreeView_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || sender is not TreeView tree)
        {
            return;
        }

        var node = tree.GetNodeAt(e.Location);
        if (node is not null)
        {
            tree.SelectedNode = node;
            _contextTreeView = tree;
            return;
        }

        tree.SelectedNode = null;
        _contextTreeView = tree;
        ShowTreeContextMenu(tree, e.Location);
    }

    private void ShowTreeContextMenu(TreeView tree, Point location)
    {
        if (ReferenceEquals(tree, hierarchyTreeView))
        {
            if (tree.GetNodeAt(location) is null)
            {
                _hierarchyBlankMenu.Show(tree, location);
            }
            else
            {
                _hierarchyNodeMenu.Show(tree, location);
            }
            return;
        }

        if (tree.GetNodeAt(location) is null)
        {
            _assetsBlankMenu.Show(tree, location);
        }
        else
        {
            _assetsNodeMenu.Show(tree, location);
        }
    }

    private void CreateHierarchyNodeFromMenu(string defaultName, string kind, string type, bool asChild)
    {
        _contextTreeView = hierarchyTreeView;
        if (_contextTreeView is null)
        {
            return;
        }

        CaptureHistorySnapshot();
        var node = CreateNode(defaultName, kind, type);
        if (!asChild || _contextTreeView.SelectedNode is null)
        {
            _contextTreeView.Nodes.Add(node);
        }
        else
        {
            _contextTreeView.SelectedNode.Nodes.Add(node);
            _contextTreeView.SelectedNode.Expand();
        }

        _contextTreeView.SelectedNode = node;
        node.BeginEdit();
        SaveTreeData();
        RefreshSceneFromHierarchyTree();
        RefreshCameraTargetCandidates();
    }

    private void DuplicateHierarchyNode()
    {
        _contextTreeView = hierarchyTreeView;
        if (_contextTreeView?.SelectedNode is null)
        {
            return;
        }

        CaptureHistorySnapshot();

        var source = _contextTreeView.SelectedNode;
        var clone = CloneNode(source);
        if (source.Parent is null)
        {
            _contextTreeView.Nodes.Add(clone);
        }
        else
        {
            source.Parent.Nodes.Add(clone);
            source.Parent.Expand();
        }

        _contextTreeView.SelectedNode = clone;
        SaveTreeData();
        RefreshSceneFromHierarchyTree();
        RefreshCameraTargetCandidates();
    }

    private static TreeNode CloneNode(TreeNode source)
    {
        var meta = GetMeta(source);
        var clonedMeta = meta with { RawText = $"{meta.RawText}_copy" };
        var clone = new TreeNode { Tag = clonedMeta };
        UpdateNodeText(clone);
        foreach (TreeNode child in source.Nodes)
        {
            clone.Nodes.Add(CloneNode(child));
        }

        return clone;
    }

    private TreeNode CreateNode(string text, string kind, string type = "")
    {
        var node = new TreeNode
        {
            Tag = new NodeMeta(text, kind, type)
        };
        UpdateNodeText(node);
        return node;
    }

    private static NodeMeta GetMeta(TreeNode node)
    {
        return node.Tag as NodeMeta ?? new NodeMeta(node.Text, NodeKindItem, NodeTypeObject);
    }

    private static bool IsCameraNode(NodeMeta meta)
    {
        return string.Equals(meta.Type, NodeTypeCamera, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetNodeTypeDisplayName(string type)
    {
        return type switch
        {
            NodeTypeCamera => "摄像机",
            NodeTypeUi => "UI",
            NodeTypeEmpty => "空节点",
            NodeTypeObject => "对象",
            _ => string.IsNullOrWhiteSpace(type) ? "对象" : type
        };
    }

    private static void UpdateNodeText(TreeNode node)
    {
        var meta = GetMeta(node);
        var icon = meta.Kind == NodeKindFolder ? (node.IsExpanded ? "📂" : "📁") : "📌";
        node.Text = $"{icon} {meta.RawText}";
        node.Tag = meta;
    }

    private void AddChildNode()
    {
        if (_contextTreeView is null)
        {
            return;
        }

        CaptureHistorySnapshot();

        var parent = _contextTreeView.SelectedNode;
        if (parent is null)
        {
            var root = CreateNode(_localization.Format("tree.newNode", _newNodeCounter++), NodeKindFolder);
            _contextTreeView.Nodes.Add(root);
            _contextTreeView.SelectedNode = root;
            root.BeginEdit();
            SaveTreeData();
            return;
        }

        var child = CreateNode(_localization.Format("tree.newNode", _newNodeCounter++), NodeKindItem, NodeTypeObject);
        parent.Nodes.Add(child);
        parent.Expand();
        _contextTreeView.SelectedNode = child;
        child.BeginEdit();
        SaveTreeData();
        RefreshSceneFromHierarchyTree();
    }

    private void AddSiblingNode()
    {
        if (_contextTreeView is null)
        {
            return;
        }

        CaptureHistorySnapshot();

        var selected = _contextTreeView.SelectedNode;
        if (selected is null)
        {
            AddChildNode();
            return;
        }

        var sibling = CreateNode(_localization.Format("tree.newNode", _newNodeCounter++), GetMeta(selected).Kind);
        if (selected.Parent is null)
        {
            _contextTreeView.Nodes.Add(sibling);
        }
        else
        {
            selected.Parent.Nodes.Add(sibling);
            selected.Parent.Expand();
        }

        _contextTreeView.SelectedNode = sibling;
        sibling.BeginEdit();
        SaveTreeData();
        RefreshSceneFromHierarchyTree();
    }

    private void AddAssetFolderNode()
    {
        if (_contextTreeView is null)
        {
            return;
        }

        CaptureHistorySnapshot();

        var parent = _contextTreeView.SelectedNode;
        var folder = CreateNode(_localization.T("tree.newAssetFolder"), NodeKindFolder);
        if (parent is null)
        {
            _contextTreeView.Nodes.Add(folder);
        }
        else
        {
            parent.Nodes.Add(folder);
            parent.Expand();
        }

        _contextTreeView.SelectedNode = folder;
        folder.BeginEdit();
        SaveTreeData();
    }

    private void AddAssetItemNode()
    {
        if (_contextTreeView is null)
        {
            return;
        }

        CaptureHistorySnapshot();

        var parent = _contextTreeView.SelectedNode;
        var node = CreateNode(_localization.T("tree.newAssetNode"), NodeKindItem);
        if (parent is null)
        {
            _contextTreeView.Nodes.Add(node);
        }
        else
        {
            parent.Nodes.Add(node);
            parent.Expand();
        }

        _contextTreeView.SelectedNode = node;
        node.BeginEdit();
        SaveTreeData();
    }

    private void AddAssetNodeByType(string defaultName, string kind)
    {
        _contextTreeView = assetsTreeView;
        if (_contextTreeView is null || _currentProject is null)
        {
            return;
        }

        CaptureHistorySnapshot();

        var parent = _contextTreeView.SelectedNode;
        var node = CreateNode(defaultName, kind);
        var targetDirectory = ResolveAssetTargetDirectory(parent);
        if (kind == NodeKindFolder)
        {
            Directory.CreateDirectory(Path.Combine(targetDirectory, defaultName));
        }
        else if (defaultName.Contains('.', StringComparison.Ordinal))
        {
            var targetFile = EnsureUniqueFilePath(targetDirectory, defaultName);
            File.WriteAllText(targetFile, string.Empty);
            node.Tag = GetMeta(node) with { RawText = Path.GetFileName(targetFile) };
            UpdateNodeText(node);
        }

        if (parent is null)
        {
            _contextTreeView.Nodes.Add(node);
        }
        else
        {
            parent.Nodes.Add(node);
            parent.Expand();
        }

        _contextTreeView.SelectedNode = node;
        node.BeginEdit();
        SaveTreeData();
    }

    private void ImportAssets()
    {
        if (_currentProject is null)
        {
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = _localization.T("dialog.asset.import.title"),
            Filter = "All Files (*.*)|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.FileNames.Length == 0)
        {
            return;
        }

        _contextTreeView = assetsTreeView;
        CaptureHistorySnapshot();
        var selected = _contextTreeView.SelectedNode;
        var targetDirectory = ResolveAssetTargetDirectory(selected);
        var insertParent = selected is not null && GetMeta(selected).Kind == NodeKindFolder ? selected : selected?.Parent;

        foreach (var file in dialog.FileNames)
        {
            var fileName = Path.GetFileName(file);
            var copiedPath = EnsureUniqueFilePath(targetDirectory, fileName);
            File.Copy(file, copiedPath, false);

            var node = CreateNode(Path.GetFileName(copiedPath), NodeKindItem);
            if (insertParent is null)
            {
                _contextTreeView.Nodes.Add(node);
            }
            else
            {
                insertParent.Nodes.Add(node);
                insertParent.Expand();
            }
        }

        SaveTreeData();
        _logger.Info(_localization.Format("log.asset.imported", dialog.FileNames.Length));
    }

    private void ExportSelectedAsset()
    {
        if (_currentProject is null || assetsTreeView.SelectedNode is null)
        {
            return;
        }

        using var dialog = new FolderBrowserDialog
        {
            Description = _localization.T("dialog.asset.export.description"),
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath))
        {
            return;
        }

        var selected = assetsTreeView.SelectedNode;
        var sourcePath = ResolvePhysicalPathForAssetNode(selected);
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            _logger.Warning(_localization.T("log.asset.notFound"));
            return;
        }

        if (Directory.Exists(sourcePath))
        {
            var targetFolder = Path.Combine(dialog.SelectedPath, Path.GetFileName(sourcePath));
            CopyDirectory(sourcePath, targetFolder);
            _logger.Info(_localization.Format("log.asset.exported", Path.GetFileName(sourcePath)));
            return;
        }

        if (File.Exists(sourcePath))
        {
            var targetFile = Path.Combine(dialog.SelectedPath, Path.GetFileName(sourcePath));
            File.Copy(sourcePath, targetFile, true);
            _logger.Info(_localization.Format("log.asset.exported", Path.GetFileName(sourcePath)));
            return;
        }

        _logger.Warning(_localization.T("log.asset.notFound"));
    }

    private void OpenAssetInExplorer()
    {
        if (_currentProject is null)
        {
            return;
        }

        var assetsPath = Path.Combine(_currentProject.RootDirectory, _currentProject.Project.Paths.Assets);
        Directory.CreateDirectory(assetsPath);

        var selected = assetsTreeView.SelectedNode;
        if (selected is null)
        {
            System.Diagnostics.Process.Start("explorer.exe", assetsPath);
            return;
        }

        var candidate = ResolvePhysicalPathForAssetNode(selected);
        if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
        {
            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{candidate}\"");
            return;
        }

        if (!string.IsNullOrWhiteSpace(candidate) && Directory.Exists(candidate))
        {
            System.Diagnostics.Process.Start("explorer.exe", candidate);
            return;
        }

        System.Diagnostics.Process.Start("explorer.exe", assetsPath);
    }

    private string GetAssetsRootPath()
    {
        if (_currentProject is null)
        {
            return string.Empty;
        }

        return Path.Combine(_currentProject.RootDirectory, _currentProject.Project.Paths.Assets);
    }

    private string ResolveAssetTargetDirectory(TreeNode? selectedNode)
    {
        var assetsRoot = GetAssetsRootPath();
        Directory.CreateDirectory(assetsRoot);
        if (selectedNode is null)
        {
            return assetsRoot;
        }

        var meta = GetMeta(selectedNode);
        if (meta.Kind == NodeKindFolder)
        {
            var dir = ResolvePhysicalPathForAssetNode(selectedNode);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        if (selectedNode.Parent is null)
        {
            return assetsRoot;
        }

        var parentDir = ResolvePhysicalPathForAssetNode(selectedNode.Parent);
        if (string.IsNullOrWhiteSpace(parentDir))
        {
            return assetsRoot;
        }

        Directory.CreateDirectory(parentDir);
        return parentDir;
    }

    private string ResolvePhysicalPathForAssetNode(TreeNode node)
    {
        var assetsRoot = GetAssetsRootPath();
        if (string.IsNullOrWhiteSpace(assetsRoot))
        {
            return string.Empty;
        }

        var segments = new List<string>();
        TreeNode? current = node;
        while (current is not null)
        {
            segments.Add(GetMeta(current).RawText);
            current = current.Parent;
        }

        segments.Reverse();
        if (segments.Count > 0 && segments[0].Equals("assets", StringComparison.OrdinalIgnoreCase))
        {
            segments.RemoveAt(0);
        }

        var path = assetsRoot;
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

            path = Path.Combine(path, segment);
        }

        return path;
    }

    private static string EnsureUniqueFilePath(string directory, string fileName)
    {
        Directory.CreateDirectory(directory);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        var candidate = Path.Combine(directory, fileName);
        var index = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{name}_{index}{ext}");
            index++;
        }

        return candidate;
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var targetChild = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, targetChild);
        }
    }

    private void ViewNode()
    {
        if (_contextTreeView?.SelectedNode is null)
        {
            inspectorTextBox.Text = _localization.T("tree.noProject.detail");
            return;
        }

        PopulateInspectorForNode(_contextTreeView.SelectedNode);
    }

    private static string BuildNodePath(TreeNode node)
    {
        var parts = new List<string>();
        TreeNode? current = node;
        while (current is not null)
        {
            parts.Add(GetMeta(current).RawText);
            current = current.Parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }

    private void RenameNode()
    {
        var node = _contextTreeView?.SelectedNode;
        if (node is null)
        {
            return;
        }

        _contextTreeView!.LabelEdit = true;
        node.BeginEdit();
        _contextTreeView.AfterLabelEdit -= TreeView_AfterLabelEdit;
        _contextTreeView.AfterLabelEdit += TreeView_AfterLabelEdit;
    }

    private void TreeView_AfterLabelEdit(object? sender, NodeLabelEditEventArgs e)
    {
        if (e.Node is null)
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(e.Label) ? GetMeta(e.Node).RawText : e.Label.Trim();
        name = StripNodePrefix(name);

        e.Node.Tag = GetMeta(e.Node) with { RawText = name };
        UpdateNodeText(e.Node);
        e.CancelEdit = true;
        PopulateInspectorForNode(e.Node);
        SaveTreeData();
        RefreshSceneFromHierarchyTree();
        if (sender is TreeView tree)
        {
            tree.LabelEdit = false;
        }
    }

    private void DeleteNode()
    {
        var node = _contextTreeView?.SelectedNode;
        if (node is null)
        {
            return;
        }

        CaptureHistorySnapshot();

        node.Remove();
        inspectorTextBox.Text = _localization.T("status.noProject");
        previewBox.Image?.Dispose();
        previewBox.Image = null;
        SaveTreeData();
        RefreshSceneFromHierarchyTree();
        RefreshCameraTargetCandidates();
    }

    private void InitializeInspectorEditor()
    {
        inspectorTextBox.Visible = false;

        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Padding = new Padding(8)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 11; i++)
        {
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        }
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _inspectorNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorTypeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorXLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorYLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorRotationLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorScaleLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorCameraModeLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorCameraTargetLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorCameraSmoothLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorCameraZoomLabel.TextAlign = ContentAlignment.MiddleLeft;
        _inspectorPathLabel.TextAlign = ContentAlignment.MiddleLeft;

        _inspectorNameBox.Dock = DockStyle.Fill;
        _inspectorTypeCombo.Dock = DockStyle.Fill;
        _inspectorTypeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _inspectorTypeCombo.Items.Add("folder");
        _inspectorTypeCombo.Items.Add("item");
        _inspectorTypeCombo.SelectedIndex = 1;
        _inspectorXBox.Dock = DockStyle.Fill;
        _inspectorXBox.Minimum = -100000;
        _inspectorXBox.Maximum = 100000;
        _inspectorYBox.Dock = DockStyle.Fill;
        _inspectorYBox.Minimum = -100000;
        _inspectorYBox.Maximum = 100000;
        _inspectorRotationBox.Dock = DockStyle.Fill;
        _inspectorRotationBox.Minimum = -3600;
        _inspectorRotationBox.Maximum = 3600;
        _inspectorRotationBox.DecimalPlaces = 2;
        _inspectorRotationBox.Increment = 0.5m;
        _inspectorScaleBox.Dock = DockStyle.Fill;
        _inspectorScaleBox.Minimum = 1;
        _inspectorScaleBox.Maximum = 1000;
        _inspectorScaleBox.DecimalPlaces = 2;
        _inspectorScaleBox.Increment = 0.05m;
        _inspectorScaleBox.Value = 1;
        _inspectorCameraModeCombo.Dock = DockStyle.Fill;
        _inspectorCameraModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _inspectorCameraModeCombo.Items.AddRange(["fixed", "followPlayer", "followTarget"]);
        _inspectorCameraModeCombo.SelectedIndex = 0;
        _inspectorCameraTargetBox.Dock = DockStyle.Fill;
        _inspectorCameraTargetBox.DropDownStyle = ComboBoxStyle.DropDown;
        _inspectorCameraSmoothBox.Dock = DockStyle.Fill;
        _inspectorCameraSmoothBox.Minimum = 0;
        _inspectorCameraSmoothBox.Maximum = 1;
        _inspectorCameraSmoothBox.DecimalPlaces = 2;
        _inspectorCameraSmoothBox.Increment = 0.05m;
        _inspectorCameraZoomBox.Dock = DockStyle.Fill;
        _inspectorCameraZoomBox.Minimum = 0.25m;
        _inspectorCameraZoomBox.Maximum = 4m;
        _inspectorCameraZoomBox.DecimalPlaces = 2;
        _inspectorCameraZoomBox.Increment = 0.05m;
        _inspectorCameraZoomBox.Value = 1m;
        _inspectorPathBox.Dock = DockStyle.Fill;
        _inspectorPathBox.ReadOnly = true;

        panel.Controls.Add(_inspectorNameLabel, 0, 0);
        panel.Controls.Add(_inspectorNameBox, 1, 0);
        panel.Controls.Add(_inspectorTypeLabel, 0, 1);
        panel.Controls.Add(_inspectorTypeCombo, 1, 1);
        panel.Controls.Add(_inspectorXLabel, 0, 2);
        panel.Controls.Add(_inspectorXBox, 1, 2);
        panel.Controls.Add(_inspectorYLabel, 0, 3);
        panel.Controls.Add(_inspectorYBox, 1, 3);
        panel.Controls.Add(_inspectorRotationLabel, 0, 4);
        panel.Controls.Add(_inspectorRotationBox, 1, 4);
        panel.Controls.Add(_inspectorScaleLabel, 0, 5);
        panel.Controls.Add(_inspectorScaleBox, 1, 5);
        panel.Controls.Add(_inspectorCameraModeLabel, 0, 6);
        panel.Controls.Add(_inspectorCameraModeCombo, 1, 6);
        panel.Controls.Add(_inspectorCameraTargetLabel, 0, 7);
        panel.Controls.Add(_inspectorCameraTargetBox, 1, 7);
        panel.Controls.Add(_inspectorCameraSmoothLabel, 0, 8);
        panel.Controls.Add(_inspectorCameraSmoothBox, 1, 8);
        panel.Controls.Add(_inspectorCameraZoomLabel, 0, 9);
        panel.Controls.Add(_inspectorCameraZoomBox, 1, 9);
        panel.Controls.Add(_inspectorPathLabel, 0, 10);
        panel.Controls.Add(_inspectorPathBox, 1, 10);

        inspectorGroupBox.Controls.Add(panel);

        _inspectorNameBox.TextChanged += InspectorEditorChanged;
        _inspectorTypeCombo.SelectedIndexChanged += InspectorEditorChanged;
        _inspectorXBox.ValueChanged += InspectorEditorChanged;
        _inspectorYBox.ValueChanged += InspectorEditorChanged;
        _inspectorRotationBox.ValueChanged += InspectorEditorChanged;
        _inspectorScaleBox.ValueChanged += InspectorEditorChanged;
        _inspectorCameraModeCombo.SelectedIndexChanged += InspectorEditorChanged;
        _inspectorCameraTargetBox.TextChanged += InspectorEditorChanged;
        _inspectorCameraTargetBox.SelectedIndexChanged += InspectorEditorChanged;
        _inspectorCameraSmoothBox.ValueChanged += InspectorEditorChanged;
        _inspectorCameraZoomBox.ValueChanged += InspectorEditorChanged;
        UpdateInspectorCoordinateState();
        RefreshCameraTargetCandidates();
    }

    private void PopulateInspectorForNode(TreeNode node)
    {
        _inspectorNode = node;
        var meta = GetMeta(node);
        _suppressInspectorEvents = true;
        _inspectorNameBox.Text = meta.RawText;
        _inspectorTypeCombo.SelectedItem = meta.Kind == NodeKindFolder
            ? _localization.T("tree.nodeType.folder")
            : _localization.T("tree.nodeType.item");
        _inspectorXBox.Value = ClampToNumeric(meta.X);
        _inspectorYBox.Value = ClampToNumeric(meta.Y);
        _inspectorRotationBox.Value = ClampToNumeric(meta.Rotation);
        _inspectorScaleBox.Value = ClampToScaleNumeric(meta.Scale);
        _inspectorCameraModeCombo.SelectedItem = string.IsNullOrWhiteSpace(meta.CameraMode) ? "fixed" : meta.CameraMode;
        RefreshCameraTargetCandidates(meta.CameraTarget);
        _inspectorCameraTargetBox.Text = meta.CameraTarget ?? string.Empty;
        _inspectorCameraSmoothBox.Value = ClampToCameraSmoothNumeric(meta.CameraSmooth);
        _inspectorCameraZoomBox.Value = ClampToCameraZoomNumeric(meta.CameraZoom);
        _inspectorPathBox.Text = BuildNodePath(node);
        UpdateInspectorCoordinateState();
        RefreshCameraTargetCandidates(meta.CameraTarget);
        inspectorTextBox.Text =
            $"{_localization.T("tree.details.name")}: {meta.RawText}{Environment.NewLine}" +
            $"{_localization.T("tree.details.type")}: {(meta.Kind == NodeKindFolder ? _localization.T("tree.nodeType.folder") : _localization.T("tree.nodeType.item"))}{Environment.NewLine}" +
            $"节点类别: {GetNodeTypeDisplayName(meta.Type)}{Environment.NewLine}" +
            $"旋转: {meta.Rotation?.ToString("0.##") ?? "0"}{Environment.NewLine}" +
            $"缩放: {meta.Scale?.ToString("0.##") ?? "1"}{Environment.NewLine}" +
            $"相机模式: {(IsCameraNode(meta) ? (_inspectorCameraModeCombo.SelectedItem?.ToString() ?? "fixed") : "不适用")}{Environment.NewLine}" +
            $"相机缩放: {(IsCameraNode(meta) ? meta.CameraZoom?.ToString("0.##") ?? "1" : "不适用")}{Environment.NewLine}" +
            $"{_localization.T("tree.details.path")}: {BuildNodePath(node)}";
        _suppressInspectorEvents = false;
    }

    private static decimal ClampToNumeric(float? value)
    {
        if (!value.HasValue)
        {
            return 0;
        }

        if (value.Value > 100000)
        {
            return 100000;
        }

        if (value.Value < -100000)
        {
            return -100000;
        }

        return (decimal)value.Value;
    }

    private static decimal ClampToScaleNumeric(float? value)
    {
        var v = value ?? 1f;
        if (v < 0.01f) v = 0.01f;
        if (v > 1000f) v = 1000f;
        return (decimal)v;
    }

    private static decimal ClampToCameraSmoothNumeric(float? value)
    {
        var v = value ?? 0f;
        if (v < 0f) v = 0f;
        if (v > 1f) v = 1f;
        return (decimal)v;
    }

    private static decimal ClampToCameraZoomNumeric(float? value)
    {
        var v = value ?? 1f;
        if (v < 0.25f) v = 0.25f;
        if (v > 4f) v = 4f;
        return (decimal)v;
    }

    private void InspectorEditorChanged(object? sender, EventArgs e)
    {
        if (_suppressInspectorEvents || _inspectorNode is null)
        {
            return;
        }

        CaptureHistorySnapshot();

        var kind = _inspectorTypeCombo.SelectedItem?.ToString() == _localization.T("tree.nodeType.folder")
            ? NodeKindFolder
            : NodeKindItem;
        UpdateInspectorCoordinateState();
        var meta = GetMeta(_inspectorNode);
        var x = kind == NodeKindFolder ? (float?)null : (float)_inspectorXBox.Value;
        var y = kind == NodeKindFolder ? (float?)null : (float)_inspectorYBox.Value;
        var rotation = kind == NodeKindFolder ? (float?)null : (float)_inspectorRotationBox.Value;
        var scale = kind == NodeKindFolder ? (float?)null : (float)_inspectorScaleBox.Value;
        var isCamera = IsCameraNode(meta);
        var cameraTarget = isCamera ? (_inspectorCameraTargetBox.Text.Trim()) : string.Empty;
        _inspectorNode.Tag = meta with
        {
            RawText = _inspectorNameBox.Text.Trim(),
            Kind = kind,
            X = x,
            Y = y,
            Rotation = rotation,
            Scale = scale,
            CameraMode = isCamera ? _inspectorCameraModeCombo.SelectedItem?.ToString() ?? "fixed" : string.Empty,
            CameraTarget = cameraTarget,
            CameraSmooth = isCamera ? (float)_inspectorCameraSmoothBox.Value : null,
            CameraZoom = isCamera ? (float)_inspectorCameraZoomBox.Value : null
        };
        UpdateNodeText(_inspectorNode);
        _inspectorPathBox.Text = BuildNodePath(_inspectorNode);
        SaveTreeData();
        RefreshSceneFromHierarchyTree();
        PopulateInspectorForNode(_inspectorNode);
    }

    private void UpdateInspectorCoordinateState()
    {
        var isFolder = _inspectorTypeCombo.SelectedItem?.ToString() == _localization.T("tree.nodeType.folder");
        _inspectorXBox.Enabled = !isFolder;
        _inspectorYBox.Enabled = !isFolder;
        _inspectorRotationBox.Enabled = !isFolder;
        _inspectorScaleBox.Enabled = !isFolder;
        var meta = _inspectorNode is null ? null : GetMeta(_inspectorNode);
        var isCamera = meta is not null && IsCameraNode(meta) && !isFolder;
        _inspectorCameraModeLabel.Visible = isCamera;
        _inspectorCameraModeCombo.Visible = isCamera;
        _inspectorCameraTargetLabel.Visible = isCamera;
        _inspectorCameraTargetBox.Visible = isCamera;
        _inspectorCameraSmoothLabel.Visible = isCamera;
        _inspectorCameraSmoothBox.Visible = isCamera;
        _inspectorCameraZoomLabel.Visible = isCamera;
        _inspectorCameraZoomBox.Visible = isCamera;
        _inspectorCameraTargetBox.Enabled = isCamera && string.Equals(_inspectorCameraModeCombo.SelectedItem?.ToString(), "followTarget", StringComparison.OrdinalIgnoreCase);
        _inspectorCameraSmoothBox.Enabled = isCamera;
        _inspectorCameraZoomBox.Enabled = isCamera;
        if (isFolder)
        {
            if (_inspectorXBox.Value != 0) _inspectorXBox.Value = 0;
            if (_inspectorYBox.Value != 0) _inspectorYBox.Value = 0;
            if (_inspectorRotationBox.Value != 0) _inspectorRotationBox.Value = 0;
            if (_inspectorScaleBox.Value != 1) _inspectorScaleBox.Value = 1;
        }
    }

    private void RefreshCameraTargetCandidates(string? selectedTarget = null)
    {
        var previous = selectedTarget ?? _inspectorCameraTargetBox.Text;
        var candidates = new List<string> { "player" };

        if (_currentProject is not null)
        {
            foreach (var name in EnumerateCameraTargetCandidates(hierarchyTreeView.Nodes))
            {
                if (!candidates.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    candidates.Add(name);
                }
            }
        }

        _inspectorCameraTargetBox.BeginUpdate();
        _inspectorCameraTargetBox.Items.Clear();
        _inspectorCameraTargetBox.Items.AddRange(candidates.Cast<object>().ToArray());
        if (!string.IsNullOrWhiteSpace(previous))
        {
            _inspectorCameraTargetBox.Text = previous;
        }
        _inspectorCameraTargetBox.EndUpdate();
    }

    private static IEnumerable<string> EnumerateCameraTargetCandidates(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            var meta = GetMeta(node);
            if (string.Equals(meta.Kind, NodeKindItem, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(meta.Type, NodeTypeCamera, StringComparison.OrdinalIgnoreCase))
            {
                yield return meta.RawText;
            }

            foreach (var child in EnumerateCameraTargetCandidates(node.Nodes))
            {
                yield return child;
            }
        }
    }

    private void UpdatePreviewForNode(TreeView? sourceTree, TreeNode node)
    {
        previewBox.Image?.Dispose();
        previewBox.Image = null;

        if (_currentProject is null || sourceTree != assetsTreeView)
        {
            return;
        }

        var fileName = GetMeta(node).RawText;
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif"))
        {
            return;
        }

        try
        {
            var assetsRoot = Path.Combine(_currentProject.RootDirectory, _currentProject.Project.Paths.Assets);
            if (!Directory.Exists(assetsRoot))
            {
                return;
            }

            var path = Directory.GetFiles(assetsRoot, fileName, SearchOption.AllDirectories).FirstOrDefault();
            if (path is null)
            {
                return;
            }

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            previewBox.Image = Image.FromStream(fs);
        }
        catch
        {
            previewBox.Image = null;
        }
    }

    private void InitializeSceneTools()
    {
        _toolToggleGridButton.Click += (_, _) =>
        {
            sceneCanvasPanel.ShowGrid = !sceneCanvasPanel.ShowGrid;
            _toolToggleGridButton.Checked = sceneCanvasPanel.ShowGrid;
        };
        _toolUndoSceneButton.Click += (_, _) => UndoSceneChange();
        _toolRedoSceneButton.Click += (_, _) => RedoSceneChange();
        _toolSelectSceneButton.Click += SceneToolCheckButton_Click;
        _toolMoveSceneButton.Click += SceneToolCheckButton_Click;
        _toolRotateSceneButton.Click += SceneToolCheckButton_Click;
        _toolScaleSceneButton.Click += SceneToolCheckButton_Click;
        _toolRectSceneButton.Click += SceneToolCheckButton_Click;
        _toolAlignSceneButton.Click += (_, _) =>
        {
            ToggleAlignPopup();
            _logger.Info(_localization.T("log.alignTriggered"));
        };
        _toolSelectSceneButton.Checked = true;
        _activeSceneTool = SceneTool.Select;

        sceneCanvasPanel.CanvasClicked += SceneCanvasPanel_CanvasClicked;
        sceneCanvasPanel.PointDragged += SceneCanvasPanel_PointDragged;
        sceneCanvasPanel.PointDragFinished += SceneCanvasPanel_PointDragFinished;
        sceneCanvasPanel.GizmoHoverChanged += SceneCanvasPanel_GizmoHoverChanged;
        sceneCanvasPanel.ActiveTool = SceneToolMode.Select;
        BuildSceneAlignPopup();
        sceneCanvasPanel.ShowGrid = true;
        _toolToggleGridButton.Checked = true;

        _alignPopupPanel.Visible = false;
        UpdateHistoryButtonState();
        UpdateSceneStatusBar();
    }

    private void SceneToolCheckButton_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripButton active)
        {
            return;
        }

        var buttons = new[] { _toolSelectSceneButton, _toolMoveSceneButton, _toolRotateSceneButton, _toolScaleSceneButton, _toolRectSceneButton };
        foreach (var button in buttons)
        {
            button.Checked = ReferenceEquals(button, active);
        }

        _activeSceneTool = active == _toolMoveSceneButton ? SceneTool.Move
            : active == _toolRotateSceneButton ? SceneTool.Rotate
            : active == _toolScaleSceneButton ? SceneTool.Scale
            : active == _toolRectSceneButton ? SceneTool.Rect
            : SceneTool.Select;

        _sceneDragHistoryCaptured = false;
        _sceneTransformDragInitialized = false;
        _sceneHoverTarget = SceneGizmoHitTarget.None;
        _alignPopupPanel.Visible = false;
        sceneCanvasPanel.ActiveTool = _activeSceneTool switch
        {
            SceneTool.Move => SceneToolMode.Move,
            SceneTool.Rotate => SceneToolMode.Rotate,
            SceneTool.Scale => SceneToolMode.Scale,
            SceneTool.Rect => SceneToolMode.Rect,
            _ => SceneToolMode.Select
        };
        UpdateSceneStatusBar(hierarchyTreeView.SelectedNode);
    }

    private void SceneCanvasPanel_GizmoHoverChanged(object? sender, SceneGizmoHoverChangedEventArgs e)
    {
        _sceneHoverTarget = e.Target;
        UpdateSceneStatusBar(hierarchyTreeView.SelectedNode);
    }

    private void SceneCanvasPanel_CanvasClicked(object? sender, ScenePointEventArgs e)
    {
        if (_currentProject is null || e.Button != MouseButtons.Left)
        {
            return;
        }

        var hit = FindScenePointHitIndex(e.X, e.Y);
        if (hit >= 0)
        {
            _selectedScenePointIndex = hit;
            sceneCanvasPanel.SelectedIndex = hit;
            if (hit < _scenePointNodes.Count)
            {
                hierarchyTreeView.SelectedNode = _scenePointNodes[hit];
                hierarchyTreeView.Focus();
            }
            return;
        }

        if (_activeSceneTool == SceneTool.Rect)
        {
            CaptureHistorySnapshot();
            var anchor = hierarchyTreeView.SelectedNode;
            var node = CreateNode(_localization.Format("tree.newNode", _newNodeCounter++), NodeKindItem, NodeTypeObject);
            node.Tag = new NodeMeta(GetMeta(node).RawText, NodeKindItem, NodeTypeObject, e.X, e.Y, 0f, 1f);
            UpdateNodeText(node);
            if (anchor is null)
            {
                hierarchyTreeView.Nodes.Add(node);
            }
            else
            {
                anchor.Nodes.Add(node);
                anchor.Expand();
            }

            hierarchyTreeView.SelectedNode = node;
            SaveTreeData();
            RefreshSceneFromHierarchyTree();
            PopulateInspectorForNode(node);
        }
    }

    private void SceneCanvasPanel_PointDragged(object? sender, ScenePointDragEventArgs e)
    {
        if (_activeSceneTool is not (SceneTool.Move or SceneTool.Rotate or SceneTool.Scale))
        {
            return;
        }

        if (e.Index < 0 || e.Index >= _scenePointNodes.Count)
        {
            return;
        }

        if (!_sceneDragHistoryCaptured)
        {
            CaptureHistorySnapshot();
            _sceneDragHistoryCaptured = true;
            _sceneTransformDragInitialized = false;
        }

        var node = _scenePointNodes[e.Index];
        var meta = GetMeta(node);
        if (!_sceneTransformDragInitialized)
        {
            InitializeSceneTransformDrag(meta, e.X, e.Y);
            _sceneTransformDragInitialized = true;
        }
        var shiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
        var ctrlPressed = (ModifierKeys & Keys.Control) == Keys.Control;
        node.Tag = ApplySceneDrag(meta, e.X, e.Y, shiftPressed, ctrlPressed, e.Target);
        _selectedScenePointIndex = e.Index;
        sceneCanvasPanel.SelectedIndex = e.Index;
        RefreshSceneFromHierarchyTree();
        UpdateSceneStatusBar(node);
    }

    private void SceneCanvasPanel_PointDragFinished(object? sender, ScenePointDragEventArgs e)
    {
        if (_activeSceneTool is not (SceneTool.Move or SceneTool.Rotate or SceneTool.Scale))
        {
            return;
        }

        if (e.Index < 0 || e.Index >= _scenePointNodes.Count)
        {
            return;
        }

        var node = _scenePointNodes[e.Index];
        var meta = GetMeta(node);
        var shiftPressed = (ModifierKeys & Keys.Shift) == Keys.Shift;
        var ctrlPressed = (ModifierKeys & Keys.Control) == Keys.Control;
        node.Tag = ApplySceneDrag(meta, e.X, e.Y, shiftPressed, ctrlPressed, e.Target);
        hierarchyTreeView.SelectedNode = node;
        SaveTreeData();
        PopulateInspectorForNode(node);
        _sceneDragHistoryCaptured = false;
        _sceneTransformDragInitialized = false;
        UpdateSceneStatusBar(node);
    }

    private void InitializeSceneTransformDrag(NodeMeta meta, float mouseX, float mouseY)
    {
        _sceneDragStartX = meta.X ?? 0f;
        _sceneDragStartY = meta.Y ?? 0f;
        var cx = meta.X ?? 0f;
        var cy = meta.Y ?? 0f;
        var dx = mouseX - cx;
        var dy = mouseY - cy;
        _sceneDragStartMouseAngle = MathF.Atan2(dy, dx) * 180f / MathF.PI;
        _sceneDragStartMouseDistance = MathF.Max(1f, MathF.Sqrt(dx * dx + dy * dy));
        _sceneDragStartRotation = meta.Rotation ?? 0f;
        _sceneDragStartScale = MathF.Max(0.1f, meta.Scale ?? 1f);
    }

    private NodeMeta ApplySceneDrag(NodeMeta meta, float mouseX, float mouseY, bool shiftPressed, bool ctrlPressed, SceneGizmoHitTarget target)
    {
        return _activeSceneTool switch
        {
            SceneTool.Move => ApplyMoveDrag(meta, mouseX, mouseY, shiftPressed, ctrlPressed, target),
            SceneTool.Rotate => meta with { Rotation = CalculateRotationFromDrag(meta, mouseX, mouseY, shiftPressed, ctrlPressed) },
            SceneTool.Scale => meta with { Scale = CalculateScaleFromDrag(meta, mouseX, mouseY, shiftPressed, ctrlPressed) },
            _ => meta
        };
    }

    private NodeMeta ApplyMoveDrag(NodeMeta meta, float mouseX, float mouseY, bool shiftPressed, bool ctrlPressed, SceneGizmoHitTarget target)
    {
        if (ctrlPressed)
        {
            mouseX = Quantize(mouseX, 5f);
            mouseY = Quantize(mouseY, 5f);
        }

        if (target == SceneGizmoHitTarget.MoveX)
        {
            return meta with { X = mouseX, Y = _sceneDragStartY };
        }

        if (target == SceneGizmoHitTarget.MoveY)
        {
            return meta with { X = _sceneDragStartX, Y = mouseY };
        }

        if (target == SceneGizmoHitTarget.MoveCenter)
        {
            return meta with { X = mouseX, Y = mouseY };
        }

        if (!shiftPressed)
        {
            return meta with { X = mouseX, Y = mouseY };
        }

        var dx = mouseX - _sceneDragStartX;
        var dy = mouseY - _sceneDragStartY;
        if (MathF.Abs(dx) >= MathF.Abs(dy))
        {
            return meta with { X = mouseX, Y = _sceneDragStartY };
        }

        return meta with { X = _sceneDragStartX, Y = mouseY };
    }

    private float CalculateRotationFromDrag(NodeMeta meta, float mouseX, float mouseY, bool shiftPressed, bool ctrlPressed)
    {
        var cx = meta.X ?? 0f;
        var cy = meta.Y ?? 0f;
        var dx = mouseX - cx;
        var dy = mouseY - cy;
        var currentAngle = MathF.Atan2(dy, dx) * 180f / MathF.PI;
        var delta = currentAngle - _sceneDragStartMouseAngle;
        var result = _sceneDragStartRotation + delta;
        while (result > 180f) result -= 360f;
        while (result < -180f) result += 360f;
        if (ctrlPressed)
        {
            result = MathF.Round(result);
        }
        if (shiftPressed)
        {
            result = MathF.Round(result / 15f) * 15f;
        }
        return MathF.Round(result, 2);
    }

    private float CalculateScaleFromDrag(NodeMeta meta, float mouseX, float mouseY, bool shiftPressed, bool ctrlPressed)
    {
        var cx = meta.X ?? 0f;
        var cy = meta.Y ?? 0f;
        var dx = mouseX - cx;
        var dy = mouseY - cy;
        var currentDistance = MathF.Max(1f, MathF.Sqrt(dx * dx + dy * dy));
        var factor = currentDistance / MathF.Max(1f, _sceneDragStartMouseDistance);
        var scale = _sceneDragStartScale * factor;
        scale = Math.Clamp(scale, 0.1f, 100f);
        if (ctrlPressed)
        {
            scale = MathF.Round(scale * 20f) / 20f;
        }
        if (shiftPressed)
        {
            scale = MathF.Round(scale * 10f) / 10f;
        }
        return MathF.Round(scale, 2);
    }

    private static float Quantize(float value, float step)
    {
        if (step <= 0.0001f)
        {
            return value;
        }

        return MathF.Round(value / step) * step;
    }

    private void UpdateSceneStatusBar(TreeNode? node = null)
    {
        if (_currentProject is null)
        {
            stateStatusLabel.Text = _localization.T("status.noProject");
            return;
        }

        node ??= hierarchyTreeView.SelectedNode;
        var toolName = _activeSceneTool switch
        {
            SceneTool.Move => _localization.T("tool.scene.move"),
            SceneTool.Rotate => _localization.T("tool.scene.rotate"),
            SceneTool.Scale => _localization.T("tool.scene.scale"),
            SceneTool.Rect => _localization.T("tool.scene.rect"),
            _ => _localization.T("tool.scene.select")
        };
        var hoverHint = GetSceneHoverHint(_sceneHoverTarget);

        if (node is null)
        {
            stateStatusLabel.Text = string.IsNullOrWhiteSpace(hoverHint)
                ? _localization.Format("status.sceneToolOnly", toolName)
                : _localization.Format("status.sceneToolOnlyHint", toolName, hoverHint);
            return;
        }

        var meta = GetMeta(node);
        if (!meta.X.HasValue || !meta.Y.HasValue)
        {
            stateStatusLabel.Text = string.IsNullOrWhiteSpace(hoverHint)
                ? _localization.Format("status.sceneToolOnly", toolName)
                : _localization.Format("status.sceneToolOnlyHint", toolName, hoverHint);
            return;
        }

        stateStatusLabel.Text = string.IsNullOrWhiteSpace(hoverHint)
            ? _localization.Format(
                "status.sceneTransform",
                toolName,
                meta.RawText,
                meta.X.Value.ToString("0.##"),
                meta.Y.Value.ToString("0.##"),
                (meta.Rotation ?? 0f).ToString("0.##"),
                (meta.Scale ?? 1f).ToString("0.##"))
            : _localization.Format(
                "status.sceneTransformHint",
                toolName,
                meta.RawText,
                meta.X.Value.ToString("0.##"),
                meta.Y.Value.ToString("0.##"),
                (meta.Rotation ?? 0f).ToString("0.##"),
                (meta.Scale ?? 1f).ToString("0.##"),
                hoverHint);
    }

    private string GetSceneHoverHint(SceneGizmoHitTarget target)
    {
        return target switch
        {
            SceneGizmoHitTarget.MoveX => _localization.T("status.hover.moveX"),
            SceneGizmoHitTarget.MoveY => _localization.T("status.hover.moveY"),
            SceneGizmoHitTarget.MoveCenter => _localization.T("status.hover.moveCenter"),
            SceneGizmoHitTarget.RotateRing => _localization.T("status.hover.rotate"),
            SceneGizmoHitTarget.ScaleHandle => _localization.T("status.hover.scale"),
            _ => string.Empty
        };
    }

    private int FindScenePointHitIndex(float x, float y)
    {
        for (var i = 0; i < _scenePoints.Count; i++)
        {
            var dx = _scenePoints[i].X - x;
            var dy = _scenePoints[i].Y - y;
            if (dx * dx + dy * dy <= 100)
            {
                return i;
            }
        }

        return -1;
    }

    private void SyncSceneSelectionFromNode(TreeNode node)
    {
        var idx = _scenePointNodes.IndexOf(node);
        _selectedScenePointIndex = idx;
        sceneCanvasPanel.SelectedIndex = idx;
    }

    private void RefreshSceneFromHierarchyTree()
    {
        _scenePoints.Clear();
        _scenePointNodes.Clear();
        foreach (TreeNode root in hierarchyTreeView.Nodes)
        {
            CollectScenePoints(root);
        }

        sceneCanvasPanel.SetObjects(_scenePoints);
        sceneCanvasPanel.SelectedIndex = _selectedScenePointIndex;
    }

    private void CollectScenePoints(TreeNode node)
    {
        var meta = GetMeta(node);
        if (meta.X.HasValue && meta.Y.HasValue)
        {
            _scenePoints.Add(new SceneObjectPoint(
                meta.RawText,
                meta.X.Value,
                meta.Y.Value,
                meta.Rotation ?? 0f,
                meta.Scale ?? 1f));
            _scenePointNodes.Add(node);
        }

        foreach (TreeNode child in node.Nodes)
        {
            CollectScenePoints(child);
        }
    }

    private void TreeView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (sender is not TreeView tree || e.Item is not TreeNode node)
        {
            return;
        }

        _dragSourceTreeView = tree;
        tree.SelectedNode = node;
        DoDragDrop(e.Item, DragDropEffects.Move);
    }

    private static void TreeView_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(typeof(TreeNode)) == true ? DragDropEffects.Move : DragDropEffects.None;
    }

    private void TreeView_DragOver(object? sender, DragEventArgs e)
    {
        if (sender is not TreeView tree || e.Data?.GetData(typeof(TreeNode)) is not TreeNode draggedNode)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        if (_dragSourceTreeView != tree)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        var point = tree.PointToClient(new Point(e.X, e.Y));
        var target = tree.GetNodeAt(point);
        if (target is null || target == draggedNode || IsDescendant(draggedNode, target))
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        tree.SelectedNode = target;
        e.Effect = DragDropEffects.Move;
    }

    private void TreeView_DragDrop(object? sender, DragEventArgs e)
    {
        if (_currentProject is null || sender is not TreeView tree || e.Data?.GetData(typeof(TreeNode)) is not TreeNode draggedNode)
        {
            return;
        }

        if (_dragSourceTreeView != tree)
        {
            return;
        }

        CaptureHistorySnapshot();

        var point = tree.PointToClient(new Point(e.X, e.Y));
        var target = tree.GetNodeAt(point);
        if (target is null || target == draggedNode || IsDescendant(draggedNode, target))
        {
            return;
        }

        draggedNode.Remove();
        target.Nodes.Add(draggedNode);
        target.Expand();
        tree.SelectedNode = draggedNode;
        SaveTreeData();
        if (tree == hierarchyTreeView)
        {
            RefreshSceneFromHierarchyTree();
        }
    }

    private static bool IsDescendant(TreeNode parent, TreeNode node)
    {
        TreeNode? current = node.Parent;
        while (current is not null)
        {
            if (current == parent)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private void BuildSceneAlignPopup()
    {
        _alignPopupPanel.Parent = sceneCanvasPanel;
        _alignPopupPanel.Visible = false;
        _alignPopupPanel.BorderStyle = BorderStyle.FixedSingle;
        _alignPopupPanel.Location = new Point(52, 32);
        _alignPopupPanel.Size = new Size(280, 140);
        _alignPopupPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        _alignPopupAlignLabel.Parent = _alignPopupPanel;
        _alignPopupAlignLabel.Location = new Point(10, 10);
        _alignPopupAlignLabel.AutoSize = true;
        _alignPopupAlignLabel.Text = _localization.T("tool.scene.align");

        _alignPopupDistributeLabel.Parent = _alignPopupPanel;
        _alignPopupDistributeLabel.Location = new Point(10, 72);
        _alignPopupDistributeLabel.AutoSize = true;
        _alignPopupDistributeLabel.Text = _localization.T("tool.scene.distribute");

        CreateAlignButtons(_alignPopupPanel, 10, 30, ["┝", "┿", "┥", "┰", "┿", "┸"]);
        CreateAlignButtons(_alignPopupPanel, 10, 92, ["⊣", "⊥", "⊢", "⋮", "⋮⋮", "⋮⋮⋮"]);
        _alignPopupPanel.BringToFront();
    }

    private void CreateAlignButtons(Control parent, int x, int y, IReadOnlyList<string> icons)
    {
        for (var i = 0; i < icons.Count; i++)
        {
            var button = new Button
            {
                Parent = parent,
                Text = icons[i],
                Width = 38,
                Height = 28,
                Left = x + i * 42,
                Top = y
            };
            var row = y < 60 ? 0 : 1;
            var col = i;
            button.Click += (_, _) =>
            {
                ApplyAlignAction(row, col);
                _logger.Info(_localization.T("log.alignTriggered"));
            };
        }
    }

    private void ApplyAlignAction(int row, int col)
    {
        if (_currentProject is null || _scenePointNodes.Count == 0)
        {
            return;
        }

        var selected = hierarchyTreeView.SelectedNode;
        var selectedMeta = selected is null ? null : GetMeta(selected);
        if (selectedMeta?.X is null || selectedMeta.Y is null)
        {
            return;
        }

        CaptureHistorySnapshot();
        var selectedX = selectedMeta.X.Value;
        var selectedY = selectedMeta.Y.Value;
        var targets = _scenePointNodes.Where(node =>
        {
            if (node == selected)
            {
                return false;
            }

            var meta = GetMeta(node);
            return meta.X.HasValue && meta.Y.HasValue;
        }).ToList();
        if (targets.Count == 0)
        {
            return;
        }

        if (row == 0)
        {
            foreach (var node in targets)
            {
                var meta = GetMeta(node);
                node.Tag = col switch
                {
                    0 => meta with { X = selectedX - 100f },
                    1 => meta with { X = selectedX },
                    2 => meta with { X = selectedX + 100f },
                    3 => meta with { Y = selectedY - 100f },
                    4 => meta with { Y = selectedY },
                    _ => meta with { Y = selectedY + 100f }
                };
                UpdateNodeText(node);
            }
        }
        else
        {
            if (col is 0 or 1 or 2)
            {
                var sorted = targets.OrderBy(node => GetMeta(node).X!.Value).ToList();
                var start = selectedX - 100f * (sorted.Count - 1) / 2f;
                for (var i = 0; i < sorted.Count; i++)
                {
                    var meta = GetMeta(sorted[i]);
                    sorted[i].Tag = meta with { X = start + i * 100f };
                    UpdateNodeText(sorted[i]);
                }
            }
            else
            {
                var sorted = targets.OrderBy(node => GetMeta(node).Y!.Value).ToList();
                var start = selectedY - 100f * (sorted.Count - 1) / 2f;
                for (var i = 0; i < sorted.Count; i++)
                {
                    var meta = GetMeta(sorted[i]);
                    sorted[i].Tag = meta with { Y = start + i * 100f };
                    UpdateNodeText(sorted[i]);
                }
            }
        }

        SaveTreeData();
        RefreshSceneFromHierarchyTree();
        if (selected is not null)
        {
            PopulateInspectorForNode(selected);
        }
    }

    private void ToggleAlignPopup()
    {
        _alignPopupPanel.Visible = !_alignPopupPanel.Visible;
    }

    private string GetLanguageDisplayName(string language)
    {
        return language switch
        {
            "zh-CN" => _localization.T("language.chinese"),
            "en" => _localization.T("language.english"),
            "ja-JP" => _localization.T("language.japanese"),
            _ => language
        };
    }

    private void Logger_EntryAdded(object? sender, EditorLogEntry entry)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Logger_EntryAdded(sender, entry));
            return;
        }

        var localizedLevel = _localization.T($"log.level.{entry.Level.ToLowerInvariant()}");
        if (string.Equals(localizedLevel, $"log.level.{entry.Level.ToLowerInvariant()}", StringComparison.Ordinal))
        {
            localizedLevel = entry.Level;
        }
        var logLine = $"[{entry.Time:HH:mm:ss}] {localizedLevel}: {entry.Message}";
        logTextBox.Text = string.IsNullOrWhiteSpace(logTextBox.Text)
            ? logLine
            : $"{logTextBox.Text}{Environment.NewLine}{logLine}";
    }

    private void CloseAllModules()
    {
        StopRuntime();
        foreach (var form in _openModules.Values.ToList())
        {
            if (!form.IsDisposed)
            {
                form.Close();
            }
        }

        _openModules.Clear();
    }

    private void StartRuntime(string mode)
    {
        if (_currentProject is null)
        {
            _logger.Warning(_localization.T("log.noProject"));
            return;
        }

        try
        {
            SaveTreeData();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save project before runtime: {ex.Message}");
            System.Windows.Forms.MessageBox.Show(
                this,
                $"{_localization.T("dialog.saveProjectFailed")}: {ex.Message}",
                _localization.T("app.title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (_runtimeWindow is not null && !_runtimeWindow.IsDisposed)
        {
            _runtimeWindow.Activate();
            _runtimeWindow.BringToFront();
            return;
        }

        var runtimeContext = new ProjectContext(CloneProject(_currentProject.Project), _currentProject.RootDirectory);
        _runtimeWindow = new RuntimePreviewForm(runtimeContext, _localization, mode);
        _runtimeWindow.Owner = this;
        _runtimeWindow.FormClosed += (_, _) =>
        {
            _runtimeWindow = null;
            RefreshProjectState();
        };
        _runtimeWindow.Show(this);
        RefreshProjectState();
        _logger.Info(mode == "test"
            ? _localization.T("runtime.startedTest")
            : _localization.T("runtime.startedRun"));
    }

    private void StopRuntime()
    {
        if (_runtimeWindow is null)
        {
            return;
        }

        if (!_runtimeWindow.IsDisposed)
        {
            _runtimeWindow.Close();
        }

        _runtimeWindow = null;
        RefreshProjectState();
    }

    private static AxeProject CloneProject(AxeProject project)
    {
        var json = JsonSerializer.Serialize(project);
        return JsonSerializer.Deserialize<AxeProject>(json) ?? new AxeProject();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        previewBox.Image?.Dispose();
        CloseAllModules();
    }

    private void MainForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.Z)
        {
            UndoSceneChange();
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.Y)
        {
            RedoSceneChange();
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D1)
        {
            CreateHierarchyNodeByShortcut(_localization.T("tree.newHierarchy.object"));
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D2)
        {
            CreateHierarchyNodeByShortcut(_localization.T("tree.newHierarchy.ui"));
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D3)
        {
            CreateHierarchyNodeByShortcut(_localization.T("tree.newHierarchy.camera"));
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D4)
        {
            CreateHierarchyNodeByShortcut(_localization.T("tree.newHierarchy.empty"));
            e.Handled = true;
        }
    }

    private void CreateHierarchyNodeByShortcut(string defaultName)
    {
        if (_currentProject is null)
        {
            return;
        }

        var asChild = hierarchyTreeView.SelectedNode is not null;
        var type = defaultName == _localization.T("tree.newHierarchy.camera") ? NodeTypeCamera
            : defaultName == _localization.T("tree.newHierarchy.ui") ? NodeTypeUi
            : defaultName == _localization.T("tree.newHierarchy.empty") ? NodeTypeEmpty
            : NodeTypeObject;
        CreateHierarchyNodeFromMenu(defaultName, NodeKindItem, type, asChild);
    }

    private static string WithEmojiPrefix(string emoji, string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith(emoji, StringComparison.Ordinal))
        {
            return trimmed;
        }

        return string.IsNullOrWhiteSpace(trimmed) ? emoji : $"{emoji} {trimmed}";
    }

    private static string StripNodePrefix(string text)
    {
        var value = text.Trim();
        var prefixes = new[] { "📁 ", "📂 ", "📌 ", "[D] ", "[N] ", "□ ", "■ ", "⬜ ", "⬛ " };
        foreach (var prefix in prefixes)
        {
            if (value.StartsWith(prefix, StringComparison.Ordinal))
            {
                return value[prefix.Length..].Trim();
            }
        }

        return value;
    }

    private void SaveTreeData()
    {
        if (_currentProject is null)
        {
            return;
        }

        _currentProject.Project.HierarchyTree = SerializeTree(hierarchyTreeView);
        _currentProject.Project.ResourceTree = SerializeTree(assetsTreeView);
        _projectService.SaveProject(_currentProject);
        UpdateHistoryButtonState();
    }

    private void CaptureHistorySnapshot()
    {
        if (_currentProject is null || _suppressHistoryTracking)
        {
            return;
        }

        var snapshot = SerializeTree(hierarchyTreeView);
        var snapshotJson = JsonSerializer.Serialize(snapshot);
        if (snapshotJson == _lastHistorySnapshotJson)
        {
            return;
        }

        _undoStack.Push(snapshot);
        _lastHistorySnapshotJson = snapshotJson;
        while (_undoStack.Count > MaxHistoryEntries)
        {
            var latest = _undoStack.Reverse().Take(MaxHistoryEntries).Reverse().ToList();
            _undoStack.Clear();
            foreach (var historySnapshot in latest)
            {
                _undoStack.Push(historySnapshot);
            }
            break;
        }

        _redoStack.Clear();
        UpdateHistoryButtonState();
    }

    private void UndoSceneChange()
    {
        if (_currentProject is null || _undoStack.Count == 0)
        {
            return;
        }

        _redoStack.Push(SerializeTree(hierarchyTreeView));
        var snapshot = _undoStack.Pop();
        ApplyHistorySnapshot(snapshot);
        _logger.Info("撤销");
        UpdateHistoryButtonState();
        UpdateSceneStatusBar(hierarchyTreeView.SelectedNode);
    }

    private void RedoSceneChange()
    {
        if (_currentProject is null || _redoStack.Count == 0)
        {
            return;
        }

        _undoStack.Push(SerializeTree(hierarchyTreeView));
        var snapshot = _redoStack.Pop();
        ApplyHistorySnapshot(snapshot);
        _logger.Info("重做");
        UpdateHistoryButtonState();
        UpdateSceneStatusBar(hierarchyTreeView.SelectedNode);
    }

    private void ApplyHistorySnapshot(List<ProjectTreeNode> snapshot)
    {
        if (_currentProject is null)
        {
            return;
        }

        _suppressHistoryTracking = true;
        _currentProject.Project.HierarchyTree = snapshot;
        RenderTree(hierarchyTreeView, snapshot);
        hierarchyTreeView.ExpandAll();
        _suppressHistoryTracking = false;
        SaveTreeData();
        RefreshSceneFromHierarchyTree();
        RefreshCameraTargetCandidates();
        _lastHistorySnapshotJson = JsonSerializer.Serialize(snapshot);
        if (hierarchyTreeView.SelectedNode is not null)
        {
            PopulateInspectorForNode(hierarchyTreeView.SelectedNode);
        }
    }

    private void UpdateHistoryButtonState()
    {
        _toolUndoSceneButton.Enabled = _undoStack.Count > 0;
        _toolRedoSceneButton.Enabled = _redoStack.Count > 0;
    }

    private static List<ProjectTreeNode> SerializeTree(TreeView tree)
    {
        var list = new List<ProjectTreeNode>();
        foreach (TreeNode node in tree.Nodes)
        {
            list.Add(SerializeNode(node));
        }

        return list;
    }

    private static ProjectTreeNode SerializeNode(TreeNode node)
    {
        var meta = GetMeta(node);
        var model = new ProjectTreeNode
        {
            Name = meta.RawText,
            Kind = meta.Kind,
            Type = meta.Type,
            PositionX = meta.X,
            PositionY = meta.Y,
            Rotation = meta.Rotation,
            Scale = meta.Scale,
            CameraMode = meta.CameraMode,
            CameraTarget = meta.CameraTarget,
            CameraSmooth = meta.CameraSmooth,
            CameraZoom = meta.CameraZoom
        };

        foreach (TreeNode child in node.Nodes)
        {
            model.Children.Add(SerializeNode(child));
        }

        return model;
    }

    private static void RenderTree(TreeView tree, IEnumerable<ProjectTreeNode> models)
    {
        tree.Nodes.Clear();
        foreach (var model in models)
        {
            tree.Nodes.Add(RenderNode(model));
        }
    }

    private static TreeNode RenderNode(ProjectTreeNode model)
    {
        var kind = string.IsNullOrWhiteSpace(model.Kind) ? NodeKindItem : model.Kind;
        var type = string.IsNullOrWhiteSpace(model.Type) ? InferNodeType(model.Name, kind) : model.Type;
        var node = new TreeNode
        {
            Tag = new NodeMeta(model.Name, kind, type, model.PositionX, model.PositionY, model.Rotation, model.Scale, model.CameraMode, model.CameraTarget, model.CameraSmooth, model.CameraZoom)
        };
        UpdateNodeText(node);
        foreach (var child in model.Children)
        {
            node.Nodes.Add(RenderNode(child));
        }

        return node;
    }

    private static string InferNodeType(string name, string kind)
    {
        if (string.Equals(kind, NodeKindFolder, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        if (name.Contains("camera", StringComparison.OrdinalIgnoreCase) || name.Contains("相机", StringComparison.OrdinalIgnoreCase))
        {
            return NodeTypeCamera;
        }

        if (name.Contains("ui", StringComparison.OrdinalIgnoreCase) || name.Contains("canvas", StringComparison.OrdinalIgnoreCase))
        {
            return NodeTypeUi;
        }

        return NodeTypeObject;
    }

    private void domainUpDown1_SelectedItemChanged(object sender, EventArgs e)
    {

    }

    private sealed record NodeMeta(
        string RawText,
        string Kind,
        string Type = "",
        float? X = null,
        float? Y = null,
        float? Rotation = null,
        float? Scale = null,
        string CameraMode = "",
        string CameraTarget = "",
        float? CameraSmooth = null,
        float? CameraZoom = null);
}
