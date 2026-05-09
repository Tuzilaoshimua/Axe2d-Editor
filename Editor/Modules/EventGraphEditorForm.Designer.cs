#nullable disable
using Axe2DEditor.Editor.Controls;
namespace Axe2DEditor.Editor.Modules;

partial class EventGraphEditorForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        rootLayout = new TableLayoutPanel();
        mainToolStrip = new ToolStrip();
        graphToolStripLabel = new ToolStripLabel();
        graphToolStripHost = new ComboBox();
        newTriggerToolStripButton = new ToolStripButton();
        newScriptTriggerToolStripButton = new ToolStripButton();
        duplicateTriggerToolStripButton = new ToolStripButton();
        removeTriggerToolStripButton = new ToolStripButton();
        toggleEnabledToolStripButton = new ToolStripButton();
        structuredModeToolStripButton = new ToolStripButton();
        nodeGraphModeToolStripButton = new ToolStripButton();
        convertToScriptToolStripButton = new ToolStripButton();
        saveProjectToolStripButton = new ToolStripButton();
        workspaceSplitContainer = new SplitContainer();
        triggerListLayout = new TableLayoutPanel();
        triggerListTitleLabel = new Label();
        triggerTreeView = new TreeView();
        rightRootLayout = new TableLayoutPanel();
        triggerHeaderLayout = new TableLayoutPanel();
        triggerNameLabel = new Label();
        triggerNameTextBox = new TextBox();
        scriptFormatButton = new Button();
        modeHostPanel = new Panel();
        emptyStatePanel = new Panel();
        emptyStateLabel = new Label();
        structuredPanel = new TableLayoutPanel();
        structuredSummaryLabel = new Label();
        eventGroupBox = new GroupBox();
        eventGroupLayout = new TableLayoutPanel();
        eventListBox = new ListBox();
        eventButtonsPanel = new FlowLayoutPanel();
        addEventButton = new Button();
        editEventButton = new Button();
        deleteEventButton = new Button();
        conditionGroupBox = new GroupBox();
        conditionGroupLayout = new TableLayoutPanel();
        conditionListBox = new ListBox();
        conditionButtonsPanel = new FlowLayoutPanel();
        addConditionButton = new Button();
        editConditionButton = new Button();
        deleteConditionButton = new Button();
        moveConditionUpButton = new Button();
        moveConditionDownButton = new Button();
        actionGroupBox = new GroupBox();
        actionGroupLayout = new TableLayoutPanel();
        actionListBox = new ListBox();
        actionButtonsPanel = new FlowLayoutPanel();
        addActionButton = new Button();
        editActionButton = new Button();
        deleteActionButton = new Button();
        moveActionUpButton = new Button();
        moveActionDownButton = new Button();
        advancedGroupBox = new GroupBox();
        advancedParametersTextBox = new TextBox();
        structuredSaveButton = new Button();
        nodeGraphPanel = new TableLayoutPanel();
        nodeGraphHelpLabel = new Label();
        nodeCanvasPanel = new NodeGraphCanvasControl();
        nodeDetailsGroupBox = new GroupBox();
        nodeDetailsLayout = new TableLayoutPanel();
        nodeTypeLabel = new Label();
        nodeTypeValueLabel = new Label();
        nodeTitleLabel = new Label();
        nodeTitleTextBox = new TextBox();
        nodeXLabel = new Label();
        nodeXNumericUpDown = new NumericUpDown();
        nodeYLabel = new Label();
        nodeYNumericUpDown = new NumericUpDown();
        nodeParametersLabel = new Label();
        nodeParametersTextBox = new TextBox();
        nodeSaveButton = new Button();
        scriptPanel = new TableLayoutPanel();
        scriptCodeRichTextBox = new RichTextBox();
        scriptModeTitleLabel = new Label();
        scriptPathLabel = new Label();
        scriptPathTextBox = new TextBox();
        scriptHelpLabel = new Label();
        scriptSaveButton = new Button();
        rootLayout.SuspendLayout();
        mainToolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)workspaceSplitContainer).BeginInit();
        workspaceSplitContainer.Panel1.SuspendLayout();
        workspaceSplitContainer.Panel2.SuspendLayout();
        workspaceSplitContainer.SuspendLayout();
        triggerListLayout.SuspendLayout();
        rightRootLayout.SuspendLayout();
        triggerHeaderLayout.SuspendLayout();
        modeHostPanel.SuspendLayout();
        emptyStatePanel.SuspendLayout();
        structuredPanel.SuspendLayout();
        eventGroupBox.SuspendLayout();
        eventGroupLayout.SuspendLayout();
        eventButtonsPanel.SuspendLayout();
        conditionGroupBox.SuspendLayout();
        conditionGroupLayout.SuspendLayout();
        conditionButtonsPanel.SuspendLayout();
        actionGroupBox.SuspendLayout();
        actionGroupLayout.SuspendLayout();
        actionButtonsPanel.SuspendLayout();
        advancedGroupBox.SuspendLayout();
        nodeGraphPanel.SuspendLayout();
        nodeDetailsGroupBox.SuspendLayout();
        nodeDetailsLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nodeXNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)nodeYNumericUpDown).BeginInit();
        scriptPanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(mainToolStrip, 0, 0);
        rootLayout.Controls.Add(workspaceSplitContainer, 0, 1);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 2;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.Size = new Size(1680, 980);
        rootLayout.TabIndex = 0;
        // 
        // mainToolStrip
        // 
        mainToolStrip.Dock = DockStyle.Fill;
        mainToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        mainToolStrip.ImageScalingSize = new Size(20, 20);
        mainToolStrip.Items.AddRange(new ToolStripItem[] { graphToolStripLabel, newTriggerToolStripButton, newScriptTriggerToolStripButton, duplicateTriggerToolStripButton, removeTriggerToolStripButton, toggleEnabledToolStripButton, structuredModeToolStripButton, nodeGraphModeToolStripButton, convertToScriptToolStripButton, saveProjectToolStripButton });
        mainToolStrip.Location = new Point(0, 0);
        mainToolStrip.Name = "mainToolStrip";
        mainToolStrip.Padding = new Padding(12, 6, 12, 6);
        mainToolStrip.Size = new Size(1680, 46);
        mainToolStrip.TabIndex = 0;
        // 
        // graphToolStripLabel
        // 
        graphToolStripLabel.Name = "graphToolStripLabel";
        graphToolStripLabel.Size = new Size(0, 29);
        // 
        // graphToolStripHost
        // 
        graphToolStripHost.AccessibleName = "graphToolStripHost";
        graphToolStripHost.DropDownStyle = ComboBoxStyle.DropDownList;
        graphToolStripHost.FormattingEnabled = true;
        graphToolStripHost.Location = new Point(16, 7);
        graphToolStripHost.Name = "graphToolStripHost";
        graphToolStripHost.Size = new Size(280, 32);
        graphToolStripHost.TabIndex = 0;
        // 
        // graphToolStripHost
        // 
        graphToolStripHost.AutoSize = false;
        graphToolStripHost.Margin = new Padding(4, 0, 8, 0);
        graphToolStripHost.Name = "graphToolStripHost";
        graphToolStripHost.Size = new Size(280, 32);
        // 
        // newTriggerToolStripButton
        // 
        newTriggerToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        newTriggerToolStripButton.Name = "newTriggerToolStripButton";
        newTriggerToolStripButton.Size = new Size(34, 29);
        // 
        // newScriptTriggerToolStripButton
        // 
        newScriptTriggerToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        newScriptTriggerToolStripButton.Name = "newScriptTriggerToolStripButton";
        newScriptTriggerToolStripButton.Size = new Size(34, 29);
        // 
        // duplicateTriggerToolStripButton
        // 
        duplicateTriggerToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        duplicateTriggerToolStripButton.Name = "duplicateTriggerToolStripButton";
        duplicateTriggerToolStripButton.Size = new Size(34, 29);
        // 
        // removeTriggerToolStripButton
        // 
        removeTriggerToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        removeTriggerToolStripButton.Name = "removeTriggerToolStripButton";
        removeTriggerToolStripButton.Size = new Size(34, 29);
        // 
        // toggleEnabledToolStripButton
        // 
        toggleEnabledToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        toggleEnabledToolStripButton.Name = "toggleEnabledToolStripButton";
        toggleEnabledToolStripButton.Size = new Size(34, 29);
        // 
        // structuredModeToolStripButton
        // 
        structuredModeToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        structuredModeToolStripButton.Name = "structuredModeToolStripButton";
        structuredModeToolStripButton.Size = new Size(34, 29);
        // 
        // nodeGraphModeToolStripButton
        // 
        nodeGraphModeToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        nodeGraphModeToolStripButton.Name = "nodeGraphModeToolStripButton";
        nodeGraphModeToolStripButton.Size = new Size(34, 29);
        // 
        // convertToScriptToolStripButton
        // 
        convertToScriptToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        convertToScriptToolStripButton.Name = "convertToScriptToolStripButton";
        convertToScriptToolStripButton.Size = new Size(34, 29);
        // 
        // saveProjectToolStripButton
        // 
        saveProjectToolStripButton.Alignment = ToolStripItemAlignment.Right;
        saveProjectToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        saveProjectToolStripButton.Name = "saveProjectToolStripButton";
        saveProjectToolStripButton.Size = new Size(34, 29);
        // 
        // workspaceSplitContainer
        // 
        workspaceSplitContainer.Dock = DockStyle.Fill;
        workspaceSplitContainer.FixedPanel = FixedPanel.Panel1;
        workspaceSplitContainer.Location = new Point(16, 58);
        workspaceSplitContainer.Margin = new Padding(16, 12, 16, 16);
        workspaceSplitContainer.Name = "workspaceSplitContainer";
        // 
        // workspaceSplitContainer.Panel1
        // 
        workspaceSplitContainer.Panel1.Controls.Add(triggerListLayout);
        workspaceSplitContainer.Panel1MinSize = 0;
        // 
        // workspaceSplitContainer.Panel2
        // 
        workspaceSplitContainer.Panel2.Controls.Add(rightRootLayout);
        workspaceSplitContainer.Panel2MinSize = 0;
        workspaceSplitContainer.Size = new Size(1648, 906);
        workspaceSplitContainer.SplitterDistance = 320;
        workspaceSplitContainer.TabIndex = 1;
        // 
        // triggerListLayout
        // 
        triggerListLayout.ColumnCount = 1;
        triggerListLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        triggerListLayout.Controls.Add(triggerListTitleLabel, 0, 0);
        triggerListLayout.Controls.Add(triggerTreeView, 0, 1);
        triggerListLayout.Dock = DockStyle.Fill;
        triggerListLayout.Location = new Point(0, 0);
        triggerListLayout.Margin = new Padding(0);
        triggerListLayout.Name = "triggerListLayout";
        triggerListLayout.Padding = new Padding(12);
        triggerListLayout.RowCount = 2;
        triggerListLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        triggerListLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        triggerListLayout.Size = new Size(320, 906);
        triggerListLayout.TabIndex = 0;
        // 
        // triggerListTitleLabel
        // 
        triggerListTitleLabel.AutoEllipsis = true;
        triggerListTitleLabel.Dock = DockStyle.Fill;
        triggerListTitleLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        triggerListTitleLabel.Location = new Point(12, 12);
        triggerListTitleLabel.Margin = new Padding(0);
        triggerListTitleLabel.Name = "triggerListTitleLabel";
        triggerListTitleLabel.Size = new Size(296, 34);
        triggerListTitleLabel.TabIndex = 0;
        triggerListTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // triggerTreeView
        // 
        triggerTreeView.AllowDrop = true;
        triggerTreeView.Dock = DockStyle.Fill;
        triggerTreeView.HideSelection = false;
        triggerTreeView.Location = new Point(12, 54);
        triggerTreeView.Margin = new Padding(0, 8, 0, 0);
        triggerTreeView.Name = "triggerTreeView";
        triggerTreeView.Size = new Size(296, 840);
        triggerTreeView.TabIndex = 1;
        // 
        // rightRootLayout
        // 
        rightRootLayout.ColumnCount = 1;
        rightRootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rightRootLayout.Controls.Add(triggerHeaderLayout, 0, 0);
        rightRootLayout.Controls.Add(modeHostPanel, 0, 1);
        rightRootLayout.Dock = DockStyle.Fill;
        rightRootLayout.Location = new Point(0, 0);
        rightRootLayout.Margin = new Padding(0);
        rightRootLayout.Name = "rightRootLayout";
        rightRootLayout.Padding = new Padding(12);
        rightRootLayout.RowCount = 2;
        rightRootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        rightRootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rightRootLayout.Size = new Size(1324, 906);
        rightRootLayout.TabIndex = 0;
        // 
        // triggerHeaderLayout
        // 
        triggerHeaderLayout.ColumnCount = 3;
        triggerHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
        triggerHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        triggerHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        triggerHeaderLayout.Controls.Add(triggerNameLabel, 0, 0);
        triggerHeaderLayout.Controls.Add(triggerNameTextBox, 1, 0);
        triggerHeaderLayout.Controls.Add(scriptFormatButton, 2, 0);
        triggerHeaderLayout.Dock = DockStyle.Fill;
        triggerHeaderLayout.Location = new Point(12, 12);
        triggerHeaderLayout.Margin = new Padding(0);
        triggerHeaderLayout.Name = "triggerHeaderLayout";
        triggerHeaderLayout.RowCount = 1;
        triggerHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        triggerHeaderLayout.Size = new Size(1300, 56);
        triggerHeaderLayout.TabIndex = 0;
        // 
        // triggerNameLabel
        // 
        triggerNameLabel.Dock = DockStyle.Fill;
        triggerNameLabel.Location = new Point(0, 0);
        triggerNameLabel.Margin = new Padding(0);
        triggerNameLabel.Name = "triggerNameLabel";
        triggerNameLabel.Size = new Size(132, 56);
        triggerNameLabel.TabIndex = 0;
        triggerNameLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // triggerNameTextBox
        // 
        triggerNameTextBox.Dock = DockStyle.Fill;
        triggerNameTextBox.Location = new Point(132, 7);
        triggerNameTextBox.Margin = new Padding(0, 13, 12, 13);
        triggerNameTextBox.Name = "triggerNameTextBox";
        triggerNameTextBox.Size = new Size(1036, 30);
        triggerNameTextBox.TabIndex = 1;
        // 
        // scriptFormatButton
        // 
        scriptFormatButton.Dock = DockStyle.Fill;
        scriptFormatButton.Location = new Point(1180, 4);
        scriptFormatButton.Margin = new Padding(0, 4, 12, 4);
        scriptFormatButton.Name = "scriptFormatButton";
        scriptFormatButton.Size = new Size(120, 48);
        scriptFormatButton.TabIndex = 2;
        scriptFormatButton.TextAlign = ContentAlignment.MiddleLeft;
        scriptFormatButton.UseVisualStyleBackColor = true;
        // 
        // modeHostPanel
        // 
        modeHostPanel.Controls.Add(emptyStatePanel);
        modeHostPanel.Controls.Add(structuredPanel);
        modeHostPanel.Controls.Add(nodeGraphPanel);
        modeHostPanel.Controls.Add(scriptPanel);
        modeHostPanel.Dock = DockStyle.Fill;
        modeHostPanel.Location = new Point(12, 68);
        modeHostPanel.Margin = new Padding(0);
        modeHostPanel.Name = "modeHostPanel";
        modeHostPanel.Size = new Size(1300, 826);
        modeHostPanel.TabIndex = 1;
        // 
        // emptyStatePanel
        // 
        emptyStatePanel.Controls.Add(emptyStateLabel);
        emptyStatePanel.Dock = DockStyle.Fill;
        emptyStatePanel.Location = new Point(0, 0);
        emptyStatePanel.Name = "emptyStatePanel";
        emptyStatePanel.Size = new Size(1300, 826);
        emptyStatePanel.TabIndex = 0;
        // 
        // emptyStateLabel
        // 
        emptyStateLabel.Dock = DockStyle.Fill;
        emptyStateLabel.Location = new Point(0, 0);
        emptyStateLabel.Name = "emptyStateLabel";
        emptyStateLabel.Size = new Size(1300, 826);
        emptyStateLabel.TabIndex = 0;
        emptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // structuredPanel
        // 
        structuredPanel.ColumnCount = 1;
        structuredPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        structuredPanel.Controls.Add(structuredSummaryLabel, 0, 0);
        structuredPanel.Controls.Add(eventGroupBox, 0, 1);
        structuredPanel.Controls.Add(conditionGroupBox, 0, 2);
        structuredPanel.Controls.Add(actionGroupBox, 0, 3);
        structuredPanel.Dock = DockStyle.Fill;
        structuredPanel.Location = new Point(0, 0);
        structuredPanel.Name = "structuredPanel";
        structuredPanel.RowCount = 4;
        structuredPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        structuredPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
        structuredPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33F));
        structuredPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 34F));
        structuredPanel.Size = new Size(1300, 826);
        structuredPanel.TabIndex = 1;
        // 
        // structuredSummaryLabel
        // 
        structuredSummaryLabel.Dock = DockStyle.Fill;
        structuredSummaryLabel.Location = new Point(0, 0);
        structuredSummaryLabel.Margin = new Padding(0);
        structuredSummaryLabel.Name = "structuredSummaryLabel";
        structuredSummaryLabel.Size = new Size(1300, 36);
        structuredSummaryLabel.TabIndex = 0;
        structuredSummaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // eventGroupBox
        // 
        eventGroupBox.Controls.Add(eventGroupLayout);
        eventGroupBox.Dock = DockStyle.Fill;
        eventGroupBox.Location = new Point(0, 36);
        eventGroupBox.Margin = new Padding(0);
        eventGroupBox.Name = "eventGroupBox";
        eventGroupBox.Padding = new Padding(8);
        eventGroupBox.Size = new Size(1300, 187);
        eventGroupBox.TabIndex = 1;
        eventGroupBox.TabStop = false;
        // 
        // eventGroupLayout
        // 
        eventGroupLayout.ColumnCount = 1;
        eventGroupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        eventGroupLayout.Controls.Add(eventListBox, 0, 0);
        eventGroupLayout.Controls.Add(eventButtonsPanel, 0, 1);
        eventGroupLayout.Dock = DockStyle.Fill;
        eventGroupLayout.Location = new Point(8, 31);
        eventGroupLayout.Margin = new Padding(0);
        eventGroupLayout.Name = "eventGroupLayout";
        eventGroupLayout.RowCount = 2;
        eventGroupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        eventGroupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        eventGroupLayout.Size = new Size(1284, 148);
        eventGroupLayout.TabIndex = 0;
        // 
        // eventListBox
        // 
        eventListBox.Dock = DockStyle.Fill;
        eventListBox.FormattingEnabled = true;
        eventListBox.ItemHeight = 24;
        eventListBox.Location = new Point(0, 0);
        eventListBox.Margin = new Padding(0, 0, 0, 8);
        eventListBox.Name = "eventListBox";
        eventListBox.Size = new Size(1284, 96);
        eventListBox.TabIndex = 0;
        // 
        // eventButtonsPanel
        // 
        eventButtonsPanel.Controls.Add(addEventButton);
        eventButtonsPanel.Controls.Add(editEventButton);
        eventButtonsPanel.Controls.Add(deleteEventButton);
        eventButtonsPanel.Dock = DockStyle.Fill;
        eventButtonsPanel.Location = new Point(0, 104);
        eventButtonsPanel.Margin = new Padding(0);
        eventButtonsPanel.Name = "eventButtonsPanel";
        eventButtonsPanel.Size = new Size(1284, 44);
        eventButtonsPanel.TabIndex = 1;
        eventButtonsPanel.WrapContents = false;
        // 
        // addEventButton
        // 
        addEventButton.Location = new Point(0, 0);
        addEventButton.Margin = new Padding(0, 0, 8, 0);
        addEventButton.Name = "addEventButton";
        addEventButton.Size = new Size(120, 40);
        addEventButton.TabIndex = 0;
        addEventButton.UseVisualStyleBackColor = true;
        // 
        // editEventButton
        // 
        editEventButton.Location = new Point(128, 0);
        editEventButton.Margin = new Padding(0, 0, 8, 0);
        editEventButton.Name = "editEventButton";
        editEventButton.Size = new Size(120, 40);
        editEventButton.TabIndex = 1;
        editEventButton.UseVisualStyleBackColor = true;
        // 
        // deleteEventButton
        // 
        deleteEventButton.Location = new Point(256, 0);
        deleteEventButton.Margin = new Padding(0);
        deleteEventButton.Name = "deleteEventButton";
        deleteEventButton.Size = new Size(120, 40);
        deleteEventButton.TabIndex = 2;
        deleteEventButton.UseVisualStyleBackColor = true;
        // 
        // conditionGroupBox
        // 
        conditionGroupBox.Controls.Add(conditionGroupLayout);
        conditionGroupBox.Dock = DockStyle.Fill;
        conditionGroupBox.Location = new Point(0, 223);
        conditionGroupBox.Margin = new Padding(0);
        conditionGroupBox.Name = "conditionGroupBox";
        conditionGroupBox.Padding = new Padding(8);
        conditionGroupBox.Size = new Size(1300, 187);
        conditionGroupBox.TabIndex = 2;
        conditionGroupBox.TabStop = false;
        // 
        // conditionGroupLayout
        // 
        conditionGroupLayout.ColumnCount = 1;
        conditionGroupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        conditionGroupLayout.Controls.Add(conditionListBox, 0, 0);
        conditionGroupLayout.Controls.Add(conditionButtonsPanel, 0, 1);
        conditionGroupLayout.Dock = DockStyle.Fill;
        conditionGroupLayout.Location = new Point(8, 31);
        conditionGroupLayout.Margin = new Padding(0);
        conditionGroupLayout.Name = "conditionGroupLayout";
        conditionGroupLayout.RowCount = 2;
        conditionGroupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        conditionGroupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        conditionGroupLayout.Size = new Size(1284, 148);
        conditionGroupLayout.TabIndex = 0;
        // 
        // conditionListBox
        // 
        conditionListBox.Dock = DockStyle.Fill;
        conditionListBox.FormattingEnabled = true;
        conditionListBox.ItemHeight = 24;
        conditionListBox.Location = new Point(0, 0);
        conditionListBox.Margin = new Padding(0, 0, 0, 8);
        conditionListBox.Name = "conditionListBox";
        conditionListBox.Size = new Size(1284, 96);
        conditionListBox.TabIndex = 0;
        // 
        // conditionButtonsPanel
        // 
        conditionButtonsPanel.Controls.Add(addConditionButton);
        conditionButtonsPanel.Controls.Add(editConditionButton);
        conditionButtonsPanel.Controls.Add(deleteConditionButton);
        conditionButtonsPanel.Controls.Add(moveConditionUpButton);
        conditionButtonsPanel.Controls.Add(moveConditionDownButton);
        conditionButtonsPanel.Dock = DockStyle.Fill;
        conditionButtonsPanel.Location = new Point(0, 104);
        conditionButtonsPanel.Margin = new Padding(0);
        conditionButtonsPanel.Name = "conditionButtonsPanel";
        conditionButtonsPanel.Size = new Size(1284, 44);
        conditionButtonsPanel.TabIndex = 1;
        conditionButtonsPanel.WrapContents = false;
        // 
        // addConditionButton
        // 
        addConditionButton.Location = new Point(0, 0);
        addConditionButton.Margin = new Padding(0, 0, 8, 0);
        addConditionButton.Name = "addConditionButton";
        addConditionButton.Size = new Size(120, 40);
        addConditionButton.TabIndex = 0;
        addConditionButton.UseVisualStyleBackColor = true;
        // 
        // editConditionButton
        // 
        editConditionButton.Location = new Point(128, 0);
        editConditionButton.Margin = new Padding(0, 0, 8, 0);
        editConditionButton.Name = "editConditionButton";
        editConditionButton.Size = new Size(120, 40);
        editConditionButton.TabIndex = 1;
        editConditionButton.UseVisualStyleBackColor = true;
        // 
        // deleteConditionButton
        // 
        deleteConditionButton.Location = new Point(256, 0);
        deleteConditionButton.Margin = new Padding(0, 0, 8, 0);
        deleteConditionButton.Name = "deleteConditionButton";
        deleteConditionButton.Size = new Size(120, 40);
        deleteConditionButton.TabIndex = 2;
        deleteConditionButton.UseVisualStyleBackColor = true;
        // 
        // moveConditionUpButton
        // 
        moveConditionUpButton.Location = new Point(384, 0);
        moveConditionUpButton.Margin = new Padding(0, 0, 8, 0);
        moveConditionUpButton.Name = "moveConditionUpButton";
        moveConditionUpButton.Size = new Size(120, 40);
        moveConditionUpButton.TabIndex = 3;
        moveConditionUpButton.UseVisualStyleBackColor = true;
        // 
        // moveConditionDownButton
        // 
        moveConditionDownButton.Location = new Point(512, 0);
        moveConditionDownButton.Margin = new Padding(0);
        moveConditionDownButton.Name = "moveConditionDownButton";
        moveConditionDownButton.Size = new Size(120, 40);
        moveConditionDownButton.TabIndex = 4;
        moveConditionDownButton.UseVisualStyleBackColor = true;
        // 
        // actionGroupBox
        // 
        actionGroupBox.Controls.Add(actionGroupLayout);
        actionGroupBox.Dock = DockStyle.Fill;
        actionGroupBox.Location = new Point(0, 410);
        actionGroupBox.Margin = new Padding(0);
        actionGroupBox.Name = "actionGroupBox";
        actionGroupBox.Padding = new Padding(8);
        actionGroupBox.Size = new Size(1300, 193);
        actionGroupBox.TabIndex = 3;
        actionGroupBox.TabStop = false;
        // 
        // actionGroupLayout
        // 
        actionGroupLayout.ColumnCount = 1;
        actionGroupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        actionGroupLayout.Controls.Add(actionListBox, 0, 0);
        actionGroupLayout.Controls.Add(actionButtonsPanel, 0, 1);
        actionGroupLayout.Dock = DockStyle.Fill;
        actionGroupLayout.Location = new Point(8, 31);
        actionGroupLayout.Margin = new Padding(0);
        actionGroupLayout.Name = "actionGroupLayout";
        actionGroupLayout.RowCount = 2;
        actionGroupLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        actionGroupLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        actionGroupLayout.Size = new Size(1284, 154);
        actionGroupLayout.TabIndex = 0;
        // 
        // actionListBox
        // 
        actionListBox.Dock = DockStyle.Fill;
        actionListBox.FormattingEnabled = true;
        actionListBox.ItemHeight = 24;
        actionListBox.Location = new Point(0, 0);
        actionListBox.Margin = new Padding(0, 0, 0, 8);
        actionListBox.Name = "actionListBox";
        actionListBox.Size = new Size(1284, 102);
        actionListBox.TabIndex = 0;
        // 
        // actionButtonsPanel
        // 
        actionButtonsPanel.Controls.Add(addActionButton);
        actionButtonsPanel.Controls.Add(editActionButton);
        actionButtonsPanel.Controls.Add(deleteActionButton);
        actionButtonsPanel.Controls.Add(moveActionUpButton);
        actionButtonsPanel.Controls.Add(moveActionDownButton);
        actionButtonsPanel.Dock = DockStyle.Fill;
        actionButtonsPanel.Location = new Point(0, 110);
        actionButtonsPanel.Margin = new Padding(0);
        actionButtonsPanel.Name = "actionButtonsPanel";
        actionButtonsPanel.Size = new Size(1284, 44);
        actionButtonsPanel.TabIndex = 1;
        actionButtonsPanel.WrapContents = false;
        // 
        // addActionButton
        // 
        addActionButton.Location = new Point(0, 0);
        addActionButton.Margin = new Padding(0, 0, 8, 0);
        addActionButton.Name = "addActionButton";
        addActionButton.Size = new Size(120, 40);
        addActionButton.TabIndex = 0;
        addActionButton.UseVisualStyleBackColor = true;
        // 
        // editActionButton
        // 
        editActionButton.Location = new Point(128, 0);
        editActionButton.Margin = new Padding(0, 0, 8, 0);
        editActionButton.Name = "editActionButton";
        editActionButton.Size = new Size(120, 40);
        editActionButton.TabIndex = 1;
        editActionButton.UseVisualStyleBackColor = true;
        // 
        // deleteActionButton
        // 
        deleteActionButton.Location = new Point(256, 0);
        deleteActionButton.Margin = new Padding(0, 0, 8, 0);
        deleteActionButton.Name = "deleteActionButton";
        deleteActionButton.Size = new Size(120, 40);
        deleteActionButton.TabIndex = 2;
        deleteActionButton.UseVisualStyleBackColor = true;
        // 
        // moveActionUpButton
        // 
        moveActionUpButton.Location = new Point(384, 0);
        moveActionUpButton.Margin = new Padding(0, 0, 8, 0);
        moveActionUpButton.Name = "moveActionUpButton";
        moveActionUpButton.Size = new Size(120, 40);
        moveActionUpButton.TabIndex = 3;
        moveActionUpButton.UseVisualStyleBackColor = true;
        // 
        // moveActionDownButton
        // 
        moveActionDownButton.Location = new Point(512, 0);
        moveActionDownButton.Margin = new Padding(0);
        moveActionDownButton.Name = "moveActionDownButton";
        moveActionDownButton.Size = new Size(120, 40);
        moveActionDownButton.TabIndex = 4;
        moveActionDownButton.UseVisualStyleBackColor = true;
        // 
        // nodeGraphPanel
        // 
        nodeGraphPanel.ColumnCount = 1;
        nodeGraphPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        nodeGraphPanel.Controls.Add(nodeGraphHelpLabel, 0, 0);
        nodeGraphPanel.Controls.Add(nodeCanvasPanel, 0, 1);
        nodeGraphPanel.Controls.Add(nodeDetailsGroupBox, 0, 2);
        nodeGraphPanel.Dock = DockStyle.Fill;
        nodeGraphPanel.Location = new Point(0, 0);
        nodeGraphPanel.Name = "nodeGraphPanel";
        nodeGraphPanel.RowCount = 3;
        nodeGraphPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        nodeGraphPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        nodeGraphPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 264F));
        nodeGraphPanel.Size = new Size(1300, 826);
        nodeGraphPanel.TabIndex = 2;
        // 
        // nodeGraphHelpLabel
        // 
        nodeGraphHelpLabel.Dock = DockStyle.Fill;
        nodeGraphHelpLabel.Location = new Point(0, 0);
        nodeGraphHelpLabel.Margin = new Padding(0, 0, 0, 8);
        nodeGraphHelpLabel.Name = "nodeGraphHelpLabel";
        nodeGraphHelpLabel.Size = new Size(1300, 32);
        nodeGraphHelpLabel.TabIndex = 0;
        nodeGraphHelpLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nodeCanvasPanel
        // 
        nodeCanvasPanel.BorderStyle = BorderStyle.FixedSingle;
        nodeCanvasPanel.Dock = DockStyle.Fill;
        nodeCanvasPanel.Location = new Point(0, 40);
        nodeCanvasPanel.Margin = new Padding(0, 0, 0, 12);
        nodeCanvasPanel.Name = "nodeCanvasPanel";
        nodeCanvasPanel.Size = new Size(1300, 510);
        nodeCanvasPanel.TabIndex = 1;
        // 
        // nodeDetailsGroupBox
        // 
        nodeDetailsGroupBox.Controls.Add(nodeDetailsLayout);
        nodeDetailsGroupBox.Dock = DockStyle.Fill;
        nodeDetailsGroupBox.Location = new Point(0, 562);
        nodeDetailsGroupBox.Margin = new Padding(0);
        nodeDetailsGroupBox.Name = "nodeDetailsGroupBox";
        nodeDetailsGroupBox.Padding = new Padding(8);
        nodeDetailsGroupBox.Size = new Size(1300, 264);
        nodeDetailsGroupBox.TabIndex = 2;
        nodeDetailsGroupBox.TabStop = false;
        // 
        // nodeDetailsLayout
        // 
        nodeDetailsLayout.ColumnCount = 4;
        nodeDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88F));
        nodeDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        nodeDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
        nodeDetailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
        nodeDetailsLayout.Controls.Add(nodeTypeLabel, 0, 0);
        nodeDetailsLayout.Controls.Add(nodeTypeValueLabel, 1, 0);
        nodeDetailsLayout.Controls.Add(nodeTitleLabel, 0, 1);
        nodeDetailsLayout.Controls.Add(nodeTitleTextBox, 1, 1);
        nodeDetailsLayout.Controls.Add(nodeXLabel, 2, 1);
        nodeDetailsLayout.Controls.Add(nodeXNumericUpDown, 3, 1);
        nodeDetailsLayout.Controls.Add(nodeYLabel, 2, 2);
        nodeDetailsLayout.Controls.Add(nodeYNumericUpDown, 3, 2);
        nodeDetailsLayout.Controls.Add(nodeParametersLabel, 0, 2);
        nodeDetailsLayout.Controls.Add(nodeParametersTextBox, 1, 2);
        nodeDetailsLayout.Controls.Add(nodeSaveButton, 3, 3);
        nodeDetailsLayout.Dock = DockStyle.Fill;
        nodeDetailsLayout.Location = new Point(8, 31);
        nodeDetailsLayout.Margin = new Padding(0);
        nodeDetailsLayout.Name = "nodeDetailsLayout";
        nodeDetailsLayout.RowCount = 4;
        nodeDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        nodeDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        nodeDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        nodeDetailsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        nodeDetailsLayout.Size = new Size(1284, 225);
        nodeDetailsLayout.TabIndex = 0;
        // 
        // nodeTypeLabel
        // 
        nodeTypeLabel.Dock = DockStyle.Fill;
        nodeTypeLabel.Location = new Point(0, 0);
        nodeTypeLabel.Margin = new Padding(0);
        nodeTypeLabel.Name = "nodeTypeLabel";
        nodeTypeLabel.Size = new Size(88, 32);
        nodeTypeLabel.TabIndex = 0;
        nodeTypeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nodeTypeValueLabel
        // 
        nodeTypeValueLabel.Dock = DockStyle.Fill;
        nodeTypeValueLabel.Location = new Point(88, 0);
        nodeTypeValueLabel.Margin = new Padding(0);
        nodeTypeValueLabel.Name = "nodeTypeValueLabel";
        nodeTypeValueLabel.Size = new Size(944, 32);
        nodeTypeValueLabel.TabIndex = 1;
        nodeTypeValueLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nodeTitleLabel
        // 
        nodeTitleLabel.Dock = DockStyle.Fill;
        nodeTitleLabel.Location = new Point(0, 32);
        nodeTitleLabel.Margin = new Padding(0);
        nodeTitleLabel.Name = "nodeTitleLabel";
        nodeTitleLabel.Size = new Size(88, 38);
        nodeTitleLabel.TabIndex = 2;
        nodeTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nodeTitleTextBox
        // 
        nodeTitleTextBox.Dock = DockStyle.Fill;
        nodeTitleTextBox.Location = new Point(88, 36);
        nodeTitleTextBox.Margin = new Padding(0, 4, 12, 4);
        nodeTitleTextBox.Name = "nodeTitleTextBox";
        nodeTitleTextBox.Size = new Size(932, 30);
        nodeTitleTextBox.TabIndex = 3;
        // 
        // nodeXLabel
        // 
        nodeXLabel.Dock = DockStyle.Fill;
        nodeXLabel.Location = new Point(1032, 32);
        nodeXLabel.Margin = new Padding(0);
        nodeXLabel.Name = "nodeXLabel";
        nodeXLabel.Size = new Size(72, 38);
        nodeXLabel.TabIndex = 4;
        nodeXLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nodeXNumericUpDown
        // 
        nodeXNumericUpDown.Dock = DockStyle.Fill;
        nodeXNumericUpDown.Location = new Point(1104, 36);
        nodeXNumericUpDown.Margin = new Padding(0, 4, 0, 4);
        nodeXNumericUpDown.Maximum = new decimal(new int[] { 50000, 0, 0, 0 });
        nodeXNumericUpDown.Minimum = new decimal(new int[] { 50000, 0, 0, int.MinValue });
        nodeXNumericUpDown.Name = "nodeXNumericUpDown";
        nodeXNumericUpDown.Size = new Size(180, 30);
        nodeXNumericUpDown.TabIndex = 5;
        // 
        // nodeYLabel
        // 
        nodeYLabel.Dock = DockStyle.Top;
        nodeYLabel.Location = new Point(1032, 70);
        nodeYLabel.Margin = new Padding(0);
        nodeYLabel.Name = "nodeYLabel";
        nodeYLabel.Size = new Size(72, 30);
        nodeYLabel.TabIndex = 6;
        nodeYLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nodeYNumericUpDown
        // 
        nodeYNumericUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        nodeYNumericUpDown.Location = new Point(1104, 74);
        nodeYNumericUpDown.Margin = new Padding(0, 4, 0, 0);
        nodeYNumericUpDown.Maximum = new decimal(new int[] { 50000, 0, 0, 0 });
        nodeYNumericUpDown.Minimum = new decimal(new int[] { 50000, 0, 0, int.MinValue });
        nodeYNumericUpDown.Name = "nodeYNumericUpDown";
        nodeYNumericUpDown.Size = new Size(180, 30);
        nodeYNumericUpDown.TabIndex = 7;
        // 
        // nodeParametersLabel
        // 
        nodeParametersLabel.Dock = DockStyle.Fill;
        nodeParametersLabel.Location = new Point(0, 70);
        nodeParametersLabel.Margin = new Padding(0);
        nodeParametersLabel.Name = "nodeParametersLabel";
        nodeParametersLabel.Size = new Size(88, 107);
        nodeParametersLabel.TabIndex = 8;
        // 
        // nodeParametersTextBox
        // 
        nodeParametersTextBox.AcceptsReturn = true;
        nodeParametersTextBox.AcceptsTab = true;
        nodeParametersTextBox.Dock = DockStyle.Fill;
        nodeParametersTextBox.Location = new Point(88, 74);
        nodeParametersTextBox.Margin = new Padding(0, 4, 12, 4);
        nodeParametersTextBox.Multiline = true;
        nodeParametersTextBox.Name = "nodeParametersTextBox";
        nodeParametersTextBox.ScrollBars = ScrollBars.Vertical;
        nodeParametersTextBox.Size = new Size(932, 99);
        nodeParametersTextBox.TabIndex = 9;
        // 
        // nodeSaveButton
        // 
        nodeSaveButton.Dock = DockStyle.Fill;
        nodeSaveButton.Location = new Point(1104, 177);
        nodeSaveButton.Margin = new Padding(0);
        nodeSaveButton.Name = "nodeSaveButton";
        nodeSaveButton.Size = new Size(180, 48);
        nodeSaveButton.TabIndex = 10;
        nodeSaveButton.UseVisualStyleBackColor = true;
        // 
        // scriptPanel
        // 
        scriptPanel.ColumnCount = 1;
        scriptPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        scriptPanel.Controls.Add(scriptCodeRichTextBox, 0, 0);
        scriptPanel.Dock = DockStyle.Fill;
        scriptPanel.Location = new Point(0, 0);
        scriptPanel.Name = "scriptPanel";
        scriptPanel.RowCount = 1;
        scriptPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        scriptPanel.Size = new Size(1300, 826);
        scriptPanel.TabIndex = 3;
        // 
        // scriptCodeRichTextBox
        // 
        scriptCodeRichTextBox.AcceptsTab = true;
        scriptCodeRichTextBox.BorderStyle = BorderStyle.FixedSingle;
        scriptCodeRichTextBox.DetectUrls = false;
        scriptCodeRichTextBox.Dock = DockStyle.Fill;
        scriptCodeRichTextBox.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
        scriptCodeRichTextBox.Location = new Point(0, 0);
        scriptCodeRichTextBox.Margin = new Padding(0);
        scriptCodeRichTextBox.Name = "scriptCodeRichTextBox";
        scriptCodeRichTextBox.Size = new Size(1300, 826);
        scriptCodeRichTextBox.TabIndex = 5;
        scriptCodeRichTextBox.Text = "";
        scriptCodeRichTextBox.WordWrap = false;
        // 
        // scriptModeTitleLabel
        // 
        scriptModeTitleLabel.Dock = DockStyle.Fill;
        scriptModeTitleLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        scriptModeTitleLabel.Location = new Point(0, 0);
        scriptModeTitleLabel.Margin = new Padding(0, 0, 0, 8);
        scriptModeTitleLabel.Name = "scriptModeTitleLabel";
        scriptModeTitleLabel.Size = new Size(1300, 32);
        scriptModeTitleLabel.TabIndex = 0;
        scriptModeTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // scriptPathLabel
        // 
        scriptPathLabel.Dock = DockStyle.Fill;
        scriptPathLabel.Location = new Point(0, 0);
        scriptPathLabel.Margin = new Padding(0, 0, 8, 0);
        scriptPathLabel.Name = "scriptPathLabel";
        scriptPathLabel.Size = new Size(84, 40);
        scriptPathLabel.TabIndex = 1;
        scriptPathLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // scriptPathTextBox
        // 
        scriptPathTextBox.Dock = DockStyle.Fill;
        scriptPathTextBox.Location = new Point(92, 5);
        scriptPathTextBox.Margin = new Padding(0, 5, 12, 5);
        scriptPathTextBox.Name = "scriptPathTextBox";
        scriptPathTextBox.ReadOnly = true;
        scriptPathTextBox.Size = new Size(276, 30);
        scriptPathTextBox.TabIndex = 2;
        // 
        // scriptHelpLabel
        // 
        scriptHelpLabel.Dock = DockStyle.Fill;
        scriptHelpLabel.Location = new Point(0, 112);
        scriptHelpLabel.Margin = new Padding(0, 0, 12, 8);
        scriptHelpLabel.Name = "scriptHelpLabel";
        scriptHelpLabel.Size = new Size(368, 618);
        scriptHelpLabel.TabIndex = 3;
        // 
        // scriptSaveButton
        // 
        scriptSaveButton.Dock = DockStyle.Fill;
        scriptSaveButton.Location = new Point(0, 738);
        scriptSaveButton.Margin = new Padding(0, 0, 12, 0);
        scriptSaveButton.Name = "scriptSaveButton";
        scriptSaveButton.Size = new Size(368, 48);
        scriptSaveButton.TabIndex = 4;
        scriptSaveButton.UseVisualStyleBackColor = true;
        // 
        // EventGraphEditorForm
        // 
        AutoScaleDimensions = new SizeF(11F, 24F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1680, 980);
        Controls.Add(rootLayout);
        MinimumSize = new Size(1280, 820);
        Name = "EventGraphEditorForm";
        Text = "事件图编辑器";
        rootLayout.ResumeLayout(false);
        rootLayout.PerformLayout();
        mainToolStrip.ResumeLayout(false);
        mainToolStrip.PerformLayout();
        workspaceSplitContainer.Panel1.ResumeLayout(false);
        workspaceSplitContainer.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)workspaceSplitContainer).EndInit();
        workspaceSplitContainer.ResumeLayout(false);
        triggerListLayout.ResumeLayout(false);
        rightRootLayout.ResumeLayout(false);
        triggerHeaderLayout.ResumeLayout(false);
        triggerHeaderLayout.PerformLayout();
        modeHostPanel.ResumeLayout(false);
        emptyStatePanel.ResumeLayout(false);
        structuredPanel.ResumeLayout(false);
        eventGroupBox.ResumeLayout(false);
        eventGroupLayout.ResumeLayout(false);
        eventButtonsPanel.ResumeLayout(false);
        conditionGroupBox.ResumeLayout(false);
        conditionGroupLayout.ResumeLayout(false);
        conditionButtonsPanel.ResumeLayout(false);
        actionGroupBox.ResumeLayout(false);
        actionGroupLayout.ResumeLayout(false);
        actionButtonsPanel.ResumeLayout(false);
        nodeGraphPanel.ResumeLayout(false);
        nodeDetailsGroupBox.ResumeLayout(false);
        nodeDetailsLayout.ResumeLayout(false);
        nodeDetailsLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)nodeXNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)nodeYNumericUpDown).EndInit();
        scriptPanel.ResumeLayout(false);
        ResumeLayout(false);
    }

    private TableLayoutPanel rootLayout;
    private ToolStrip mainToolStrip;
    private ToolStripLabel graphToolStripLabel;
    private ToolStripButton newTriggerToolStripButton;
    private ToolStripButton newScriptTriggerToolStripButton;
    private ToolStripButton duplicateTriggerToolStripButton;
    private ToolStripButton removeTriggerToolStripButton;
    private ToolStripButton toggleEnabledToolStripButton;
    private ToolStripButton structuredModeToolStripButton;
    private ToolStripButton nodeGraphModeToolStripButton;
    private ToolStripButton convertToScriptToolStripButton;
    private ToolStripButton saveProjectToolStripButton;
    private SplitContainer workspaceSplitContainer;
    private TableLayoutPanel triggerListLayout;
    private Label triggerListTitleLabel;
    private TreeView triggerTreeView;
    private TableLayoutPanel rightRootLayout;
    private TableLayoutPanel triggerHeaderLayout;
    private Label triggerNameLabel;
    private TextBox triggerNameTextBox;
    private Button scriptFormatButton;
    private Panel modeHostPanel;
    private Panel emptyStatePanel;
    private Label emptyStateLabel;
    private TableLayoutPanel structuredPanel;
    private Label structuredSummaryLabel;
    private GroupBox eventGroupBox;
    private TableLayoutPanel eventGroupLayout;
    private ListBox eventListBox;
    private FlowLayoutPanel eventButtonsPanel;
    private Button addEventButton;
    private Button editEventButton;
    private Button deleteEventButton;
    private GroupBox conditionGroupBox;
    private TableLayoutPanel conditionGroupLayout;
    private ListBox conditionListBox;
    private FlowLayoutPanel conditionButtonsPanel;
    private Button addConditionButton;
    private Button editConditionButton;
    private Button deleteConditionButton;
    private Button moveConditionUpButton;
    private Button moveConditionDownButton;
    private GroupBox actionGroupBox;
    private TableLayoutPanel actionGroupLayout;
    private ListBox actionListBox;
    private FlowLayoutPanel actionButtonsPanel;
    private Button addActionButton;
    private Button editActionButton;
    private Button deleteActionButton;
    private Button moveActionUpButton;
    private Button moveActionDownButton;
    private GroupBox advancedGroupBox;
    private TextBox advancedParametersTextBox;
    private Button structuredSaveButton;
    private TableLayoutPanel nodeGraphPanel;
    private Label nodeGraphHelpLabel;
    private NodeGraphCanvasControl nodeCanvasPanel;
    private GroupBox nodeDetailsGroupBox;
    private TableLayoutPanel nodeDetailsLayout;
    private Label nodeTypeLabel;
    private Label nodeTypeValueLabel;
    private Label nodeTitleLabel;
    private TextBox nodeTitleTextBox;
    private Label nodeXLabel;
    private NumericUpDown nodeXNumericUpDown;
    private Label nodeYLabel;
    private NumericUpDown nodeYNumericUpDown;
    private Label nodeParametersLabel;
    private TextBox nodeParametersTextBox;
    private Button nodeSaveButton;
    private TableLayoutPanel scriptPanel;
    private Label scriptModeTitleLabel;
    private Label scriptPathLabel;
    private TextBox scriptPathTextBox;
    private Label scriptHelpLabel;
    private Button scriptSaveButton;
    private RichTextBox scriptCodeRichTextBox;
    private ComboBox graphToolStripHost;
}
