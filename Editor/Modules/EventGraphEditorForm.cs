using Axe2DEditor.Core.Graphs;
using Axe2DEditor.Core.Projects;
using Axe2DEditor.Editor.Controls;
using Axe2DEditor.Editor.Localization;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace Axe2DEditor.Editor.Modules;

public partial class EventGraphEditorForm : Form
{
    private const string TriggerOwnerParameterKey = "__ownerTriggerId";

    private readonly ProjectContext _context;
    private readonly ProjectService _projectService;
    private readonly LocalizationService _localization;

    private EventGraphDefinition? _selectedGraph;
    private GraphNodeDefinition? _selectedTrigger;
    private GraphNodeDefinition? _selectedCanvasNode;
    private GraphNodeDefinition? _linkSourceNode;
    private Point _canvasContextLocation;
    private string? _canvasContextNodeId;
    private string? _nodeCanvasRootTriggerId;
    private bool _isRefreshingTriggers;
    private bool _isSavingStructuredTrigger;
    private bool _isApplyingScriptHighlight;
    private bool _isLoadingScriptEditor;
    private bool _scriptEditorDirty;
    private string? _loadedScriptTriggerId;
    private string? _dragTriggerId;

    private const string TriggerModeStructured = "structured";
    private const string TriggerModeNodeGraph = "nodeGraph";
    private const string TriggerModeScript = "script";

    private readonly ContextMenuStrip _triggerContextMenu = new();
    private readonly ContextMenuStrip _eventContextMenu = new();
    private readonly ContextMenuStrip _conditionContextMenu = new();
    private readonly ContextMenuStrip _actionContextMenu = new();
    private readonly ContextMenuStrip _nodeCanvasContextMenu = new();
    private readonly ContextMenuStrip _nodeItemContextMenu = new();
    private readonly Dictionary<string, GraphNodeCardControl> _nodeCardControls = new(StringComparer.OrdinalIgnoreCase);

    private readonly ToolStripMenuItem _renameTriggerMenuItem = new();
    private readonly ToolStripMenuItem _duplicateTriggerMenuItem = new();
    private readonly ToolStripMenuItem _toggleEnabledMenuItem = new();
    private readonly ToolStripMenuItem _createStructuredTriggerMenuItem = new();
    private readonly ToolStripMenuItem _createNodeGraphTriggerMenuItem = new();
    private readonly ToolStripMenuItem _createScriptTriggerMenuItem = new();
    private readonly ToolStripMenuItem _structuredModeMenuItem = new();
    private readonly ToolStripMenuItem _nodeGraphModeMenuItem = new();
    private readonly ToolStripMenuItem _convertToScriptMenuItem = new();
    private readonly ToolStripMenuItem _deleteTriggerMenuItem = new();
    private readonly ToolStripMenuItem _createPresetTriggerMenuItem = new();
    private readonly ToolStripMenuItem _createTeleportPresetTriggerMenuItem = new();
    private readonly ToolStripMenuItem _createChestPresetTriggerMenuItem = new();
    private readonly ToolStripMenuItem _createKeyInteractPresetTriggerMenuItem = new();

    private readonly ToolStripMenuItem _addEventMenuItem = new();
    private readonly ToolStripMenuItem _editEventMenuItem = new();
    private readonly ToolStripMenuItem _deleteEventMenuItem = new();

    private readonly ToolStripMenuItem _addConditionMenuItem = new();
    private readonly ToolStripMenuItem _editConditionMenuItem = new();
    private readonly ToolStripMenuItem _deleteConditionMenuItem = new();
    private readonly ToolStripMenuItem _moveConditionUpMenuItem = new();
    private readonly ToolStripMenuItem _moveConditionDownMenuItem = new();

    private readonly ToolStripMenuItem _addActionMenuItem = new();
    private readonly ToolStripMenuItem _editActionMenuItem = new();
    private readonly ToolStripMenuItem _deleteActionMenuItem = new();
    private readonly ToolStripMenuItem _moveActionUpMenuItem = new();
    private readonly ToolStripMenuItem _moveActionDownMenuItem = new();

    private readonly ToolStripMenuItem _createConditionNodeMenuItem = new();
    private readonly ToolStripMenuItem _createActionNodeMenuItem = new();
    private readonly ToolStripMenuItem _setLinkSourceMenuItem = new();
    private readonly ToolStripMenuItem _connectFromSourceMenuItem = new();
    private readonly ToolStripMenuItem _deleteCanvasNodeMenuItem = new();

    public EventGraphEditorForm(ProjectContext context, ProjectService projectService, LocalizationService localization)
    {
        _context = context;
        _projectService = projectService;
        _localization = localization;

        InitializeComponent();
        ConfigureShell();
        ApplyUiStyle();
        BindEvents();
        RefreshLocalization();
        RefreshGraphs();
    }

    public EventGraphEditorForm()
    {
        _context = new ProjectContext(new AxeProject(), Environment.CurrentDirectory);
        _projectService = new ProjectService();
        _localization = new LocalizationService();
        TryLoadDesignLanguage();

        _context.Project.EventGraphs =
        [
            new EventGraphDefinition
            {
                Id = "event.main",
                DisplayName = "主事件图",
                Nodes =
                [
                    new GraphNodeDefinition
                    {
                        Id = "node.1",
                        Kind = NodeKinds.Trigger,
                        Title = "开始触发器",
                        X = 80,
                        Y = 60,
                        Parameters = EventGraphPresentationService.CreateDefaultTriggerParameters(TriggerModeStructured)
                    }
                ],
                Edges = []
            }
        ];

        InitializeComponent();
        ConfigureShell();
        ApplyUiStyle();
        BindEvents();
        RefreshLocalization();
        RefreshGraphs();
    }

    private void TryLoadDesignLanguage()
    {
        try
        {
            _localization.Load("zh-CN");
        }
        catch
        {
            // Keep designer available.
        }
    }

    private void ConfigureShell()
    {
        KeyPreview = true;
        triggerTreeView.ContextMenuStrip = _triggerContextMenu;
        eventListBox.ContextMenuStrip = _eventContextMenu;
        conditionListBox.ContextMenuStrip = _conditionContextMenu;
        actionListBox.ContextMenuStrip = _actionContextMenu;

        _triggerContextMenu.Items.AddRange([
            _createStructuredTriggerMenuItem,
            _createNodeGraphTriggerMenuItem,
            _createScriptTriggerMenuItem,
            _createPresetTriggerMenuItem,
            new ToolStripSeparator(),
            _renameTriggerMenuItem,
            _duplicateTriggerMenuItem,
            _toggleEnabledMenuItem,
            new ToolStripSeparator(),
            _structuredModeMenuItem,
            _nodeGraphModeMenuItem,
            _convertToScriptMenuItem,
            new ToolStripSeparator(),
            _deleteTriggerMenuItem]);

        _eventContextMenu.Items.AddRange([
            _addEventMenuItem,
            _editEventMenuItem,
            _deleteEventMenuItem]);

        _conditionContextMenu.Items.AddRange([
            _addConditionMenuItem,
            _editConditionMenuItem,
            _deleteConditionMenuItem,
            new ToolStripSeparator(),
            _moveConditionUpMenuItem,
            _moveConditionDownMenuItem]);

        _actionContextMenu.Items.AddRange([
            _addActionMenuItem,
            _editActionMenuItem,
            _deleteActionMenuItem,
            new ToolStripSeparator(),
            _moveActionUpMenuItem,
            _moveActionDownMenuItem]);

        _nodeCanvasContextMenu.Items.AddRange([
            _createConditionNodeMenuItem,
            _createActionNodeMenuItem]);

        _nodeItemContextMenu.Items.AddRange([
            _setLinkSourceMenuItem,
            _connectFromSourceMenuItem,
            new ToolStripSeparator(),
            _deleteCanvasNodeMenuItem]);

        // Attach tab select all behavior to numeric up down controls
        TabSelectAllBehavior.Attach(nodeXNumericUpDown);
        TabSelectAllBehavior.Attach(nodeYNumericUpDown);
    }


    private void ApplyUiStyle()
    {
        Text = _localization.T("module.eventGraphEditor");
        SetListBoxStyle(eventListBox);
        SetListBoxStyle(conditionListBox);
        SetListBoxStyle(actionListBox);
        SetStructuredListRendering(eventListBox);
        SetStructuredListRendering(conditionListBox);
        SetStructuredListRendering(actionListBox);

        workspaceSplitContainer.SplitterWidth = 8;
        workspaceSplitContainer.Panel1MinSize = 0;
        workspaceSplitContainer.Panel2MinSize = 0;
        nodeDetailsGroupBox.Visible = false;
        if (nodeGraphPanel.RowStyles.Count > 2)
        {
            nodeGraphPanel.RowStyles[2].SizeType = SizeType.Absolute;
            nodeGraphPanel.RowStyles[2].Height = 0;
        }
    }

