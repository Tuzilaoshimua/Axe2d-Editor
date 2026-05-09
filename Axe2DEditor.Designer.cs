namespace Axe2DEditor
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            mainMenuStrip = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            newProjectToolStripMenuItem = new ToolStripMenuItem();
            openProjectToolStripMenuItem = new ToolStripMenuItem();
            saveProjectToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            modulesToolStripMenuItem = new ToolStripMenuItem();
            dataEditorToolStripMenuItem = new ToolStripMenuItem();
            formulaEditorToolStripMenuItem = new ToolStripMenuItem();
            mapEditorToolStripMenuItem = new ToolStripMenuItem();
            eventGraphEditorToolStripMenuItem = new ToolStripMenuItem();
            optionsToolStripMenuItem = new ToolStripMenuItem();
            languageToolStripMenuItem = new ToolStripMenuItem();
            languageZhCnToolStripMenuItem = new ToolStripMenuItem();
            languageEnToolStripMenuItem = new ToolStripMenuItem();
            languageJaJpToolStripMenuItem = new ToolStripMenuItem();
            themeToolStripMenuItem = new ToolStripMenuItem();
            lightThemeToolStripMenuItem = new ToolStripMenuItem();
            darkThemeToolStripMenuItem = new ToolStripMenuItem();
            editorToolStrip = new ToolStrip();
            toolNewProjectButton = new ToolStripButton();
            toolOpenProjectButton = new ToolStripButton();
            toolSaveProjectButton = new ToolStripButton();
            toolSep1 = new ToolStripSeparator();
            _toolUndoSceneButton = new ToolStripButton();
            _toolRedoSceneButton = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            toolDataEditorButton = new ToolStripButton();
            toolFormulaEditorButton = new ToolStripButton();
            toolMapEditorButton = new ToolStripButton();
            toolEventGraphEditorButton = new ToolStripButton();
            toolRuntimeSep = new ToolStripSeparator();
            toolRunButton = new ToolStripButton();
            toolTestButton = new ToolStripButton();
            toolStopButton = new ToolStripButton();
            toolSceneSep1 = new ToolStripSeparator();
            _toolSelectSceneButton = new ToolStripButton();
            _toolMoveSceneButton = new ToolStripButton();
            _toolRotateSceneButton = new ToolStripButton();
            _toolScaleSceneButton = new ToolStripButton();
            _toolRectSceneButton = new ToolStripButton();
            _toolAlignSceneButton = new ToolStripButton();
            toolSceneSep2 = new ToolStripSeparator();
            _toolToggleGridButton = new ToolStripButton();
            mainSplit = new SplitContainer();
            leftSplit = new SplitContainer();
            hierarchyGroupBox = new GroupBox();
            hierarchyTreeView = new TreeView();
            assetsGroupBox = new GroupBox();
            assetsTreeView = new TreeView();
            centerRightSplit = new SplitContainer();
            centerSplit = new SplitContainer();
            sceneGroupBox = new GroupBox();
            sceneCanvasPanel = new Axe2DEditor.Editor.Controls.SceneCanvasPanel();
            logGroupBox = new GroupBox();
            logTextBox = new TextBox();
            rightSplit = new SplitContainer();
            inspectorGroupBox = new GroupBox();
            inspectorTextBox = new TextBox();
            previewGroupBox = new GroupBox();
            previewBox = new PictureBox();
            statusStrip = new StatusStrip();
            projectStatusLabel = new ToolStripStatusLabel();
            stateStatusLabel = new ToolStripStatusLabel();
            mainMenuStrip.SuspendLayout();
            editorToolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)mainSplit).BeginInit();
            mainSplit.Panel1.SuspendLayout();
            mainSplit.Panel2.SuspendLayout();
            mainSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)leftSplit).BeginInit();
            leftSplit.Panel1.SuspendLayout();
            leftSplit.Panel2.SuspendLayout();
            leftSplit.SuspendLayout();
            hierarchyGroupBox.SuspendLayout();
            assetsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)centerRightSplit).BeginInit();
            centerRightSplit.Panel1.SuspendLayout();
            centerRightSplit.Panel2.SuspendLayout();
            centerRightSplit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)centerSplit).BeginInit();
            centerSplit.Panel1.SuspendLayout();
            centerSplit.Panel2.SuspendLayout();
            centerSplit.SuspendLayout();
            sceneGroupBox.SuspendLayout();
            logGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)rightSplit).BeginInit();
            rightSplit.Panel1.SuspendLayout();
            rightSplit.Panel2.SuspendLayout();
            rightSplit.SuspendLayout();
            inspectorGroupBox.SuspendLayout();
            previewGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)previewBox).BeginInit();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // mainMenuStrip
            // 
            mainMenuStrip.ImageScalingSize = new Size(24, 24);
            mainMenuStrip.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, modulesToolStripMenuItem, optionsToolStripMenuItem });
            mainMenuStrip.Location = new Point(0, 0);
            mainMenuStrip.Name = "mainMenuStrip";
            mainMenuStrip.Size = new Size(1400, 32);
            mainMenuStrip.TabIndex = 0;
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { newProjectToolStripMenuItem, openProjectToolStripMenuItem, saveProjectToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(62, 28);
            fileToolStripMenuItem.Text = "文件";
            // 
            // newProjectToolStripMenuItem
            // 
            newProjectToolStripMenuItem.Name = "newProjectToolStripMenuItem";
            newProjectToolStripMenuItem.Size = new Size(182, 34);
            newProjectToolStripMenuItem.Text = "新建项目";
            newProjectToolStripMenuItem.Click += NewProjectMenuItem_Click;
            // 
            // openProjectToolStripMenuItem
            // 
            openProjectToolStripMenuItem.Name = "openProjectToolStripMenuItem";
            openProjectToolStripMenuItem.Size = new Size(182, 34);
            openProjectToolStripMenuItem.Text = "打开项目";
            openProjectToolStripMenuItem.Click += OpenProjectMenuItem_Click;
            // 
            // saveProjectToolStripMenuItem
            // 
            saveProjectToolStripMenuItem.Name = "saveProjectToolStripMenuItem";
            saveProjectToolStripMenuItem.Size = new Size(182, 34);
            saveProjectToolStripMenuItem.Text = "保存项目";
            saveProjectToolStripMenuItem.Click += SaveProjectMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(182, 34);
            exitToolStripMenuItem.Text = "退出";
            exitToolStripMenuItem.Click += ExitMenuItem_Click;
            // 
            // modulesToolStripMenuItem
            // 
            modulesToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { dataEditorToolStripMenuItem, formulaEditorToolStripMenuItem, mapEditorToolStripMenuItem, eventGraphEditorToolStripMenuItem });
            modulesToolStripMenuItem.Name = "modulesToolStripMenuItem";
            modulesToolStripMenuItem.Size = new Size(62, 28);
            modulesToolStripMenuItem.Text = "模块";
            // 
            // dataEditorToolStripMenuItem
            // 
            dataEditorToolStripMenuItem.Name = "dataEditorToolStripMenuItem";
            dataEditorToolStripMenuItem.Size = new Size(218, 34);
            dataEditorToolStripMenuItem.Text = "数据编辑器";
            dataEditorToolStripMenuItem.Click += OpenDataEditorMenuItem_Click;
            // 
            // formulaEditorToolStripMenuItem
            // 
            formulaEditorToolStripMenuItem.Name = "formulaEditorToolStripMenuItem";
            formulaEditorToolStripMenuItem.Size = new Size(218, 34);
            formulaEditorToolStripMenuItem.Text = "公式编辑器";
            formulaEditorToolStripMenuItem.Click += OpenFormulaEditorMenuItem_Click;
            // 
            // mapEditorToolStripMenuItem
            // 
            mapEditorToolStripMenuItem.Name = "mapEditorToolStripMenuItem";
            mapEditorToolStripMenuItem.Size = new Size(218, 34);
            mapEditorToolStripMenuItem.Text = "地图编辑器";
            mapEditorToolStripMenuItem.Click += OpenMapEditorMenuItem_Click;
            // 
            // eventGraphEditorToolStripMenuItem
            // 
            eventGraphEditorToolStripMenuItem.Name = "eventGraphEditorToolStripMenuItem";
            eventGraphEditorToolStripMenuItem.Size = new Size(218, 34);
            eventGraphEditorToolStripMenuItem.Text = "事件图编辑器";
            eventGraphEditorToolStripMenuItem.Click += OpenEventGraphEditorMenuItem_Click;
            // 
            // optionsToolStripMenuItem
            // 
            optionsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { languageToolStripMenuItem, themeToolStripMenuItem });
            optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            optionsToolStripMenuItem.Size = new Size(62, 28);
            optionsToolStripMenuItem.Text = "选项";
            // 
            // languageToolStripMenuItem
            // 
            languageToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { languageZhCnToolStripMenuItem, languageEnToolStripMenuItem, languageJaJpToolStripMenuItem });
            languageToolStripMenuItem.Name = "languageToolStripMenuItem";
            languageToolStripMenuItem.Size = new Size(146, 34);
            languageToolStripMenuItem.Text = "语言";
            // 
            // languageZhCnToolStripMenuItem
            // 
            languageZhCnToolStripMenuItem.Name = "languageZhCnToolStripMenuItem";
            languageZhCnToolStripMenuItem.Size = new Size(164, 34);
            languageZhCnToolStripMenuItem.Text = "中文";
            languageZhCnToolStripMenuItem.Click += LanguageZhCnMenuItem_Click;
            // 
            // languageEnToolStripMenuItem
            // 
            languageEnToolStripMenuItem.Name = "languageEnToolStripMenuItem";
            languageEnToolStripMenuItem.Size = new Size(164, 34);
            languageEnToolStripMenuItem.Text = "英文";
            languageEnToolStripMenuItem.Click += LanguageEnMenuItem_Click;
            // 
            // languageJaJpToolStripMenuItem
            // 
            languageJaJpToolStripMenuItem.Name = "languageJaJpToolStripMenuItem";
            languageJaJpToolStripMenuItem.Size = new Size(164, 34);
            languageJaJpToolStripMenuItem.Text = "日本語";
            languageJaJpToolStripMenuItem.Click += LanguageJaJpMenuItem_Click;
            // 
            // themeToolStripMenuItem
            // 
            themeToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { lightThemeToolStripMenuItem, darkThemeToolStripMenuItem });
            themeToolStripMenuItem.Name = "themeToolStripMenuItem";
            themeToolStripMenuItem.Size = new Size(146, 34);
            themeToolStripMenuItem.Text = "主题";
            // 
            // lightThemeToolStripMenuItem
            // 
            lightThemeToolStripMenuItem.Name = "lightThemeToolStripMenuItem";
            lightThemeToolStripMenuItem.Size = new Size(146, 34);
            lightThemeToolStripMenuItem.Text = "浅色";
            lightThemeToolStripMenuItem.Click += LightThemeMenuItem_Click;
            // 
            // darkThemeToolStripMenuItem
            // 
            darkThemeToolStripMenuItem.Name = "darkThemeToolStripMenuItem";
            darkThemeToolStripMenuItem.Size = new Size(146, 34);
            darkThemeToolStripMenuItem.Text = "深色";
            darkThemeToolStripMenuItem.Click += DarkThemeMenuItem_Click;
            // 
            // editorToolStrip
            // 
            editorToolStrip.GripStyle = ToolStripGripStyle.Hidden;
            editorToolStrip.ImageScalingSize = new Size(24, 24);
            editorToolStrip.Items.AddRange(new ToolStripItem[] { toolNewProjectButton, toolOpenProjectButton, toolSaveProjectButton, toolSep1, _toolUndoSceneButton, _toolRedoSceneButton, toolStripSeparator1, toolDataEditorButton, toolFormulaEditorButton, toolMapEditorButton, toolEventGraphEditorButton, toolRuntimeSep, toolRunButton, toolTestButton, toolStopButton, toolSceneSep1, _toolSelectSceneButton, _toolMoveSceneButton, _toolRotateSceneButton, _toolScaleSceneButton, _toolRectSceneButton, _toolAlignSceneButton, toolSceneSep2, _toolToggleGridButton });
            editorToolStrip.Location = new Point(0, 32);
            editorToolStrip.Name = "editorToolStrip";
            editorToolStrip.Size = new Size(1400, 45);
            editorToolStrip.TabIndex = 1;
            // 
            // toolNewProjectButton
            // 
            toolNewProjectButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolNewProjectButton.Name = "toolNewProjectButton";
            toolNewProjectButton.Size = new Size(39, 40);
            toolNewProjectButton.Text = "📁";
            toolNewProjectButton.ToolTipText = "新建一个工程开始制作游戏";
            toolNewProjectButton.Click += NewProjectMenuItem_Click;
            // 
            // toolOpenProjectButton
            // 
            toolOpenProjectButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolOpenProjectButton.Name = "toolOpenProjectButton";
            toolOpenProjectButton.Size = new Size(39, 40);
            toolOpenProjectButton.Text = "📂";
            toolOpenProjectButton.ToolTipText = "打开一个工程";
            toolOpenProjectButton.Click += OpenProjectMenuItem_Click;
            // 
            // toolSaveProjectButton
            // 
            toolSaveProjectButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolSaveProjectButton.Name = "toolSaveProjectButton";
            toolSaveProjectButton.Size = new Size(39, 40);
            toolSaveProjectButton.Text = "💾";
            toolSaveProjectButton.ToolTipText = "保存现有工程";
            toolSaveProjectButton.Click += SaveProjectMenuItem_Click;
            // 
            // toolSep1
            // 
            toolSep1.Name = "toolSep1";
            toolSep1.Size = new Size(6, 45);
            // 
            // _toolUndoSceneButton
            // 
            _toolUndoSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolUndoSceneButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _toolUndoSceneButton.Name = "_toolUndoSceneButton";
            _toolUndoSceneButton.Size = new Size(34, 40);
            _toolUndoSceneButton.Text = "↪️";
            // 
            // _toolRedoSceneButton
            // 
            _toolRedoSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolRedoSceneButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _toolRedoSceneButton.Name = "_toolRedoSceneButton";
            _toolRedoSceneButton.Size = new Size(34, 40);
            _toolRedoSceneButton.Text = "↩️";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 45);
            // 
            // toolDataEditorButton
            // 
            toolDataEditorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolDataEditorButton.Name = "toolDataEditorButton";
            toolDataEditorButton.Size = new Size(39, 40);
            toolDataEditorButton.Text = "\U0001f9e9";
            toolDataEditorButton.ToolTipText = "数据编辑器";
            toolDataEditorButton.Click += OpenDataEditorMenuItem_Click;
            // 
            // toolFormulaEditorButton
            // 
            toolFormulaEditorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolFormulaEditorButton.Name = "toolFormulaEditorButton";
            toolFormulaEditorButton.Size = new Size(34, 40);
            toolFormulaEditorButton.Text = "ƒ";
            toolFormulaEditorButton.ToolTipText = "公式编辑器";
            toolFormulaEditorButton.Click += OpenFormulaEditorMenuItem_Click;
            // 
            // toolMapEditorButton
            // 
            toolMapEditorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolMapEditorButton.Name = "toolMapEditorButton";
            toolMapEditorButton.Size = new Size(39, 40);
            toolMapEditorButton.Text = "🗺";
            toolMapEditorButton.ToolTipText = "地图编辑器";
            toolMapEditorButton.Click += OpenMapEditorMenuItem_Click;
            // 
            // toolEventGraphEditorButton
            // 
            toolEventGraphEditorButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolEventGraphEditorButton.Name = "toolEventGraphEditorButton";
            toolEventGraphEditorButton.Size = new Size(39, 40);
            toolEventGraphEditorButton.Text = "\U0001f9e0";
            toolEventGraphEditorButton.ToolTipText = "事件图编辑器";
            toolEventGraphEditorButton.Click += OpenEventGraphEditorMenuItem_Click;
            // 
            // toolRuntimeSep
            // 
            toolRuntimeSep.Name = "toolRuntimeSep";
            toolRuntimeSep.Size = new Size(6, 45);
            // 
            // toolRunButton
            // 
            toolRunButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolRunButton.Name = "toolRunButton";
            toolRunButton.Size = new Size(34, 40);
            toolRunButton.Text = "▶";
            toolRunButton.ToolTipText = "运行游戏";
            toolRunButton.Click += RunProjectButton_Click;
            // 
            // toolTestButton
            // 
            toolTestButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolTestButton.Name = "toolTestButton";
            toolTestButton.Size = new Size(39, 40);
            toolTestButton.Text = "\U0001f9ea";
            toolTestButton.ToolTipText = "测试当前项目";
            toolTestButton.Click += TestProjectButton_Click;
            // 
            // toolStopButton
            // 
            toolStopButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            toolStopButton.Name = "toolStopButton";
            toolStopButton.Size = new Size(34, 40);
            toolStopButton.Text = "■";
            toolStopButton.ToolTipText = "停止运行";
            toolStopButton.Click += StopRuntimeButton_Click;
            // 
            // toolSceneSep1
            // 
            toolSceneSep1.Name = "toolSceneSep1";
            toolSceneSep1.Size = new Size(6, 45);
            // 
            // _toolSelectSceneButton
            // 
            _toolSelectSceneButton.CheckOnClick = true;
            _toolSelectSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolSelectSceneButton.Name = "_toolSelectSceneButton";
            _toolSelectSceneButton.Size = new Size(39, 40);
            _toolSelectSceneButton.Text = "🖱";
            // 
            // _toolMoveSceneButton
            // 
            _toolMoveSceneButton.CheckOnClick = true;
            _toolMoveSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolMoveSceneButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _toolMoveSceneButton.Name = "_toolMoveSceneButton";
            _toolMoveSceneButton.Size = new Size(39, 40);
            _toolMoveSceneButton.Text = "💠";
            // 
            // _toolRotateSceneButton
            // 
            _toolRotateSceneButton.CheckOnClick = true;
            _toolRotateSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolRotateSceneButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _toolRotateSceneButton.Name = "_toolRotateSceneButton";
            _toolRotateSceneButton.Size = new Size(39, 40);
            _toolRotateSceneButton.Text = "🔄️";
            // 
            // _toolScaleSceneButton
            // 
            _toolScaleSceneButton.CheckOnClick = true;
            _toolScaleSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolScaleSceneButton.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _toolScaleSceneButton.ImageAlign = ContentAlignment.TopCenter;
            _toolScaleSceneButton.Name = "_toolScaleSceneButton";
            _toolScaleSceneButton.Size = new Size(36, 40);
            _toolScaleSceneButton.Text = "⤢";
            // 
            // _toolRectSceneButton
            // 
            _toolRectSceneButton.CheckOnClick = true;
            _toolRectSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolRectSceneButton.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _toolRectSceneButton.Name = "_toolRectSceneButton";
            _toolRectSceneButton.Padding = new Padding(0, 0, 2, 0);
            _toolRectSceneButton.Size = new Size(39, 40);
            _toolRectSceneButton.Text = "⌖";
            // 
            // _toolAlignSceneButton
            // 
            _toolAlignSceneButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolAlignSceneButton.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _toolAlignSceneButton.Name = "_toolAlignSceneButton";
            _toolAlignSceneButton.Size = new Size(34, 40);
            _toolAlignSceneButton.Text = "☰";
            // 
            // toolSceneSep2
            // 
            toolSceneSep2.Name = "toolSceneSep2";
            toolSceneSep2.Size = new Size(6, 45);
            // 
            // _toolToggleGridButton
            // 
            _toolToggleGridButton.CheckOnClick = true;
            _toolToggleGridButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _toolToggleGridButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _toolToggleGridButton.Name = "_toolToggleGridButton";
            _toolToggleGridButton.Size = new Size(39, 40);
            _toolToggleGridButton.Text = "\U0001fa9f";
            // 
            // mainSplit
            // 
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Location = new Point(0, 77);
            mainSplit.Name = "mainSplit";
            // 
            // mainSplit.Panel1
            // 
            mainSplit.Panel1.Controls.Add(leftSplit);
            mainSplit.Panel1MinSize = 0;
            // 
            // mainSplit.Panel2
            // 
            mainSplit.Panel2.Controls.Add(centerRightSplit);
            mainSplit.Panel2MinSize = 0;
            mainSplit.Size = new Size(1400, 732);
            mainSplit.SplitterDistance = 360;
            mainSplit.TabIndex = 2;
            // 
            // leftSplit
            // 
            leftSplit.Dock = DockStyle.Fill;
            leftSplit.Location = new Point(0, 0);
            leftSplit.Name = "leftSplit";
            leftSplit.Orientation = Orientation.Horizontal;
            // 
            // leftSplit.Panel1
            // 
            leftSplit.Panel1.Controls.Add(hierarchyGroupBox);
            leftSplit.Panel1MinSize = 0;
            // 
            // leftSplit.Panel2
            // 
            leftSplit.Panel2.Controls.Add(assetsGroupBox);
            leftSplit.Panel2MinSize = 0;
            leftSplit.Size = new Size(360, 732);
            leftSplit.SplitterDistance = 287;
            leftSplit.TabIndex = 0;
            // 
            // hierarchyGroupBox
            // 
            hierarchyGroupBox.Controls.Add(hierarchyTreeView);
            hierarchyGroupBox.Dock = DockStyle.Fill;
            hierarchyGroupBox.Location = new Point(0, 0);
            hierarchyGroupBox.Name = "hierarchyGroupBox";
            hierarchyGroupBox.Size = new Size(360, 287);
            hierarchyGroupBox.TabIndex = 0;
            hierarchyGroupBox.TabStop = false;
            hierarchyGroupBox.Text = "层级管理器";
            // 
            // hierarchyTreeView
            // 
            hierarchyTreeView.Dock = DockStyle.Fill;
            hierarchyTreeView.Location = new Point(3, 26);
            hierarchyTreeView.Name = "hierarchyTreeView";
            hierarchyTreeView.Size = new Size(354, 258);
            hierarchyTreeView.TabIndex = 0;
            // 
            // assetsGroupBox
            // 
            assetsGroupBox.Controls.Add(assetsTreeView);
            assetsGroupBox.Dock = DockStyle.Fill;
            assetsGroupBox.Location = new Point(0, 0);
            assetsGroupBox.Name = "assetsGroupBox";
            assetsGroupBox.Size = new Size(360, 441);
            assetsGroupBox.TabIndex = 0;
            assetsGroupBox.TabStop = false;
            assetsGroupBox.Text = "资源管理器";
            // 
            // assetsTreeView
            // 
            assetsTreeView.Dock = DockStyle.Fill;
            assetsTreeView.Location = new Point(3, 26);
            assetsTreeView.Name = "assetsTreeView";
            assetsTreeView.Size = new Size(354, 412);
            assetsTreeView.TabIndex = 0;
            // 
            // centerRightSplit
            // 
            centerRightSplit.Dock = DockStyle.Fill;
            centerRightSplit.Location = new Point(0, 0);
            centerRightSplit.Name = "centerRightSplit";
            // 
            // centerRightSplit.Panel1
            // 
            centerRightSplit.Panel1.Controls.Add(centerSplit);
            centerRightSplit.Panel1MinSize = 0;
            // 
            // centerRightSplit.Panel2
            // 
            centerRightSplit.Panel2.Controls.Add(rightSplit);
            centerRightSplit.Panel2MinSize = 0;
            centerRightSplit.Size = new Size(1036, 732);
            centerRightSplit.SplitterDistance = 760;
            centerRightSplit.TabIndex = 0;
            // 
            // centerSplit
            // 
            centerSplit.Dock = DockStyle.Fill;
            centerSplit.Location = new Point(0, 0);
            centerSplit.Name = "centerSplit";
            centerSplit.Orientation = Orientation.Horizontal;
            // 
            // centerSplit.Panel1
            // 
            centerSplit.Panel1.Controls.Add(sceneGroupBox);
            centerSplit.Panel1MinSize = 0;
            // 
            // centerSplit.Panel2
            // 
            centerSplit.Panel2.Controls.Add(logGroupBox);
            centerSplit.Panel2MinSize = 0;
            centerSplit.Size = new Size(760, 732);
            centerSplit.SplitterDistance = 517;
            centerSplit.TabIndex = 0;
            // 
            // sceneGroupBox
            // 
            sceneGroupBox.Controls.Add(sceneCanvasPanel);
            sceneGroupBox.Dock = DockStyle.Fill;
            sceneGroupBox.Location = new Point(0, 0);
            sceneGroupBox.Name = "sceneGroupBox";
            sceneGroupBox.Size = new Size(760, 517);
            sceneGroupBox.TabIndex = 0;
            sceneGroupBox.TabStop = false;
            sceneGroupBox.Text = "场景编辑器";
            // 
            // sceneCanvasPanel
            // 
            sceneCanvasPanel.ActiveTool = Editor.Controls.SceneToolMode.Select;
            sceneCanvasPanel.Dock = DockStyle.Fill;
            sceneCanvasPanel.Location = new Point(3, 26);
            sceneCanvasPanel.Name = "sceneCanvasPanel";
            sceneCanvasPanel.SelectedIndex = -1;
            sceneCanvasPanel.ShowGrid = true;
            sceneCanvasPanel.Size = new Size(754, 488);
            sceneCanvasPanel.TabIndex = 0;
            sceneCanvasPanel.TabStop = true;
            // 
            // logGroupBox
            // 
            logGroupBox.Controls.Add(logTextBox);
            logGroupBox.Dock = DockStyle.Fill;
            logGroupBox.Location = new Point(0, 0);
            logGroupBox.Name = "logGroupBox";
            logGroupBox.Size = new Size(760, 211);
            logGroupBox.TabIndex = 0;
            logGroupBox.TabStop = false;
            logGroupBox.Text = "活动日志";
            // 
            // logTextBox
            // 
            logTextBox.Dock = DockStyle.Fill;
            logTextBox.Location = new Point(3, 26);
            logTextBox.Multiline = true;
            logTextBox.Name = "logTextBox";
            logTextBox.ReadOnly = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Size = new Size(754, 182);
            logTextBox.TabIndex = 0;
            // 
            // rightSplit
            // 
            rightSplit.Dock = DockStyle.Fill;
            rightSplit.Location = new Point(0, 0);
            rightSplit.Name = "rightSplit";
            rightSplit.Orientation = Orientation.Horizontal;
            // 
            // rightSplit.Panel1
            // 
            rightSplit.Panel1.Controls.Add(inspectorGroupBox);
            rightSplit.Panel1MinSize = 0;
            // 
            // rightSplit.Panel2
            // 
            rightSplit.Panel2.Controls.Add(previewGroupBox);
            rightSplit.Panel2MinSize = 0;
            rightSplit.Size = new Size(272, 732);
            rightSplit.SplitterDistance = 513;
            rightSplit.TabIndex = 0;
            // 
            // inspectorGroupBox
            // 
            inspectorGroupBox.Controls.Add(inspectorTextBox);
            inspectorGroupBox.Dock = DockStyle.Fill;
            inspectorGroupBox.Location = new Point(0, 0);
            inspectorGroupBox.Name = "inspectorGroupBox";
            inspectorGroupBox.Size = new Size(272, 513);
            inspectorGroupBox.TabIndex = 0;
            inspectorGroupBox.TabStop = false;
            inspectorGroupBox.Text = "属性检查器";
            // 
            // inspectorTextBox
            // 
            inspectorTextBox.Dock = DockStyle.Fill;
            inspectorTextBox.Location = new Point(3, 26);
            inspectorTextBox.Multiline = true;
            inspectorTextBox.Name = "inspectorTextBox";
            inspectorTextBox.ReadOnly = true;
            inspectorTextBox.ScrollBars = ScrollBars.Vertical;
            inspectorTextBox.Size = new Size(266, 484);
            inspectorTextBox.TabIndex = 0;
            // 
            // previewGroupBox
            // 
            previewGroupBox.Controls.Add(previewBox);
            previewGroupBox.Dock = DockStyle.Fill;
            previewGroupBox.Location = new Point(0, 0);
            previewGroupBox.Name = "previewGroupBox";
            previewGroupBox.Size = new Size(272, 215);
            previewGroupBox.TabIndex = 0;
            previewGroupBox.TabStop = false;
            previewGroupBox.Text = "预览";
            // 
            // previewBox
            // 
            previewBox.BackColor = Color.White;
            previewBox.Location = new Point(66, 31);
            previewBox.Name = "previewBox";
            previewBox.Size = new Size(140, 140);
            previewBox.SizeMode = PictureBoxSizeMode.Zoom;
            previewBox.TabIndex = 0;
            previewBox.TabStop = false;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(24, 24);
            statusStrip.Items.AddRange(new ToolStripItem[] { projectStatusLabel, stateStatusLabel });
            statusStrip.Location = new Point(0, 809);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1400, 31);
            statusStrip.TabIndex = 2;
            // 
            // projectStatusLabel
            // 
            projectStatusLabel.Name = "projectStatusLabel";
            projectStatusLabel.Size = new Size(100, 24);
            projectStatusLabel.Text = "未打开项目";
            // 
            // stateStatusLabel
            // 
            stateStatusLabel.Alignment = ToolStripItemAlignment.Right;
            stateStatusLabel.Name = "stateStatusLabel";
            stateStatusLabel.Size = new Size(92, 24);
            stateStatusLabel.Text = "版本 0.1.0";
            // 
            // MainForm
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(1400, 840);
            Controls.Add(mainSplit);
            Controls.Add(editorToolStrip);
            Controls.Add(statusStrip);
            Controls.Add(mainMenuStrip);
            Font = new Font("Microsoft YaHei UI", 9F);
            MainMenuStrip = mainMenuStrip;
            MinimumSize = new Size(1200, 760);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Axe2D 编辑器";
            FormClosing += MainForm_FormClosing;
            Load += Form1_Load;
            Resize += MainForm_Resize;
            mainMenuStrip.ResumeLayout(false);
            mainMenuStrip.PerformLayout();
            editorToolStrip.ResumeLayout(false);
            editorToolStrip.PerformLayout();
            mainSplit.Panel1.ResumeLayout(false);
            mainSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)mainSplit).EndInit();
            mainSplit.ResumeLayout(false);
            leftSplit.Panel1.ResumeLayout(false);
            leftSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)leftSplit).EndInit();
            leftSplit.ResumeLayout(false);
            hierarchyGroupBox.ResumeLayout(false);
            assetsGroupBox.ResumeLayout(false);
            centerRightSplit.Panel1.ResumeLayout(false);
            centerRightSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)centerRightSplit).EndInit();
            centerRightSplit.ResumeLayout(false);
            centerSplit.Panel1.ResumeLayout(false);
            centerSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)centerSplit).EndInit();
            centerSplit.ResumeLayout(false);
            sceneGroupBox.ResumeLayout(false);
            logGroupBox.ResumeLayout(false);
            logGroupBox.PerformLayout();
            rightSplit.Panel1.ResumeLayout(false);
            rightSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)rightSplit).EndInit();
            rightSplit.ResumeLayout(false);
            inspectorGroupBox.ResumeLayout(false);
            inspectorGroupBox.PerformLayout();
            previewGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)previewBox).EndInit();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private MenuStrip mainMenuStrip;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem newProjectToolStripMenuItem;
        private ToolStripMenuItem openProjectToolStripMenuItem;
        private ToolStripMenuItem saveProjectToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem modulesToolStripMenuItem;
        private ToolStripMenuItem dataEditorToolStripMenuItem;
        private ToolStripMenuItem formulaEditorToolStripMenuItem;
        private ToolStripMenuItem mapEditorToolStripMenuItem;
        private ToolStripMenuItem eventGraphEditorToolStripMenuItem;
        private ToolStripMenuItem optionsToolStripMenuItem;
        private ToolStripMenuItem languageToolStripMenuItem;
        private ToolStripMenuItem languageZhCnToolStripMenuItem;
        private ToolStripMenuItem languageEnToolStripMenuItem;
        private ToolStripMenuItem languageJaJpToolStripMenuItem;
        private ToolStripMenuItem themeToolStripMenuItem;
        private ToolStripMenuItem lightThemeToolStripMenuItem;
        private ToolStripMenuItem darkThemeToolStripMenuItem;
        private ToolStrip editorToolStrip;
        private ToolStripButton toolNewProjectButton;
        private ToolStripButton toolOpenProjectButton;
        private ToolStripButton toolSaveProjectButton;
        private ToolStripSeparator toolSep1;
        private ToolStripButton toolDataEditorButton;
        private ToolStripButton toolFormulaEditorButton;
        private ToolStripButton toolMapEditorButton;
        private ToolStripButton toolEventGraphEditorButton;
        private ToolStripSeparator toolRuntimeSep;
        private ToolStripButton toolRunButton;
        private ToolStripButton toolTestButton;
        private ToolStripButton toolStopButton;
        private ToolStripSeparator toolSceneSep1;
        private ToolStripButton _toolSelectSceneButton;
        private ToolStripButton _toolMoveSceneButton;
        private ToolStripButton _toolRotateSceneButton;
        private ToolStripButton _toolScaleSceneButton;
        private ToolStripButton _toolRectSceneButton;
        private ToolStripButton _toolAlignSceneButton;
        private ToolStripSeparator toolSceneSep2;
        private ToolStripButton _toolUndoSceneButton;
        private ToolStripButton _toolRedoSceneButton;
        private ToolStripButton _toolToggleGridButton;
        private SplitContainer mainSplit;
        private SplitContainer leftSplit;
        private GroupBox hierarchyGroupBox;
        private TreeView hierarchyTreeView;
        private GroupBox assetsGroupBox;
        private TreeView assetsTreeView;
        private SplitContainer centerRightSplit;
        private SplitContainer centerSplit;
        private GroupBox sceneGroupBox;
        private global::Axe2DEditor.Editor.Controls.SceneCanvasPanel sceneCanvasPanel;
        private GroupBox logGroupBox;
        private TextBox logTextBox;
        private SplitContainer rightSplit;
        private GroupBox inspectorGroupBox;
        private TextBox inspectorTextBox;
        private GroupBox previewGroupBox;
        private PictureBox previewBox;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel projectStatusLabel;
        private ToolStripStatusLabel stateStatusLabel;
        private ToolStripSeparator toolStripSeparator1;
    }
}