    private void BindEvents()
    {
        graphToolStripHost.SelectedIndexChanged += (_, _) => OnGraphChanged();
        triggerTreeView.AfterSelect += TriggerTreeView_AfterSelect;
        triggerTreeView.NodeMouseClick += TriggerTreeView_NodeMouseClick;
        triggerTreeView.ItemDrag += TriggerTreeView_ItemDrag;
        triggerTreeView.DragEnter += TriggerTreeView_DragEnter;
        triggerTreeView.DragOver += TriggerTreeView_DragOver;
        triggerTreeView.DragDrop += TriggerTreeView_DragDrop;
        _triggerContextMenu.Opening += TriggerContextMenu_Opening;
        _eventContextMenu.Opening += (_, e) => ConfigureStructuredListMenu(_eventContextMenu, eventListBox.SelectedItem is not null, false, false, e);
        _conditionContextMenu.Opening += (_, e) => ConfigureStructuredListMenu(_conditionContextMenu, conditionListBox.SelectedItem is not null, true, conditionListBox.SelectedIndex > 0, e, conditionListBox.SelectedIndex >= 0 && conditionListBox.SelectedIndex < conditionListBox.Items.Count - 1);
        _actionContextMenu.Opening += (_, e) => ConfigureStructuredListMenu(_actionContextMenu, actionListBox.SelectedItem is not null, true, actionListBox.SelectedIndex > 0, e, actionListBox.SelectedIndex >= 0 && actionListBox.SelectedIndex < actionListBox.Items.Count - 1);
        eventListBox.DoubleClick += (_, _) => AddOrEditEvent(true);
        conditionListBox.DoubleClick += (_, _) => EditStructuredNode(NodeKinds.Condition);
        actionListBox.DoubleClick += (_, _) => EditStructuredNode(NodeKinds.Action);
        conditionListBox.SelectedIndexChanged += (_, _) => UpdateStructuredButtonStates();
        actionListBox.SelectedIndexChanged += (_, _) => UpdateStructuredButtonStates();
        Shown += (_, _) => BeginInvoke(new Action(ApplyWorkspaceSplitLayout));
        SizeChanged += (_, _) => ApplyWorkspaceSplitLayout();
        _nodeCanvasContextMenu.Opening += NodeCanvasContextMenu_Opening;
        _nodeItemContextMenu.Opening += NodeItemContextMenu_Opening;

        newTriggerToolStripButton.Click += (_, _) => CreateTrigger(false);
        newScriptTriggerToolStripButton.Click += (_, _) => CreateTrigger(true);
        duplicateTriggerToolStripButton.Click += (_, _) => DuplicateSelectedTrigger();
        removeTriggerToolStripButton.Click += (_, _) => DeleteSelectedTrigger();
        toggleEnabledToolStripButton.Click += (_, _) => ToggleSelectedTriggerEnabled();
        structuredModeToolStripButton.Click += (_, _) => SetSelectedTriggerMode(TriggerModeStructured);
        nodeGraphModeToolStripButton.Click += (_, _) => SetSelectedTriggerMode(TriggerModeNodeGraph);
        convertToScriptToolStripButton.Click += (_, _) => ConvertSelectedTriggerToScript();
        saveProjectToolStripButton.Click += (_, _) => SaveProject();

        _renameTriggerMenuItem.Click += (_, _) => triggerNameTextBox.Focus();
        _createStructuredTriggerMenuItem.Click += (_, _) => CreateTrigger(TriggerModeStructured);
        _createNodeGraphTriggerMenuItem.Click += (_, _) => CreateTrigger(TriggerModeNodeGraph);
        _createScriptTriggerMenuItem.Click += (_, _) => CreateTrigger(TriggerModeScript);
        _createTeleportPresetTriggerMenuItem.Click += (_, _) => CreatePresetTrigger("teleport");
        _createChestPresetTriggerMenuItem.Click += (_, _) => CreatePresetTrigger("chest");
        _createKeyInteractPresetTriggerMenuItem.Click += (_, _) => CreatePresetTrigger("keyInteract");
        _duplicateTriggerMenuItem.Click += (_, _) => DuplicateSelectedTrigger();
        _toggleEnabledMenuItem.Click += (_, _) => ToggleSelectedTriggerEnabled();
        _structuredModeMenuItem.Click += (_, _) => SetSelectedTriggerMode(TriggerModeStructured);
        _nodeGraphModeMenuItem.Click += (_, _) => SetSelectedTriggerMode(TriggerModeNodeGraph);
        _convertToScriptMenuItem.Click += (_, _) => ConvertSelectedTriggerToScript();
        _deleteTriggerMenuItem.Click += (_, _) => DeleteSelectedTrigger();

        addEventButton.Click += (_, _) => AddOrEditEvent(false);
        editEventButton.Click += (_, _) => AddOrEditEvent(true);
        deleteEventButton.Click += (_, _) => DeleteEvent();
        _addEventMenuItem.Click += (_, _) => AddOrEditEvent(false);
        _editEventMenuItem.Click += (_, _) => AddOrEditEvent(true);
        _deleteEventMenuItem.Click += (_, _) => DeleteEvent();

        addConditionButton.Click += (_, _) => AddStructuredNode(NodeKinds.Condition);
        editConditionButton.Click += (_, _) => EditStructuredNode(NodeKinds.Condition);
        deleteConditionButton.Click += (_, _) => DeleteStructuredNode(NodeKinds.Condition);
        moveConditionUpButton.Click += (_, _) => MoveStructuredNode(NodeKinds.Condition, -1);
        moveConditionDownButton.Click += (_, _) => MoveStructuredNode(NodeKinds.Condition, 1);
        _addConditionMenuItem.Click += (_, _) => AddStructuredNode(NodeKinds.Condition);
        _editConditionMenuItem.Click += (_, _) => EditStructuredNode(NodeKinds.Condition);
        _deleteConditionMenuItem.Click += (_, _) => DeleteStructuredNode(NodeKinds.Condition);
        _moveConditionUpMenuItem.Click += (_, _) => MoveStructuredNode(NodeKinds.Condition, -1);
        _moveConditionDownMenuItem.Click += (_, _) => MoveStructuredNode(NodeKinds.Condition, 1);

        addActionButton.Click += (_, _) => AddStructuredNode(NodeKinds.Action);
        editActionButton.Click += (_, _) => EditStructuredNode(NodeKinds.Action);
        deleteActionButton.Click += (_, _) => DeleteStructuredNode(NodeKinds.Action);
        moveActionUpButton.Click += (_, _) => MoveStructuredNode(NodeKinds.Action, -1);
        moveActionDownButton.Click += (_, _) => MoveStructuredNode(NodeKinds.Action, 1);
        _addActionMenuItem.Click += (_, _) => AddStructuredNode(NodeKinds.Action);
        _editActionMenuItem.Click += (_, _) => EditStructuredNode(NodeKinds.Action);
        _deleteActionMenuItem.Click += (_, _) => DeleteStructuredNode(NodeKinds.Action);
        _moveActionUpMenuItem.Click += (_, _) => MoveStructuredNode(NodeKinds.Action, -1);
        _moveActionDownMenuItem.Click += (_, _) => MoveStructuredNode(NodeKinds.Action, 1);

        nodeSaveButton.Click += (_, _) => SaveCanvasNode();
        scriptCodeRichTextBox.TextChanged += ScriptCodeRichTextBox_TextChanged;
        triggerNameTextBox.Leave += (_, _) => SaveStructuredTrigger();
        scriptFormatButton.Click += (_, _) => CheckScriptFormat();

        nodeCanvasPanel.SelectionChanged += NodeCanvasPanel_SelectionChanged;
        nodeCanvasPanel.GraphChanged += NodeCanvasPanel_GraphChanged;
        nodeCanvasPanel.NodeContextRequested += NodeCanvasPanel_NodeContextRequested;
        nodeCanvasPanel.CanvasContextRequested += NodeCanvasPanel_CanvasContextRequested;
        _createConditionNodeMenuItem.Click += (_, _) => CreateNodeAtCanvas(NodeKinds.Condition);
        _createActionNodeMenuItem.Click += (_, _) => CreateNodeAtCanvas(NodeKinds.Action);
        _setLinkSourceMenuItem.Click += (_, _) => SetCanvasLinkSource();
        _connectFromSourceMenuItem.Click += (_, _) => ConnectCanvasNodeFromSource();
        _deleteCanvasNodeMenuItem.Click += (_, _) => DeleteCanvasNode();

        KeyDown += EventGraphEditorForm_KeyDown;
        FormClosing += EventGraphEditorForm_FormClosing;
    }

    private void ApplyWorkspaceSplitLayout()
    {
        SplitContainerLayout.ApplySafe(workspaceSplitContainer, 320, 300, 700);
    }

    private void RefreshLocalization()
    {
        Text = _localization.T("module.eventGraphEditor");

        graphToolStripLabel.Text = _localization.T("graph.toolbar.graph");
        newTriggerToolStripButton.Text = "✨";
        newTriggerToolStripButton.ToolTipText = $"{_localization.T("graph.button.newTrigger")} (Ctrl+N)";
        newScriptTriggerToolStripButton.Text = "📝";
        newScriptTriggerToolStripButton.ToolTipText = $"{_localization.T("graph.button.newScriptTrigger")} (Ctrl+Shift+N)";
        duplicateTriggerToolStripButton.Text = "📄";
        duplicateTriggerToolStripButton.ToolTipText = $"{_localization.T("graph.button.duplicateTrigger")} (Ctrl+D)";
        removeTriggerToolStripButton.Text = "🗑";
        removeTriggerToolStripButton.ToolTipText = $"{_localization.T("graph.button.removeTrigger")} (Delete)";
        toggleEnabledToolStripButton.Text = "⚡";
        toggleEnabledToolStripButton.ToolTipText = $"{_localization.T("graph.button.toggleEnabled")} (Ctrl+E)";
        structuredModeToolStripButton.Text = "📋";
        structuredModeToolStripButton.ToolTipText = $"{_localization.T("graph.button.structuredMode")} (Ctrl+1)";
        nodeGraphModeToolStripButton.Text = "🧩";
        nodeGraphModeToolStripButton.ToolTipText = $"{_localization.T("graph.button.nodeGraphMode")} (Ctrl+2)";
        convertToScriptToolStripButton.Text = "📜";
        convertToScriptToolStripButton.ToolTipText = $"{_localization.T("graph.button.convertToScript")} (Ctrl+3)";
        saveProjectToolStripButton.Text = "💾";
        saveProjectToolStripButton.ToolTipText = $"{_localization.T("graph.button.saveProject")} (Ctrl+S)";

        triggerListTitleLabel.Text = _localization.T("graph.panel.triggers");

        triggerNameLabel.Text = _localization.T("graph.field.triggerName");
        scriptFormatButton.Text = _localization.T("graph.button.checkScriptFormat");

        emptyStateLabel.Text = _localization.T("graph.summary.noTriggerSelected");

        eventGroupBox.Text = _localization.T("graph.section.event");
        conditionGroupBox.Text = _localization.T("graph.section.condition");
        actionGroupBox.Text = _localization.T("graph.section.action");
        advancedGroupBox.Text = _localization.T("graph.section.advanced");
        nodeDetailsGroupBox.Text = _localization.T("graph.panel.nodeProperties");
        scriptPathLabel.Text = _localization.T("graph.field.scriptPath");

        addEventButton.Text = _localization.T("common.add");
        editEventButton.Text = _localization.T("common.edit");
        deleteEventButton.Text = _localization.T("common.delete");

        addConditionButton.Text = _localization.T("common.add");
        editConditionButton.Text = _localization.T("common.edit");
        deleteConditionButton.Text = _localization.T("common.delete");
        moveConditionUpButton.Text = _localization.T("common.moveUp");
        moveConditionDownButton.Text = _localization.T("common.moveDown");

        addActionButton.Text = _localization.T("common.add");
        editActionButton.Text = _localization.T("common.edit");
        deleteActionButton.Text = _localization.T("common.delete");
        moveActionUpButton.Text = _localization.T("common.moveUp");
        moveActionDownButton.Text = _localization.T("common.moveDown");

        nodeSaveButton.Text = _localization.T("graph.button.saveNode");

        nodeGraphHelpLabel.Text = _localization.T("graph.help.nodeCanvas");
        nodeTypeLabel.Text = _localization.T("graph.field.nodeType");
        nodeTitleLabel.Text = _localization.T("graph.field.title");
        nodeXLabel.Text = _localization.T("graph.field.positionX");
        nodeYLabel.Text = _localization.T("graph.field.positionY");
        nodeParametersLabel.Text = _localization.T("graph.field.parameters");
        scriptHelpLabel.Text = _localization.T("graph.help.scriptMode");

        _createStructuredTriggerMenuItem.Text = _localization.T("graph.menu.createStructuredTrigger");
        _createNodeGraphTriggerMenuItem.Text = _localization.T("graph.menu.createNodeGraphTrigger");
        _createScriptTriggerMenuItem.Text = _localization.T("graph.menu.createScriptTrigger");
        _createPresetTriggerMenuItem.Text = _localization.T("graph.menu.createPresetTrigger");
        _createPresetTriggerMenuItem.DropDownItems.Clear();
        _createPresetTriggerMenuItem.DropDownItems.AddRange([
            _createTeleportPresetTriggerMenuItem,
            _createChestPresetTriggerMenuItem,
            _createKeyInteractPresetTriggerMenuItem
        ]);
        _createTeleportPresetTriggerMenuItem.Text = _localization.T("graph.menu.presetTeleport");
        _createChestPresetTriggerMenuItem.Text = _localization.T("graph.menu.presetChest");
        _createKeyInteractPresetTriggerMenuItem.Text = _localization.T("graph.menu.presetKeyInteract");
        _renameTriggerMenuItem.Text = _localization.T("graph.menu.renameTrigger");
        _duplicateTriggerMenuItem.Text = _localization.T("graph.menu.duplicateTrigger");
        _toggleEnabledMenuItem.Text = _localization.T("graph.menu.toggleEnabled");
        _structuredModeMenuItem.Text = _localization.T("graph.menu.structuredMode");
        _nodeGraphModeMenuItem.Text = _localization.T("graph.menu.nodeGraphMode");
        _convertToScriptMenuItem.Text = _localization.T("graph.menu.convertToScript");
        _deleteTriggerMenuItem.Text = _localization.T("graph.menu.deleteTrigger");
        _duplicateTriggerMenuItem.ShortcutKeyDisplayString = "Ctrl+D";
        _toggleEnabledMenuItem.ShortcutKeyDisplayString = "Ctrl+E";
        _structuredModeMenuItem.ShortcutKeyDisplayString = "Ctrl+1";
        _nodeGraphModeMenuItem.ShortcutKeyDisplayString = "Ctrl+2";
        _convertToScriptMenuItem.ShortcutKeyDisplayString = "Ctrl+3";
        _deleteTriggerMenuItem.ShortcutKeyDisplayString = "Delete";

        _addEventMenuItem.Text = _localization.T("common.add");
        _editEventMenuItem.Text = _localization.T("common.edit");
        _deleteEventMenuItem.Text = _localization.T("common.delete");

        _addConditionMenuItem.Text = _localization.T("common.add");
        _editConditionMenuItem.Text = _localization.T("common.edit");
        _deleteConditionMenuItem.Text = _localization.T("common.delete");
        _moveConditionUpMenuItem.Text = _localization.T("common.moveUp");
        _moveConditionDownMenuItem.Text = _localization.T("common.moveDown");

        _addActionMenuItem.Text = _localization.T("common.add");
        _editActionMenuItem.Text = _localization.T("common.edit");
        _deleteActionMenuItem.Text = _localization.T("common.delete");
        _moveActionUpMenuItem.Text = _localization.T("common.moveUp");
        _moveActionDownMenuItem.Text = _localization.T("common.moveDown");

        _createConditionNodeMenuItem.Text = _localization.T("graph.node.createCondition");
        _createActionNodeMenuItem.Text = _localization.T("graph.node.createAction");
        _setLinkSourceMenuItem.Text = _localization.T("graph.node.setLinkSource");
        _connectFromSourceMenuItem.Text = _localization.T("graph.node.connectFromSource");
        _deleteCanvasNodeMenuItem.Text = _localization.T("common.delete");
        nodeCanvasPanel.Localization = _localization;
    }

    private void RefreshGraphs(string? targetGraphId = null)
    {
        var selectedId = targetGraphId ?? (graphToolStripHost.SelectedItem as EventGraphDefinition)?.Id ?? _selectedGraph?.Id;
        graphToolStripHost.Items.Clear();

        foreach (var graph in _context.Project.EventGraphs)
        {
            graphToolStripHost.Items.Add(graph);
        }

        if (graphToolStripHost.Items.Count == 0)
        {
            _selectedGraph = null;
            RefreshTriggerList();
            return;
        }

        var selectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            for (var i = 0; i < graphToolStripHost.Items.Count; i++)
            {
                if (graphToolStripHost.Items[i] is EventGraphDefinition graph && graph.Id == selectedId)
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        graphToolStripHost.SelectedIndex = selectedIndex;
    }

    private void OnGraphChanged()
    {
        PersistPendingEditorChanges();
        _selectedGraph = graphToolStripHost.SelectedItem as EventGraphDefinition;
        _selectedTrigger = null;
        _selectedCanvasNode = null;
        _linkSourceNode = null;
        RefreshTriggerList();
    }

    private void RefreshTriggerList(string? selectedTriggerId = null)
    {
        selectedTriggerId ??= _selectedTrigger?.Id;
        _isRefreshingTriggers = true;
        try
        {
            triggerTreeView.BeginUpdate();
            triggerTreeView.Nodes.Clear();

            if (_selectedGraph is null)
            {
                _selectedTrigger = null;
                RefreshRightPane();
                return;
            }

            var rootNode = new TreeNode(_selectedGraph.DisplayName)
            {
                Name = _selectedGraph.Id,
                Tag = _selectedGraph
            };

            foreach (var trigger in _selectedGraph.Nodes.Where(node => node.Kind == NodeKinds.Trigger))
            {
                rootNode.Nodes.Add(CreateTriggerTreeNode(trigger));
            }

            triggerTreeView.Nodes.Add(rootNode);
            rootNode.Expand();

            var selectedNode = !string.IsNullOrWhiteSpace(selectedTriggerId)
                ? FindTriggerTreeNode(rootNode, selectedTriggerId)
                : FindFirstTriggerTreeNode(rootNode);
            selectedNode ??= rootNode;

            triggerTreeView.SelectedNode = selectedNode;
            selectedNode.EnsureVisible();
        }
        finally
        {
            triggerTreeView.EndUpdate();
            _isRefreshingTriggers = false;
        }

        ApplyTreeSelection(triggerTreeView.SelectedNode, saveCurrent: false);
    }

    private void TriggerTreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (_isRefreshingTriggers)
        {
            return;
        }

        ApplyTreeSelection(e.Node, saveCurrent: true);
    }

    private void TriggerTreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        triggerTreeView.SelectedNode = e.Node;
    }

    private void TriggerTreeView_ItemDrag(object? sender, ItemDragEventArgs e)
    {
        if (e.Item is not TreeNode node || node.Tag is not GraphNodeDefinition trigger || trigger.Kind != NodeKinds.Trigger)
        {
            return;
        }

        _dragTriggerId = trigger.Id;
        DoDragDrop(trigger.Id, DragDropEffects.Move);
    }

    private void TriggerTreeView_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(typeof(string)) == true ? DragDropEffects.Move : DragDropEffects.None;
    }

    private void TriggerTreeView_DragOver(object? sender, DragEventArgs e)
    {
        var draggedTrigger = ResolveDraggedTrigger(e);
        if (draggedTrigger is null)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        var targetNode = GetTreeNodeFromDragEvent(e);
        if (targetNode is null)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        triggerTreeView.SelectedNode = targetNode;
        e.Effect = CanDropTriggerOnNode(draggedTrigger, targetNode) ? DragDropEffects.Move : DragDropEffects.None;
    }

    private void TriggerTreeView_DragDrop(object? sender, DragEventArgs e)
    {
        var draggedTrigger = ResolveDraggedTrigger(e);
        var targetNode = GetTreeNodeFromDragEvent(e);
        _dragTriggerId = null;

        if (draggedTrigger is null || targetNode is null || _selectedGraph is null)
        {
            return;
        }

        if (ResolveTriggerFromTreeNode(targetNode) is { } targetTrigger)
        {
            if (string.Equals(targetTrigger.Id, draggedTrigger.Id, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            MoveTriggerBefore(draggedTrigger, targetTrigger);
            RefreshTriggerList(draggedTrigger.Id);
            return;
        }

        if (targetNode.Tag is EventGraphDefinition)
        {
            MoveTriggerToGroupEnd(draggedTrigger);
            RefreshTriggerList(draggedTrigger.Id);
        }
    }

    private void ApplyTreeSelection(TreeNode? node, bool saveCurrent)
    {
        if (saveCurrent)
        {
            PersistPendingEditorChanges();
        }

        _selectedTrigger = ResolveTriggerFromTreeNode(node);
        var selectedStructuredNode = node?.Tag is TriggerTreeNodeTag tag ? tag.Node : null;

        _selectedCanvasNode = null;
        _linkSourceNode = null;
        RefreshRightPane();
        ApplyStructuredSelection(selectedStructuredNode);
    }

    private TreeNode CreateTriggerTreeNode(GraphNodeDefinition trigger)
    {
        var triggerNode = new TreeNode(CreateTriggerTreeText(trigger))
        {
            Name = trigger.Id,
            Tag = trigger
        };

        return triggerNode;
    }

    private void ApplyStructuredSelection(GraphNodeDefinition? structuredNode)
    {
        conditionListBox.ClearSelected();
        actionListBox.ClearSelected();

        if (structuredNode is null)
        {
            return;
        }

        var listBox = structuredNode.Kind switch
        {
            NodeKinds.Condition => conditionListBox,
            NodeKinds.Action => actionListBox,
            _ => null
        };
        if (listBox is null)
        {
            return;
        }

        for (var i = 0; i < listBox.Items.Count; i++)
        {
            if (listBox.Items[i] is NodeViewItem item && string.Equals(item.Node.Id, structuredNode.Id, StringComparison.OrdinalIgnoreCase))
            {
                listBox.SelectedIndex = i;
                break;
            }
        }

        UpdateStructuredButtonStates();
    }

    private GraphNodeDefinition? ResolveTriggerFromTreeNode(TreeNode? node)
    {
        while (node is not null)
        {
            switch (node.Tag)
            {
                case GraphNodeDefinition trigger when trigger.Kind == NodeKinds.Trigger:
                    return trigger;
                case TriggerTreeNodeTag tag:
                    return tag.Trigger;
            }

            node = node.Parent;
        }

        return null;
    }

    private static TreeNode? FindTriggerTreeNode(TreeNode rootNode, string triggerId)
    {
        foreach (TreeNode child in rootNode.Nodes)
        {
            if (child.Tag is GraphNodeDefinition trigger && string.Equals(trigger.Id, triggerId, StringComparison.OrdinalIgnoreCase))
            {
                return child;
            }

            var nested = FindTriggerTreeNode(child, triggerId);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private static TreeNode? FindFirstTriggerTreeNode(TreeNode rootNode)
    {
        foreach (TreeNode child in rootNode.Nodes)
        {
            if (child.Tag is GraphNodeDefinition trigger && trigger.Kind == NodeKinds.Trigger)
            {
                return child;
            }

            var nested = FindFirstTriggerTreeNode(child);
            if (nested is not null)
            {
                return nested;
            }
        }

        return null;
    }

    private GraphNodeDefinition? ResolveDraggedTrigger(DragEventArgs e)
    {
        var triggerId = e.Data?.GetData(typeof(string)) as string ?? _dragTriggerId;
        if (string.IsNullOrWhiteSpace(triggerId) || _selectedGraph is null)
        {
            return null;
        }

        return _selectedGraph.Nodes.FirstOrDefault(node => node.Kind == NodeKinds.Trigger && string.Equals(node.Id, triggerId, StringComparison.OrdinalIgnoreCase));
    }

    private TreeNode? GetTreeNodeFromDragEvent(DragEventArgs e)
    {
        var point = triggerTreeView.PointToClient(new Point(e.X, e.Y));
        return triggerTreeView.GetNodeAt(point);
    }

    private bool CanDropTriggerOnNode(GraphNodeDefinition draggedTrigger, TreeNode targetNode)
    {
        if (ResolveTriggerFromTreeNode(targetNode) is { } targetTrigger)
        {
            return !string.Equals(targetTrigger.Id, draggedTrigger.Id, StringComparison.OrdinalIgnoreCase);
        }

        return targetNode.Tag is EventGraphDefinition;
    }

    private void MoveTriggerBefore(GraphNodeDefinition draggedTrigger, GraphNodeDefinition targetTrigger)
    {
        if (_selectedGraph is null)
        {
            return;
        }

        var draggedIndex = _selectedGraph.Nodes.FindIndex(node => string.Equals(node.Id, draggedTrigger.Id, StringComparison.OrdinalIgnoreCase));
        var targetIndex = _selectedGraph.Nodes.FindIndex(node => string.Equals(node.Id, targetTrigger.Id, StringComparison.OrdinalIgnoreCase));
        if (draggedIndex < 0 || targetIndex < 0 || draggedIndex == targetIndex)
        {
            return;
        }

        var item = _selectedGraph.Nodes[draggedIndex];
        _selectedGraph.Nodes.RemoveAt(draggedIndex);
        if (draggedIndex < targetIndex)
        {
            targetIndex--;
        }

        _selectedGraph.Nodes.Insert(targetIndex, item);
    }

    private void MoveTriggerToGroupEnd(GraphNodeDefinition draggedTrigger)
    {
        if (_selectedGraph is null)
        {
            return;
        }

        var lastTrigger = _selectedGraph.Nodes.LastOrDefault(node => node.Kind == NodeKinds.Trigger
            && !string.Equals(node.Id, draggedTrigger.Id, StringComparison.OrdinalIgnoreCase));
        if (lastTrigger is null)
        {
            return;
        }

        MoveTriggerAfter(draggedTrigger, lastTrigger);
    }

    private void MoveTriggerAfter(GraphNodeDefinition draggedTrigger, GraphNodeDefinition targetTrigger)
    {
        if (_selectedGraph is null)
        {
            return;
        }

        var draggedIndex = _selectedGraph.Nodes.FindIndex(node => string.Equals(node.Id, draggedTrigger.Id, StringComparison.OrdinalIgnoreCase));
        var targetIndex = _selectedGraph.Nodes.FindIndex(node => string.Equals(node.Id, targetTrigger.Id, StringComparison.OrdinalIgnoreCase));
        if (draggedIndex < 0 || targetIndex < 0 || draggedIndex == targetIndex)
        {
            return;
        }

        var item = _selectedGraph.Nodes[draggedIndex];
        _selectedGraph.Nodes.RemoveAt(draggedIndex);
        if (draggedIndex < targetIndex)
        {
            targetIndex--;
        }

        _selectedGraph.Nodes.Insert(targetIndex + 1, item);
    }

    private void TriggerContextMenu_Opening(object? sender, CancelEventArgs e)
    {
        var hasTrigger = _selectedTrigger is not null;
        ShowMenuItem(_createStructuredTriggerMenuItem, true);
        ShowMenuItem(_createNodeGraphTriggerMenuItem, true);
        ShowMenuItem(_createScriptTriggerMenuItem, true);
        ShowMenuItem(_createPresetTriggerMenuItem, true);

        if (!hasTrigger)
        {
            ShowMenuItem(_renameTriggerMenuItem, false);
            ShowMenuItem(_duplicateTriggerMenuItem, false);
            ShowMenuItem(_toggleEnabledMenuItem, false);
            ShowMenuItem(_structuredModeMenuItem, false);
            ShowMenuItem(_nodeGraphModeMenuItem, false);
            ShowMenuItem(_convertToScriptMenuItem, false);
            ShowMenuItem(_deleteTriggerMenuItem, false);
            CleanupMenuSeparators(_triggerContextMenu);
            return;
        }

        var mode = GetTriggerMode(_selectedTrigger!);
        ShowMenuItem(_renameTriggerMenuItem, true);
        ShowMenuItem(_duplicateTriggerMenuItem, true);
        ShowMenuItem(_toggleEnabledMenuItem, true);
        ShowMenuItem(_structuredModeMenuItem, mode != TriggerModeStructured);
        ShowMenuItem(_nodeGraphModeMenuItem, mode != TriggerModeNodeGraph);
        ShowMenuItem(_convertToScriptMenuItem, mode != TriggerModeScript);
        ShowMenuItem(_deleteTriggerMenuItem, true);
        CleanupMenuSeparators(_triggerContextMenu);
    }

    private void ConfigureStructuredListMenu(ContextMenuStrip menu, bool hasSelection, bool includeMove, bool canMoveUp, CancelEventArgs e, bool canMoveDown = false)
    {
        if (_selectedTrigger is null)
        {
            e.Cancel = true;
            return;
        }

        var editable = !EventGraphAnalysisService.AnalyzeTrigger(_selectedGraph!, _selectedTrigger).IsComplex;
        if (!editable)
        {
            e.Cancel = true;
            return;
        }

        if (ReferenceEquals(menu, _eventContextMenu))
        {
            ShowMenuItem(_addEventMenuItem, true);
            ShowMenuItem(_editEventMenuItem, hasSelection);
            ShowMenuItem(_deleteEventMenuItem, hasSelection);
        }
        else if (ReferenceEquals(menu, _conditionContextMenu))
        {
            ShowMenuItem(_addConditionMenuItem, true);
            ShowMenuItem(_editConditionMenuItem, hasSelection);
            ShowMenuItem(_deleteConditionMenuItem, hasSelection);
            ShowMenuItem(_moveConditionUpMenuItem, includeMove && canMoveUp);
            ShowMenuItem(_moveConditionDownMenuItem, includeMove && canMoveDown);
        }
        else if (ReferenceEquals(menu, _actionContextMenu))
        {
            ShowMenuItem(_addActionMenuItem, true);
            ShowMenuItem(_editActionMenuItem, hasSelection);
            ShowMenuItem(_deleteActionMenuItem, hasSelection);
            ShowMenuItem(_moveActionUpMenuItem, includeMove && canMoveUp);
            ShowMenuItem(_moveActionDownMenuItem, includeMove && canMoveDown);
        }

        CleanupMenuSeparators(menu);
    }

    private void NodeCanvasContextMenu_Opening(object? sender, CancelEventArgs e)
    {
        if (_selectedTrigger is null || GetTriggerMode(_selectedTrigger) != TriggerModeNodeGraph)
        {
            e.Cancel = true;
            return;
        }

        ShowMenuItem(_createConditionNodeMenuItem, true);
        ShowMenuItem(_createActionNodeMenuItem, true);
        CleanupMenuSeparators(_nodeCanvasContextMenu);
    }

    private void NodeItemContextMenu_Opening(object? sender, CancelEventArgs e)
    {
        if (_selectedCanvasNode is null)
        {
            e.Cancel = true;
            return;
        }

        ShowMenuItem(_setLinkSourceMenuItem, true);
        ShowMenuItem(_connectFromSourceMenuItem, _linkSourceNode is not null && !string.Equals(_linkSourceNode.Id, _selectedCanvasNode.Id, StringComparison.OrdinalIgnoreCase));
        ShowMenuItem(_deleteCanvasNodeMenuItem, _selectedCanvasNode.Kind != NodeKinds.Trigger);
        CleanupMenuSeparators(_nodeItemContextMenu);
    }

    private static void ShowMenuItem(ToolStripItem item, bool visible)
    {
        item.Visible = visible;
        item.Enabled = visible;
    }

    private static void CleanupMenuSeparators(ContextMenuStrip menu)
    {
        var previousVisibleWasSeparator = true;
        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripSeparator separator)
            {
                separator.Visible = !previousVisibleWasSeparator;
                previousVisibleWasSeparator = true;
                continue;
            }

            if (!item.Visible)
            {
                continue;
            }

            previousVisibleWasSeparator = false;
        }

        for (var i = menu.Items.Count - 1; i >= 0; i--)
        {
            if (menu.Items[i] is ToolStripSeparator separator)
            {
                if (separator.Visible)
                {
                    break;
                }
            }
            else if (menu.Items[i].Visible)
            {
                break;
            }
        }

        for (var i = menu.Items.Count - 1; i >= 0; i--)
        {
            if (menu.Items[i] is ToolStripSeparator separator)
            {
                if (separator.Visible)
                {
                    separator.Visible = false;
                    continue;
                }
            }
            break;
        }
    }

    private string CreateTriggerTreeText(GraphNodeDefinition node)
    {
        var title = string.IsNullOrWhiteSpace(node.Title) ? node.Id : node.Title;
        var modeIcon = GetTriggerMode(node) switch
        {
            TriggerModeNodeGraph => "🧩",
            TriggerModeScript => "📜",
            _ => "📋"
        };
        return $"{modeIcon} {title}";
    }

    private void RefreshRightPane()
    {
        var hasTrigger = _selectedTrigger is not null;
        triggerNameTextBox.Enabled = hasTrigger;
        triggerNameTextBox.Text = hasTrigger ? _selectedTrigger!.Title : string.Empty;
        var isScriptMode = hasTrigger && string.Equals(GetTriggerMode(_selectedTrigger!), TriggerModeScript, StringComparison.OrdinalIgnoreCase);
        scriptFormatButton.Visible = isScriptMode;
        triggerHeaderLayout.ColumnStyles[2].Width = isScriptMode ? 168F : 0F;

        emptyStatePanel.Visible = !hasTrigger;
        structuredPanel.Visible = false;
        nodeGraphPanel.Visible = false;
        scriptPanel.Visible = false;

        if (!hasTrigger)
        {
            return;
        }

        var mode = GetTriggerMode(_selectedTrigger!);
        switch (mode)
        {
            case TriggerModeNodeGraph:
                nodeGraphPanel.Visible = true;
                RefreshNodeGraphPanel();
                break;
            case TriggerModeScript:
                scriptPanel.Visible = true;
                RefreshScriptPanel();
                break;
            default:
                structuredPanel.Visible = true;
                RefreshStructuredPanel();
                break;
        }
    }

    private void RefreshStructuredPanel()
    {
        if (_selectedTrigger is null)
        {
            return;
        }

        var view = EventGraphAnalysisService.AnalyzeTrigger(_selectedGraph!, _selectedTrigger);
        structuredSummaryLabel.Text = view.IsComplex
            ? _localization.T("graph.summary.complexTrigger")
            : _localization.Format("graph.summary.triggerMode", IsTriggerEnabled(_selectedTrigger) ? _localization.T("graph.state.enabled") : _localization.T("graph.state.disabled"), _localization.T("graph.mode.structured"));

        eventListBox.Items.Clear();
        var eventSummary = EventGraphPresentationService.SummarizeTriggerEvent(_localization, _selectedTrigger.Parameters);
        if (!string.IsNullOrWhiteSpace(eventSummary))
        {
            eventListBox.Items.Add(new StringViewItem(eventSummary));
        }

        conditionListBox.Items.Clear();
        var connectedConditionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var condition in view.Conditions)
        {
            connectedConditionIds.Add(condition.Id);
            conditionListBox.Items.Add(new NodeViewItem(condition, EventGraphPresentationService.SummarizeStructuredNode(_localization, condition), true));
        }
        foreach (var condition in EventGraphAnalysisService.GetDetachedOwnedNodes(_selectedGraph!, _selectedTrigger, NodeKinds.Condition, connectedConditionIds))
        {
            conditionListBox.Items.Add(new NodeViewItem(condition, EventGraphPresentationService.SummarizeStructuredNode(_localization, condition), false));
        }

        actionListBox.Items.Clear();
        var connectedActionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in view.Actions)
        {
            connectedActionIds.Add(action.Id);
            actionListBox.Items.Add(new NodeViewItem(action, EventGraphPresentationService.SummarizeStructuredNode(_localization, action), true));
        }
        foreach (var action in EventGraphAnalysisService.GetDetachedOwnedNodes(_selectedGraph!, _selectedTrigger, NodeKinds.Action, connectedActionIds))
        {
            actionListBox.Items.Add(new NodeViewItem(action, EventGraphPresentationService.SummarizeStructuredNode(_localization, action), false));
        }

        var structuredEditable = !view.IsComplex;
        SetStructuredButtonsEnabled(structuredEditable);
    }

    private void SetStructuredButtonsEnabled(bool enabled)
    {
        var selectedCondition = conditionListBox.SelectedItem as NodeViewItem;
        var selectedAction = actionListBox.SelectedItem as NodeViewItem;
        var conditionEditable = enabled && selectedCondition is not null && selectedCondition.Connected;
        var actionEditable = enabled && selectedAction is not null && selectedAction.Connected;

        addEventButton.Enabled = enabled;
        editEventButton.Enabled = enabled;
        deleteEventButton.Enabled = enabled;
        addConditionButton.Enabled = enabled;
        editConditionButton.Enabled = conditionEditable;
        deleteConditionButton.Enabled = conditionEditable;
        moveConditionUpButton.Enabled = conditionEditable;
        moveConditionDownButton.Enabled = conditionEditable;
        addActionButton.Enabled = enabled;
        editActionButton.Enabled = actionEditable;
        deleteActionButton.Enabled = actionEditable;
        moveActionUpButton.Enabled = actionEditable;
        moveActionDownButton.Enabled = actionEditable;
    }

    private void UpdateStructuredButtonStates()
    {
        if (_selectedTrigger is null)
        {
            return;
        }

        var view = EventGraphAnalysisService.AnalyzeTrigger(_selectedGraph!, _selectedTrigger);
        SetStructuredButtonsEnabled(!view.IsComplex);
    }

    private void RefreshNodeGraphPanel()
    {
        if (_selectedTrigger is null)
        {
            return;
        }

        _selectedCanvasNode ??= _selectedTrigger;
        _connectFromSourceMenuItem.Enabled = _linkSourceNode is not null;
        nodeCanvasPanel.BindGraph(_selectedGraph, _selectedTrigger);
        nodeCanvasPanel.SelectedNodeId = _selectedCanvasNode?.Id ?? _selectedTrigger.Id;
        nodeCanvasPanel.LinkSourceNodeId = _linkSourceNode?.Id;
    }

    private void NodeCanvasPanel_SelectionChanged(object? sender, NodeSelectionChangedEventArgs e)
    {
        if (_selectedGraph is null)
        {
            return;
        }

        _selectedCanvasNode = string.IsNullOrWhiteSpace(e.NodeId)
            ? null
            : _selectedGraph.Nodes.FirstOrDefault(node => string.Equals(node.Id, e.NodeId, StringComparison.OrdinalIgnoreCase));
    }

    private void NodeCanvasPanel_GraphChanged(object? sender, EventArgs e)
    {
        AutoSaveEventGraph();
        if (_selectedCanvasNode is not null && _selectedCanvasNode.Kind == NodeKinds.Trigger)
        {
            UpdateTriggerTreeNodeText(_selectedCanvasNode);
        }

        RefreshNodeGraphPanel();
    }

    private void NodeCanvasPanel_NodeContextRequested(object? sender, NodeContextRequestedEventArgs e)
    {
        _canvasContextNodeId = e.NodeId;
        _canvasContextLocation = Point.Round(e.WorldLocation);
        _selectedCanvasNode = _selectedGraph?.Nodes.FirstOrDefault(node => string.Equals(node.Id, e.NodeId, StringComparison.OrdinalIgnoreCase));
        _connectFromSourceMenuItem.Enabled = _linkSourceNode is not null && !string.Equals(_linkSourceNode.Id, e.NodeId, StringComparison.OrdinalIgnoreCase);
        _nodeItemContextMenu.Show(nodeCanvasPanel, e.Location);
    }

    private void NodeCanvasPanel_CanvasContextRequested(object? sender, CanvasContextRequestedEventArgs e)
    {
        _canvasContextNodeId = null;
        _canvasContextLocation = Point.Round(e.WorldLocation);
        _nodeCanvasContextMenu.Show(nodeCanvasPanel, e.Location);
    }

    private void RefreshScriptPanel()
    {
        SaveScriptEditor(force: false);

        if (_selectedTrigger is null)
        {
            scriptPathTextBox.Text = string.Empty;
            scriptCodeRichTextBox.Text = string.Empty;
            _loadedScriptTriggerId = null;
            _scriptEditorDirty = false;
            return;
        }

        scriptPathTextBox.Text = GetParameter(_selectedTrigger.Parameters, "scriptPath", string.Empty);
        scriptHelpLabel.Text = _localization.Format("graph.summary.scriptPath", string.IsNullOrWhiteSpace(scriptPathTextBox.Text) ? _localization.T("graph.summary.noScript") : scriptPathTextBox.Text);

        var scriptText = string.Empty;
        var filePath = EventGraphScriptService.ResolveScriptPath(_context, scriptPathTextBox.Text);
        if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
        {
            scriptText = File.ReadAllText(filePath, Encoding.UTF8);
        }

        _isLoadingScriptEditor = true;
        try
        {
            scriptCodeRichTextBox.Text = EventGraphScriptService.BuildScriptEditorText(scriptPathTextBox.Text, scriptText);
            ApplyScriptHighlight();
            _loadedScriptTriggerId = _selectedTrigger.Id;
            _scriptEditorDirty = false;
        }
        finally
        {
            _isLoadingScriptEditor = false;
        }
    }

    private void RebuildNodeCanvas()
    {
        nodeCanvasPanel.SuspendLayout();
        nodeCanvasPanel.Controls.Clear();
        _nodeCardControls.Clear();
        _nodeCanvasRootTriggerId = _selectedTrigger?.Id;

        foreach (var node in EventGraphAnalysisService.GetReachableNodes(_selectedGraph!, _selectedTrigger))
        {
            var card = new GraphNodeCardControl(_localization, node, OnNodeCardChanged, selectedNode => SelectCanvasNode(selectedNode.Id))
            {
                Left = Math.Max(16, node.X),
                Top = Math.Max(16, node.Y),
                ContextMenuStrip = _nodeItemContextMenu,
                Tag = node.Id
            };
            WireCanvasCard(card, node.Id);
            _nodeCardControls[node.Id] = card;
            nodeCanvasPanel.Controls.Add(card);
        }

        UpdateCanvasScrollBounds();
        RefreshNodeCanvasSelection();
        nodeCanvasPanel.ResumeLayout();
        nodeCanvasPanel.Invalidate();
    }

    private void RefreshNodeCanvasSelection()
    {
        foreach (var card in _nodeCardControls.Values)
        {
            card.Selected = _selectedCanvasNode is not null && string.Equals(card.Node.Id, _selectedCanvasNode.Id, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void OnNodeCardChanged(GraphNodeDefinition node)
    {
        if (_selectedGraph is null)
        {
            return;
        }

        if (node.Kind == NodeKinds.Trigger)
        {
            UpdateTriggerTreeNodeText(node);
        }

        _nodeCanvasRootTriggerId = _selectedTrigger?.Id;
        UpdateCanvasScrollBounds();
        AutoSaveEventGraph();
        nodeCanvasPanel.Invalidate();
    }

    private void UpdateTriggerTreeNodeText(GraphNodeDefinition trigger)
    {
        if (triggerTreeView.Nodes.Count == 0)
        {
            return;
        }

        var rootNode = triggerTreeView.Nodes[0];
        var triggerNode = FindTriggerTreeNode(rootNode, trigger.Id);
        if (triggerNode is not null)
        {
            triggerNode.Text = CreateTriggerTreeText(trigger);
        }
    }

    private void WireCanvasCard(Control control, string nodeId)
    {
        control.MouseUp += (_, e) => CanvasCard_MouseUp(nodeId, e);
        foreach (Control child in control.Controls)
        {
            WireCanvasCard(child, nodeId);
        }
    }

    private void CanvasCard_MouseUp(string nodeId, MouseEventArgs e)
    {
        _canvasContextNodeId = nodeId;
        if (e.Button == MouseButtons.Left)
        {
            SelectCanvasNode(nodeId);
        }
    }

    private Point GetCardSocketLocation(string nodeId, string direction)
    {
        if (_nodeCardControls.TryGetValue(nodeId, out var card))
        {
            var socket = card.GetSocketCenter(direction);
            var scroll = nodeCanvasPanel.AutoScrollPosition;
            return new Point(card.Left + socket.X + scroll.X, card.Top + socket.Y + scroll.Y);
        }

        var fallback = _selectedGraph?.Nodes.FirstOrDefault(node => node.Id == nodeId);
        if (fallback is null)
        {
            return Point.Empty;
        }

        var socketFallback = GraphNodeCardControl.GetSocketCenter(
            new Size(GraphNodeCardControl.CardWidth, GraphNodeCardControl.CardWidth),
            GraphNodeCardControl.CardPadding,
            direction);
        var scrollFallback = nodeCanvasPanel.AutoScrollPosition;
        return new Point(fallback.X + socketFallback.X + scrollFallback.X, fallback.Y + socketFallback.Y + scrollFallback.Y);
    }

    private void UpdateCanvasScrollBounds()
    {
        if (_selectedGraph is null || _nodeCardControls.Count == 0)
        {
            nodeCanvasPanel.AutoScrollMinSize = Size.Empty;
            return;
        }

        var maxRight = 0;
        var maxBottom = 0;
        foreach (var card in _nodeCardControls.Values)
        {
            maxRight = Math.Max(maxRight, card.Left + card.Width);
            maxBottom = Math.Max(maxBottom, card.Top + card.Height);
        }

        nodeCanvasPanel.AutoScrollMinSize = new Size(maxRight + 120, maxBottom + 120);
    }

    private void NodeCanvasPanel_Paint(object? sender, PaintEventArgs e)
    {
        if (_selectedGraph is null || _selectedTrigger is null)
        {
            return;
        }

        using var pen = new Pen(Color.DimGray, 2F);
        var visibleNodes = EventGraphAnalysisService.GetReachableNodes(_selectedGraph!, _selectedTrigger).ToDictionary(node => node.Id, node => node, StringComparer.OrdinalIgnoreCase);

        foreach (var edge in _selectedGraph.Edges.Where(edge => visibleNodes.ContainsKey(edge.FromNodeId) && visibleNodes.ContainsKey(edge.ToNodeId)))
        {
            var from = visibleNodes[edge.FromNodeId];
            var to = visibleNodes[edge.ToNodeId];
            var start = GetCardSocketLocation(from.Id, NodePortDirections.Output);
            var end = GetCardSocketLocation(to.Id, NodePortDirections.Input);
            e.Graphics.DrawLine(pen, start, end);
        }
    }

    private void NodeCanvasPanel_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        _canvasContextNodeId = null;
        _canvasContextLocation = new Point(e.X - nodeCanvasPanel.AutoScrollPosition.X, e.Y - nodeCanvasPanel.AutoScrollPosition.Y);
    }

    private void CanvasNode_MouseUp(object? sender, MouseEventArgs e)
    {
        if (sender is not Control control)
        {
            return;
        }

        if (control.Tag is string nodeId)
        {
            SelectCanvasNode(nodeId);
            _canvasContextNodeId = nodeId;
            _connectFromSourceMenuItem.Enabled = _linkSourceNode is not null && !string.Equals(_linkSourceNode.Id, nodeId, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void SelectCanvasNode(string nodeId)
    {
        _selectedCanvasNode = _selectedGraph?.Nodes.FirstOrDefault(node => node.Id == nodeId);
        RefreshNodeCanvasSelection();
    }

    private void CreateTrigger(bool scriptMode)
    {
        CreateTrigger(scriptMode ? TriggerModeScript : TriggerModeStructured);
    }

    private void CreateTrigger(string mode)
    {
        if (_selectedGraph is null)
        {
            return;
        }

        mode = mode switch
        {
            TriggerModeNodeGraph => TriggerModeNodeGraph,
            TriggerModeScript => TriggerModeScript,
            _ => TriggerModeStructured
        };

        var next = GetNextNodeSequence();
        var node = new GraphNodeDefinition
        {
            Id = $"node.{next}",
            Kind = NodeKinds.Trigger,
            Title = mode == TriggerModeScript
                ? _localization.Format("graph.template.scriptTriggerName", next)
                : _localization.Format("graph.template.triggerDefaultName", next),
            X = 80,
            Y = 60,
            Parameters = EventGraphPresentationService.CreateDefaultTriggerParameters(TriggerModeStructured)
        };

        if (mode == TriggerModeScript)
        {
            EventGraphScriptService.EnsureScriptForTrigger(_context, _selectedGraph, node);
        }

        _selectedGraph.Nodes.Add(node);
        _selectedTrigger = node;
        AutoSaveEventGraph();
        RefreshTriggerList(node.Id);
    }

    private void CreatePresetTrigger(string presetKey)
    {
        if (_selectedGraph is null)
        {
            return;
        }

        var preset = GetPresetTriggerSpec(presetKey);
        if (preset is null)
        {
            return;
        }

        var next = GetNextNodeSequence();
        var trigger = new GraphNodeDefinition
        {
            Id = $"node.{next++}",
            Kind = NodeKinds.Trigger,
            Title = _localization.T(preset.TitleKey),
            X = 80,
            Y = 60,
            Parameters = EventGraphPresentationService.CreateDefaultTriggerParameters(TriggerModeStructured, preset.EventType, preset.Subject)
        };
        trigger.Parameters["preset"] = preset.Key;
        trigger.Parameters["mode"] = TriggerModeStructured;
        preset.ConfigureRoot?.Invoke(trigger.Parameters);

        var generatedNodes = new List<GraphNodeDefinition>();
        foreach (var action in preset.Actions)
        {
            generatedNodes.Add(CreatePresetActionNode(action, next++, trigger.Id));
        }

        _selectedGraph.Nodes.Add(trigger);
        foreach (var node in generatedNodes)
        {
            _selectedGraph.Nodes.Add(node);
        }

        EventGraphAnalysisService.RebuildStructuredEdges(_selectedGraph!, trigger, [], generatedNodes);
        _selectedTrigger = trigger;
        AutoSaveEventGraph();
        RefreshTriggerList(trigger.Id);
    }

    private TriggerPresetSpec? GetPresetTriggerSpec(string presetKey)
    {
        return presetKey switch
        {
            "teleport" => new TriggerPresetSpec(
                "teleport",
                "graph.template.teleportTriggerName",
                "OnEnterArea",
                "player",
                parameters =>
                {
                    parameters["areaSource"] = "self";
                    parameters["shape"] = "box";
                    parameters["width"] = "96";
                    parameters["height"] = "96";
                    parameters["once"] = "false";
                    parameters["runOnce"] = "false";
                    parameters["detail"] = _localization.T("graph.preset.teleport.detail");
                },
                [
                    new PresetActionSpec("changeMap", "graph.template.changeMap", "graph.preset.teleport.action")
                ]),
            "chest" => new TriggerPresetSpec(
                "chest",
                "graph.template.chestTriggerName",
                "OnInteract",
                "player",
                parameters =>
                {
                    parameters["once"] = "true";
                    parameters["runOnce"] = "true";
                    parameters["detail"] = _localization.T("graph.preset.chest.detail");
                },
                [
                    new PresetActionSpec("animation", "graph.template.animation", "graph.preset.chest.animation"),
                    new PresetActionSpec("giveItem", "graph.template.giveItem", "graph.preset.chest.giveItem")
                ]),
            "keyInteract" => new TriggerPresetSpec(
                "keyInteract",
                "graph.template.keyInteractTriggerName",
                "OnInteract",
                "player",
                parameters =>
                {
                    parameters["once"] = "false";
                    parameters["runOnce"] = "false";
                    parameters["detail"] = _localization.T("graph.preset.keyInteract.detail");
                },
                [
                    new PresetActionSpec("dialogue", "graph.template.dialogue", "graph.preset.keyInteract.dialogue"),
                    new PresetActionSpec("animation", "graph.template.animation", "graph.preset.keyInteract.animation")
                ]),
            _ => null
        };
    }

    private GraphNodeDefinition CreatePresetActionNode(PresetActionSpec action, int sequence, string ownerTriggerId)
    {
        return new GraphNodeDefinition
        {
            Id = $"node.{sequence}",
            Kind = NodeKinds.Action,
            Title = _localization.T(action.TitleKey),
            X = 0,
            Y = 0,
            Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["template"] = action.Template,
                ["detail"] = _localization.T(action.DetailKey),
                [TriggerOwnerParameterKey] = ownerTriggerId
            }
        };
    }

    private void DuplicateSelectedTrigger()
    {
        if (_selectedGraph is null || _selectedTrigger is null)
        {
            return;
        }

        SaveStructuredTriggerSilently();
        var sequence = GetNextNodeSequence();
        var cloneMap = new Dictionary<string, GraphNodeDefinition>(StringComparer.OrdinalIgnoreCase);
        var cloneTrigger = CloneNode(_selectedTrigger, $"node.{sequence++}");
        cloneTrigger.Title = _localization.Format("graph.template.copyName", string.IsNullOrWhiteSpace(_selectedTrigger.Title) ? _selectedTrigger.Id : _selectedTrigger.Title);
        cloneTrigger.X += 36;
        cloneTrigger.Y += 36;
        cloneMap[_selectedTrigger.Id] = cloneTrigger;
        _selectedGraph.Nodes.Add(cloneTrigger);

        foreach (var node in EventGraphAnalysisService.GetReachableNodes(_selectedGraph!, _selectedTrigger).Where(node => node.Id != _selectedTrigger.Id))
        {
            var clone = CloneNode(node, $"node.{sequence++}");
            clone.X += 36;
            clone.Y += 36;
            EventGraphAnalysisService.SetNodeOwner(clone, cloneTrigger.Id);
            cloneMap[node.Id] = clone;
            _selectedGraph.Nodes.Add(clone);
        }

        foreach (var edge in _selectedGraph.Edges.Where(edge => cloneMap.ContainsKey(edge.FromNodeId) && cloneMap.ContainsKey(edge.ToNodeId)).ToList())
        {
            _selectedGraph.Edges.Add(new GraphEdgeDefinition
            {
                FromNodeId = cloneMap[edge.FromNodeId].Id,
                ToNodeId = cloneMap[edge.ToNodeId].Id,
                FromPort = edge.FromPort,
                ToPort = edge.ToPort,
                ValueType = edge.ValueType
            });
        }

        if (GetTriggerMode(cloneTrigger) == TriggerModeScript)
        {
            cloneTrigger.Parameters.Remove("scriptPath");
            EventGraphScriptService.EnsureScriptForTrigger(_context, _selectedGraph, cloneTrigger);
        }

        _selectedTrigger = cloneTrigger;
        AutoSaveEventGraph();
        RefreshTriggerList(cloneTrigger.Id);
    }

    private static GraphNodeDefinition CloneNode(GraphNodeDefinition source, string id)
    {
        return new GraphNodeDefinition
        {
            Id = id,
            Kind = source.Kind,
            Title = source.Title,
            X = source.X,
            Y = source.Y,
            Parameters = new Dictionary<string, string>(source.Parameters, StringComparer.OrdinalIgnoreCase)
        };
    }

    private void DeleteSelectedTrigger()
    {
        if (_selectedGraph is null || _selectedTrigger is null)
        {
            return;
        }

        var reachableIds = EventGraphAnalysisService.GetReachableNodes(_selectedGraph!, _selectedTrigger).Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var owned in _selectedGraph.Nodes.Where(node =>
                     node.Kind != NodeKinds.Trigger &&
                     string.Equals(EventGraphAnalysisService.GetNodeOwner(node), _selectedTrigger.Id, StringComparison.OrdinalIgnoreCase)))
        {
            reachableIds.Add(owned.Id);
        }
        _selectedGraph.Nodes.RemoveAll(node => reachableIds.Contains(node.Id));
        _selectedGraph.Edges.RemoveAll(edge => reachableIds.Contains(edge.FromNodeId) || reachableIds.Contains(edge.ToNodeId));
        _selectedTrigger = null;
        _selectedCanvasNode = null;
        _linkSourceNode = null;
        AutoSaveEventGraph();
        RefreshTriggerList();
    }

    private void ToggleSelectedTriggerEnabled()
    {
        if (_selectedTrigger is null)
        {
            return;
        }

        SaveStructuredTriggerSilently();
        _selectedTrigger.Parameters["enabled"] = IsTriggerEnabled(_selectedTrigger) ? "false" : "true";
        AutoSaveEventGraph();
        RefreshTriggerList(_selectedTrigger.Id);
    }

    private void SetSelectedTriggerMode(string mode)
    {
        if (_selectedTrigger is null)
        {
            return;
        }

        PersistPendingEditorChanges();
        _selectedTrigger.Parameters["mode"] = mode;
        if (mode == TriggerModeScript)
        {
            EventGraphScriptService.EnsureScriptForTrigger(_context, _selectedGraph, _selectedTrigger);
        }
        AutoSaveEventGraph();
        RefreshTriggerList(_selectedTrigger.Id);
    }

    private void ConvertSelectedTriggerToScript()
    {
        if (_selectedTrigger is null)
        {
            return;
        }

        PersistPendingEditorChanges();
        _selectedTrigger.Parameters["mode"] = TriggerModeScript;
            EventGraphScriptService.EnsureScriptForTrigger(_context, _selectedGraph, _selectedTrigger);
        AutoSaveEventGraph();
        RefreshTriggerList(_selectedTrigger.Id);
    }

    private void AddOrEditEvent(bool editExisting)
    {
        EventGraphStructuredEditingService.AddOrEditEvent(_localization, _selectedTrigger, editExisting, AutoSaveEventGraph, RefreshRightPane, RefreshTriggerList);
    }

    private void DeleteEvent()
    {
        EventGraphStructuredEditingService.DeleteEvent(_selectedTrigger, AutoSaveEventGraph, RefreshRightPane);
    }

    private void AddStructuredNode(string kind)
    {
        EventGraphStructuredEditingService.AddStructuredNode(_localization, _selectedGraph, _selectedTrigger, kind, GetNextNodeSequence, ShowWarning, AutoSaveEventGraph, RefreshRightPane);
    }

    private void EditStructuredNode(string kind)
    {
        var selectedNode = kind == NodeKinds.Condition
            ? (conditionListBox.SelectedItem as NodeViewItem)?.Node
            : (actionListBox.SelectedItem as NodeViewItem)?.Node;
        EventGraphStructuredEditingService.EditStructuredNode(_localization, _selectedGraph, _selectedTrigger, selectedNode, AutoSaveEventGraph, RefreshRightPane);
    }

    private void DeleteStructuredNode(string kind)
    {
        var selectedNode = kind == NodeKinds.Condition
            ? (conditionListBox.SelectedItem as NodeViewItem)?.Node
            : (actionListBox.SelectedItem as NodeViewItem)?.Node;
        EventGraphStructuredEditingService.DeleteStructuredNode(_localization, _selectedGraph, _selectedTrigger, kind, selectedNode, ShowWarning, AutoSaveEventGraph, RefreshRightPane);
    }

    private void MoveStructuredNode(string kind, int offset)
    {
        var selectedNode = kind == NodeKinds.Condition
            ? (conditionListBox.SelectedItem as NodeViewItem)?.Node
            : (actionListBox.SelectedItem as NodeViewItem)?.Node;
        var movedNodeId = EventGraphStructuredEditingService.MoveStructuredNode(_localization, _selectedGraph, _selectedTrigger, kind, offset, selectedNode, ShowWarning, AutoSaveEventGraph, RefreshRightPane);
        if (movedNodeId is null)
        {
            return;
        }

        var listBox = kind == NodeKinds.Condition ? conditionListBox : actionListBox;
        for (var i = 0; i < listBox.Items.Count; i++)
        {
            if (listBox.Items[i] is NodeViewItem item && item.Node.Id == movedNodeId)
            {
                listBox.SelectedIndex = i;
                break;
            }
        }
    }

    private void SaveStructuredTrigger()
    {
        SaveStructuredTriggerSilently();
        AutoSaveEventGraph();
    }

    private void SaveStructuredTriggerSilently(bool refreshList = true)
    {
        if (_selectedTrigger is null || _isSavingStructuredTrigger)
        {
            return;
        }

        _isSavingStructuredTrigger = true;
        try
        {
            EventGraphEditorStateService.SaveStructuredTriggerState(
                _selectedTrigger,
                triggerNameTextBox.Text,
                GetTriggerMode,
                IsTriggerEnabled);
        }
        finally
        {
            _isSavingStructuredTrigger = false;
        }

        if (refreshList)
        {
            RefreshTriggerList(_selectedTrigger.Id);
        }
    }

    private void SaveCanvasNode()
    {
        EventGraphCanvasEditingService.SaveCanvasNode(_selectedCanvasNode, nodeTitleTextBox.Text.Trim(), (int)nodeXNumericUpDown.Value, (int)nodeYNumericUpDown.Value, nodeParametersTextBox.Text, AutoSaveEventGraph, RefreshRightPane, RefreshTriggerList);
    }

    private void CreateNodeAtCanvas(string kind)
    {
        if (_selectedGraph is null || _selectedTrigger is null)
        {
            return;
        }

        using var dialog = new TemplateNodeDialog(_localization, kind);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var node = new GraphNodeDefinition
        {
            Id = $"node.{GetNextNodeSequence()}",
            Kind = kind,
            Title = dialog.NodeTitle,
            X = Math.Max(16, _canvasContextLocation.X),
            Y = Math.Max(16, _canvasContextLocation.Y),
            Parameters = dialog.BuildParameters()
        };
        EventGraphAnalysisService.SetNodeOwner(node, _selectedTrigger.Id);

        _selectedGraph.Nodes.Add(node);
        _selectedCanvasNode = node;
        AutoSaveEventGraph();
        RefreshRightPane();
    }

    private void SetCanvasLinkSource()
    {
        EventGraphCanvasEditingService.SetCanvasLinkSource(_selectedCanvasNode, node => _linkSourceNode = node, RefreshNodeGraphPanel);
    }

    private void ConnectCanvasNodeFromSource()
    {
        EventGraphCanvasEditingService.ConnectCanvasNodeFromSource(_localization, _selectedGraph, _linkSourceNode, _canvasContextNodeId, InferPortValueType, node => _selectedCanvasNode = node, ShowWarning, AutoSaveEventGraph, RefreshRightPane);
    }

    private void DeleteCanvasNode()
    {
        EventGraphCanvasEditingService.DeleteCanvasNode(_localization, _selectedGraph, _selectedTrigger, _canvasContextNodeId, ref _selectedCanvasNode, ref _linkSourceNode, ShowWarning, AutoSaveEventGraph, RefreshRightPane);
    }

    private void SaveProject()
    {
        SaveStructuredTriggerSilently();
        SaveScriptEditor(force: false);
        if (_selectedGraph is not null)
        {
            var validation = EventGraphSaveService.ValidateGraph(_selectedGraph, _localization.T, (key, arg) => _localization.Format(key, arg));
            if (validation is not null)
            {
                ShowWarning(validation);
                return;
            }
        }

        EventGraphSaveService.SaveProject(_context, _projectService);
    }

    private void ScriptCodeRichTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (_isLoadingScriptEditor || _isApplyingScriptHighlight)
        {
            return;
        }

        _scriptEditorDirty = true;
        ApplyScriptHighlight();
    }

    private void SaveScriptEditor(bool force)
    {
        var targetTrigger = ResolveLoadedScriptTrigger();
        if (targetTrigger is null || GetTriggerMode(targetTrigger) != TriggerModeScript)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_loadedScriptTriggerId) && !_scriptEditorDirty)
        {
            return;
        }

        if (!_scriptEditorDirty && !force)
        {
            return;
        }

        var scriptPath = GetParameter(targetTrigger.Parameters, "scriptPath", string.Empty);
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
        EventGraphScriptService.EnsureScriptForTrigger(_context, _selectedGraph, targetTrigger);
            scriptPath = GetParameter(targetTrigger.Parameters, "scriptPath", string.Empty);
            if (ReferenceEquals(targetTrigger, _selectedTrigger))
            {
                scriptPathTextBox.Text = scriptPath;
            }
        }

        var filePath = EventGraphScriptService.ResolveScriptPath(_context, scriptPath);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var parentDirectory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }

        File.WriteAllText(filePath, EventGraphScriptService.ExtractScriptBody(scriptCodeRichTextBox.Text), Encoding.UTF8);
        _scriptEditorDirty = false;
        scriptHelpLabel.Text = _localization.Format("graph.summary.scriptPath", scriptPath);
        AutoSaveEventGraph();
    }

    private GraphNodeDefinition? ResolveLoadedScriptTrigger()
    {
        if (_selectedGraph is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(_loadedScriptTriggerId))
        {
            return _selectedGraph.Nodes.FirstOrDefault(node => string.Equals(node.Id, _loadedScriptTriggerId, StringComparison.OrdinalIgnoreCase));
        }

        return _selectedTrigger;
    }

    private void AutoSaveEventGraph()
    {
        _projectService.SaveProject(_context);
    }

    private void PersistPendingEditorChanges(bool force = false)
    {
        SaveStructuredTriggerSilently(refreshList: false);
        SaveScriptEditor(force: force);
    }

    private void EventGraphEditorForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        try
        {
            PersistPendingEditorChanges(force: true);
        }
        catch (Exception ex)
        {
            e.Cancel = true;
            ShowWarning($"{_localization.T("dialog.saveProjectFailed")}: {ex.Message}");
        }
    }

    private void ApplyScriptHighlight()
    {
        if (_isApplyingScriptHighlight)
        {
            return;
        }

        _isApplyingScriptHighlight = true;
        try
        {
            var selectionStart = scriptCodeRichTextBox.SelectionStart;
            var selectionLength = scriptCodeRichTextBox.SelectionLength;
            var firstVisibleLine = SendMessage(scriptCodeRichTextBox.Handle, EmGetFirstVisibleLine, 0, 0);

            SendMessage(scriptCodeRichTextBox.Handle, WmSetRedraw, 0, 0);
            try
            {
                scriptCodeRichTextBox.SuspendLayout();
                scriptCodeRichTextBox.SelectAll();
                scriptCodeRichTextBox.SelectionColor = Color.Black;

                var text = scriptCodeRichTextBox.Text;
                HighlightRegexMatches(text, @"^// 脚本路径: .*$", Color.FromArgb(128, 128, 128), RegexOptions.Multiline);
                HighlightRegexMatches(text, @"\b(export|function|const|let|var|if|else|for|while|return|switch|case|break|continue|new|class|extends|import|from|try|catch|finally|throw|typeof|instanceof|async|await)\b", Color.FromArgb(0, 102, 204));
                HighlightRegexMatches(text, @"(//.*?$)|(/\*[\s\S]*?\*/)", Color.FromArgb(0, 128, 0), RegexOptions.Multiline);
                HighlightRegexMatches(text, "\"([^\"\\\\]|\\\\.)*\"|'([^'\\\\]|\\\\.)*'", Color.FromArgb(163, 21, 21));

                scriptCodeRichTextBox.SelectionStart = selectionStart;
                scriptCodeRichTextBox.SelectionLength = selectionLength;
                var currentVisibleLine = SendMessage(scriptCodeRichTextBox.Handle, EmGetFirstVisibleLine, 0, 0);
                SendMessage(scriptCodeRichTextBox.Handle, EmLineScroll, 0, firstVisibleLine - currentVisibleLine);
                scriptCodeRichTextBox.SelectionColor = Color.Black;
            }
            finally
            {
                scriptCodeRichTextBox.ResumeLayout();
                SendMessage(scriptCodeRichTextBox.Handle, WmSetRedraw, 1, 0);
                scriptCodeRichTextBox.Invalidate();
                scriptCodeRichTextBox.Update();
            }
        }
        finally
        {
            _isApplyingScriptHighlight = false;
        }
    }

    private void HighlightRegexMatches(string text, string pattern, Color color, RegexOptions options = RegexOptions.None)
    {
        foreach (Match match in Regex.Matches(text, pattern, options | RegexOptions.CultureInvariant))
        {
            scriptCodeRichTextBox.Select(match.Index, match.Length);
            scriptCodeRichTextBox.SelectionColor = color;
        }
    }

    private void CheckScriptFormat()
    {
        var scriptBody = EventGraphScriptService.ExtractScriptBody(scriptCodeRichTextBox.Text);
        var validationMessage = EventGraphScriptService.ValidateScriptFormat(scriptBody);
        if (validationMessage is null)
        {
            ShowInfo(_localization.T("graph.info.scriptFormatOk"));
            return;
        }

        ShowWarning(_localization.Format("graph.error.scriptFormatInvalid", validationMessage));
    }

    private const int EmGetFirstVisibleLine = 0x00CE;
    private const int EmLineScroll = 0x00B6;
    private const int WmSetRedraw = 0x000B;

    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    private void EventGraphEditorForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.Shift && e.KeyCode == Keys.N)
        {
            CreateTrigger(true);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.N)
        {
            CreateTrigger(false);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D)
        {
            DuplicateSelectedTrigger();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Delete)
        {
            if (GetTriggerMode(_selectedTrigger) == TriggerModeNodeGraph && _selectedCanvasNode is not null && _selectedCanvasNode.Kind != NodeKinds.Trigger)
            {
                _canvasContextNodeId = _selectedCanvasNode.Id;
                DeleteCanvasNode();
            }
            else
            {
                DeleteSelectedTrigger();
            }
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.E)
        {
            ToggleSelectedTriggerEnabled();
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D1)
        {
            SetSelectedTriggerMode(TriggerModeStructured);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D2)
        {
            SetSelectedTriggerMode(TriggerModeNodeGraph);
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D3)
        {
            ConvertSelectedTriggerToScript();
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.S)
        {
            SaveProject();
            e.Handled = true;
        }
    }

    private int GetNextNodeSequence()
    {
        if (_selectedGraph is null)
        {
            return 1;
        }

        var max = 0;
        foreach (var node in _selectedGraph.Nodes)
        {
            if (node.Id.StartsWith("node.", StringComparison.Ordinal) && int.TryParse(node.Id[5..], out var value))
            {
                max = Math.Max(max, value);
            }
        }

        return max + 1;
    }

    private static string GetParameter(Dictionary<string, string> parameters, string key, string fallback)
    {
        return parameters.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static string GetTriggerMode(GraphNodeDefinition? node)
    {
        return node is null ? TriggerModeStructured : GetParameter(node.Parameters, "mode", TriggerModeStructured);
    }

    private static bool IsTriggerEnabled(GraphNodeDefinition node)
    {
        return string.Equals(GetParameter(node.Parameters, "enabled", "true"), "true", StringComparison.OrdinalIgnoreCase);
    }

    private string GetTriggerModeLabel(string mode)
    {
        return mode switch
        {
            TriggerModeNodeGraph => _localization.T("graph.mode.nodeGraph"),
            TriggerModeScript => _localization.T("graph.mode.script"),
            _ => _localization.T("graph.mode.structured")
        };
    }

    private static decimal ClampToNumeric(NumericUpDown numeric, string value)
    {
        return decimal.TryParse(value, out var parsed)
            ? Math.Max(numeric.Minimum, Math.Min(numeric.Maximum, parsed))
            : numeric.Value;
    }

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

    private static Color GetNodeBackColor(GraphNodeDefinition node)
    {
        return node.Kind switch
        {
            NodeKinds.Trigger => Color.FromArgb(255, 239, 207),
            NodeKinds.Condition => Color.FromArgb(224, 239, 255),
            NodeKinds.Action => Color.FromArgb(230, 250, 230),
            _ => Color.White
        };
    }

    private string InferPortValueType(GraphNodeDefinition node, bool output)
    {
        if (node.Parameters.TryGetValue(output ? "outputType" : "inputType", out var explicitType) && !string.IsNullOrWhiteSpace(explicitType))
        {
            return explicitType;
        }

        return NodePortValueTypes.Flow;
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ShowInfo(string message)
    {
        MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SetListBoxStyle(ListBox listBox)
    {
        listBox.IntegralHeight = false;
        listBox.ItemHeight = Math.Max(listBox.ItemHeight, Math.Max(28, TextRenderer.MeasureText("事件图", Font).Height + 10));
    }

    private void SetStructuredListRendering(ListBox listBox)
    {
        listBox.DrawMode = DrawMode.OwnerDrawFixed;
        listBox.DrawItem -= StructuredListBox_DrawItem;
        listBox.DrawItem += StructuredListBox_DrawItem;
    }

    private void StructuredListBox_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ListBox listBox || e.Index < 0 || e.Index >= listBox.Items.Count)
        {
            return;
        }

        e.DrawBackground();

        var item = listBox.Items[e.Index];
        var text = item.ToString() ?? string.Empty;
        var isConnected = item is NodeViewItem nodeItem ? nodeItem.Connected : true;
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        var textColor = selected
            ? SystemColors.HighlightText
            : isConnected
                ? listBox.ForeColor
                : Color.FromArgb(150, 150, 150);

        using var brush = new SolidBrush(textColor);
        var bounds = new RectangleF(e.Bounds.X + 6, e.Bounds.Y + 3, e.Bounds.Width - 12, e.Bounds.Height - 6);
        e.Graphics.DrawString(text, Font, brush, bounds, StringFormat.GenericDefault);
        e.DrawFocusRectangle();
    }

    private sealed class TriggerAnalysis
    {
        public bool IsComplex { get; set; }
        public List<GraphNodeDefinition> Conditions { get; } = [];
        public List<GraphNodeDefinition> Actions { get; } = [];
    }

    private sealed record NodeViewItem(GraphNodeDefinition Node, string Text, bool Connected)
    {
        public override string ToString() => Connected ? Text : $"{Text}（未连接）";
    }

    private sealed record StringViewItem(string Text)
    {
        public override string ToString() => Text;
    }

    private sealed record TriggerTreeNodeTag(GraphNodeDefinition Trigger, GraphNodeDefinition? Node);

    private sealed record TriggerPresetSpec(
        string Key,
        string TitleKey,
        string EventType,
        string Subject,
        Action<Dictionary<string, string>>? ConfigureRoot,
        IReadOnlyList<PresetActionSpec> Actions);

    private sealed record PresetActionSpec(string Template, string TitleKey, string DetailKey);

}
