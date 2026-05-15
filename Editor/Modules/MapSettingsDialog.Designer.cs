namespace Axe2DEditor.Editor.Modules;

partial class MapSettingsDialog
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel mainTableLayoutPanel;
    private Label idLabel;
    private Label nameLabel;
    private Label descriptionLabel;
    private Label viewTypeLabel;
    private Label widthLabel;
    private Label heightLabel;
    private Label tileSizeLabel;
    private Label tilesetLabel;
    private Label backgroundLabel;
    private TextBox idTextBox;
    private TextBox nameTextBox;
    private TextBox descriptionTextBox;
    private ComboBox viewTypeComboBox;
    private NumericUpDown widthNumericUpDown;
    private NumericUpDown heightNumericUpDown;
    private NumericUpDown tileSizeNumericUpDown;
    private TextBox tilesetTextBox;
    private TextBox backgroundTextBox;
    private Button backgroundColorButton;
    private Button okButton;
    private Button cancelButton;
    private FlowLayoutPanel buttonPanel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        mainTableLayoutPanel = new TableLayoutPanel();
        idLabel = new Label();
        idTextBox = new TextBox();
        nameLabel = new Label();
        nameTextBox = new TextBox();
        descriptionLabel = new Label();
        descriptionTextBox = new TextBox();
        viewTypeLabel = new Label();
        viewTypeComboBox = new ComboBox();
        widthLabel = new Label();
        widthNumericUpDown = new NumericUpDown();
        heightLabel = new Label();
        heightNumericUpDown = new NumericUpDown();
        tileSizeLabel = new Label();
        tileSizeNumericUpDown = new NumericUpDown();
        tilesetLabel = new Label();
        tilesetTextBox = new TextBox();
        backgroundLabel = new Label();
        backgroundTextBox = new TextBox();
        backgroundColorButton = new Button();
        okButton = new Button();
        cancelButton = new Button();
        buttonPanel = new FlowLayoutPanel();
        mainTableLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)widthNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)heightNumericUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)tileSizeNumericUpDown).BeginInit();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // mainTableLayoutPanel
        // 
        mainTableLayoutPanel.ColumnCount = 3;
        mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 101F));
        mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 79F));
        mainTableLayoutPanel.Controls.Add(idLabel, 0, 0);
        mainTableLayoutPanel.Controls.Add(idTextBox, 1, 0);
        mainTableLayoutPanel.Controls.Add(nameLabel, 0, 1);
        mainTableLayoutPanel.Controls.Add(nameTextBox, 1, 1);
        mainTableLayoutPanel.Controls.Add(descriptionLabel, 0, 2);
        mainTableLayoutPanel.Controls.Add(descriptionTextBox, 1, 2);
        mainTableLayoutPanel.Controls.Add(viewTypeLabel, 0, 3);
        mainTableLayoutPanel.Controls.Add(viewTypeComboBox, 1, 3);
        mainTableLayoutPanel.Controls.Add(widthLabel, 0, 4);
        mainTableLayoutPanel.Controls.Add(widthNumericUpDown, 1, 4);
        mainTableLayoutPanel.Controls.Add(heightLabel, 0, 5);
        mainTableLayoutPanel.Controls.Add(heightNumericUpDown, 1, 5);
        mainTableLayoutPanel.Controls.Add(tileSizeLabel, 0, 6);
        mainTableLayoutPanel.Controls.Add(tileSizeNumericUpDown, 1, 6);
        mainTableLayoutPanel.Controls.Add(tilesetLabel, 0, 7);
        mainTableLayoutPanel.Controls.Add(tilesetTextBox, 1, 7);
        mainTableLayoutPanel.Controls.Add(backgroundLabel, 0, 8);
        mainTableLayoutPanel.Controls.Add(backgroundTextBox, 1, 8);
        mainTableLayoutPanel.Controls.Add(backgroundColorButton, 2, 8);
        mainTableLayoutPanel.Dock = DockStyle.Fill;
        mainTableLayoutPanel.Location = new Point(19, 17);
        mainTableLayoutPanel.Margin = new Padding(5, 4, 5, 4);
        mainTableLayoutPanel.Name = "mainTableLayoutPanel";
        mainTableLayoutPanel.RowCount = 9;
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 102F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
        mainTableLayoutPanel.Size = new Size(555, 486);
        mainTableLayoutPanel.TabIndex = 0;
        // 
        // idLabel
        // 
        idLabel.Dock = DockStyle.Fill;
        idLabel.Location = new Point(5, 0);
        idLabel.Margin = new Padding(5, 0, 5, 0);
        idLabel.Name = "idLabel";
        idLabel.Size = new Size(91, 48);
        idLabel.TabIndex = 0;
        idLabel.Text = "ID";
        idLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // idTextBox
        // 
        mainTableLayoutPanel.SetColumnSpan(idTextBox, 2);
        idTextBox.Dock = DockStyle.Fill;
        idTextBox.Location = new Point(106, 8);
        idTextBox.Margin = new Padding(5, 8, 5, 4);
        idTextBox.Name = "idTextBox";
        idTextBox.Size = new Size(444, 30);
        idTextBox.TabIndex = 9;
        // 
        // nameLabel
        // 
        nameLabel.Dock = DockStyle.Fill;
        nameLabel.Location = new Point(5, 48);
        nameLabel.Margin = new Padding(5, 0, 5, 0);
        nameLabel.Name = "nameLabel";
        nameLabel.Size = new Size(91, 48);
        nameLabel.TabIndex = 1;
        nameLabel.Text = "名称";
        nameLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // nameTextBox
        // 
        mainTableLayoutPanel.SetColumnSpan(nameTextBox, 2);
        nameTextBox.Dock = DockStyle.Fill;
        nameTextBox.Location = new Point(106, 56);
        nameTextBox.Margin = new Padding(5, 8, 5, 4);
        nameTextBox.Name = "nameTextBox";
        nameTextBox.Size = new Size(444, 30);
        nameTextBox.TabIndex = 10;
        // 
        // descriptionLabel
        // 
        descriptionLabel.Dock = DockStyle.Fill;
        descriptionLabel.Location = new Point(5, 96);
        descriptionLabel.Margin = new Padding(5, 0, 5, 0);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Size = new Size(91, 102);
        descriptionLabel.TabIndex = 2;
        descriptionLabel.Text = "描述";
        // 
        // descriptionTextBox
        // 
        mainTableLayoutPanel.SetColumnSpan(descriptionTextBox, 2);
        descriptionTextBox.Dock = DockStyle.Fill;
        descriptionTextBox.Location = new Point(106, 104);
        descriptionTextBox.Margin = new Padding(5, 8, 5, 4);
        descriptionTextBox.Multiline = true;
        descriptionTextBox.Name = "descriptionTextBox";
        descriptionTextBox.ScrollBars = ScrollBars.Vertical;
        descriptionTextBox.Size = new Size(444, 90);
        descriptionTextBox.TabIndex = 11;
        // 
        // viewTypeLabel
        // 
        viewTypeLabel.Dock = DockStyle.Fill;
        viewTypeLabel.Location = new Point(5, 198);
        viewTypeLabel.Margin = new Padding(5, 0, 5, 0);
        viewTypeLabel.Name = "viewTypeLabel";
        viewTypeLabel.Size = new Size(91, 48);
        viewTypeLabel.TabIndex = 3;
        viewTypeLabel.Text = "视角";
        viewTypeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // viewTypeComboBox
        // 
        mainTableLayoutPanel.SetColumnSpan(viewTypeComboBox, 2);
        viewTypeComboBox.Dock = DockStyle.Fill;
        viewTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        viewTypeComboBox.FormattingEnabled = true;
        viewTypeComboBox.Location = new Point(106, 206);
        viewTypeComboBox.Margin = new Padding(5, 8, 5, 4);
        viewTypeComboBox.Name = "viewTypeComboBox";
        viewTypeComboBox.Size = new Size(444, 32);
        viewTypeComboBox.TabIndex = 12;
        // 
        // widthLabel
        // 
        widthLabel.Dock = DockStyle.Fill;
        widthLabel.Location = new Point(5, 246);
        widthLabel.Margin = new Padding(5, 0, 5, 0);
        widthLabel.Name = "widthLabel";
        widthLabel.Size = new Size(91, 48);
        widthLabel.TabIndex = 4;
        widthLabel.Text = "宽度";
        widthLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // widthNumericUpDown
        // 
        mainTableLayoutPanel.SetColumnSpan(widthNumericUpDown, 2);
        widthNumericUpDown.Dock = DockStyle.Fill;
        widthNumericUpDown.Location = new Point(106, 254);
        widthNumericUpDown.Margin = new Padding(5, 8, 5, 4);
        widthNumericUpDown.Maximum = new decimal(new int[] { 512, 0, 0, 0 });
        widthNumericUpDown.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        widthNumericUpDown.Name = "widthNumericUpDown";
        widthNumericUpDown.Size = new Size(444, 30);
        widthNumericUpDown.TabIndex = 13;
        widthNumericUpDown.Value = new decimal(new int[] { 64, 0, 0, 0 });
        // 
        // heightLabel
        // 
        heightLabel.Dock = DockStyle.Fill;
        heightLabel.Location = new Point(5, 294);
        heightLabel.Margin = new Padding(5, 0, 5, 0);
        heightLabel.Name = "heightLabel";
        heightLabel.Size = new Size(91, 48);
        heightLabel.TabIndex = 5;
        heightLabel.Text = "高度";
        heightLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // heightNumericUpDown
        // 
        mainTableLayoutPanel.SetColumnSpan(heightNumericUpDown, 2);
        heightNumericUpDown.Dock = DockStyle.Fill;
        heightNumericUpDown.Location = new Point(106, 302);
        heightNumericUpDown.Margin = new Padding(5, 8, 5, 4);
        heightNumericUpDown.Maximum = new decimal(new int[] { 512, 0, 0, 0 });
        heightNumericUpDown.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        heightNumericUpDown.Name = "heightNumericUpDown";
        heightNumericUpDown.Size = new Size(444, 30);
        heightNumericUpDown.TabIndex = 14;
        heightNumericUpDown.Value = new decimal(new int[] { 64, 0, 0, 0 });
        // 
        // tileSizeLabel
        // 
        tileSizeLabel.Dock = DockStyle.Fill;
        tileSizeLabel.Location = new Point(5, 342);
        tileSizeLabel.Margin = new Padding(5, 0, 5, 0);
        tileSizeLabel.Name = "tileSizeLabel";
        tileSizeLabel.Size = new Size(91, 48);
        tileSizeLabel.TabIndex = 6;
        tileSizeLabel.Text = "瓦片";
        tileSizeLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tileSizeNumericUpDown
        // 
        mainTableLayoutPanel.SetColumnSpan(tileSizeNumericUpDown, 2);
        tileSizeNumericUpDown.Dock = DockStyle.Fill;
        tileSizeNumericUpDown.Location = new Point(106, 350);
        tileSizeNumericUpDown.Margin = new Padding(5, 8, 5, 4);
        tileSizeNumericUpDown.Maximum = new decimal(new int[] { 128, 0, 0, 0 });
        tileSizeNumericUpDown.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        tileSizeNumericUpDown.Name = "tileSizeNumericUpDown";
        tileSizeNumericUpDown.Size = new Size(444, 30);
        tileSizeNumericUpDown.TabIndex = 15;
        tileSizeNumericUpDown.Value = new decimal(new int[] { 48, 0, 0, 0 });
        // 
        // tilesetLabel
        // 
        tilesetLabel.Dock = DockStyle.Fill;
        tilesetLabel.Location = new Point(5, 390);
        tilesetLabel.Margin = new Padding(5, 0, 5, 0);
        tilesetLabel.Name = "tilesetLabel";
        tilesetLabel.Size = new Size(91, 48);
        tilesetLabel.TabIndex = 7;
        tilesetLabel.Text = "图块集";
        tilesetLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // tilesetTextBox
        // 
        mainTableLayoutPanel.SetColumnSpan(tilesetTextBox, 2);
        tilesetTextBox.Dock = DockStyle.Fill;
        tilesetTextBox.Location = new Point(106, 398);
        tilesetTextBox.Margin = new Padding(5, 8, 5, 4);
        tilesetTextBox.Name = "tilesetTextBox";
        tilesetTextBox.Size = new Size(444, 30);
        tilesetTextBox.TabIndex = 16;
        // 
        // backgroundLabel
        // 
        backgroundLabel.Dock = DockStyle.Fill;
        backgroundLabel.Location = new Point(5, 438);
        backgroundLabel.Margin = new Padding(5, 0, 5, 0);
        backgroundLabel.Name = "backgroundLabel";
        backgroundLabel.Size = new Size(91, 48);
        backgroundLabel.TabIndex = 8;
        backgroundLabel.Text = "背景";
        backgroundLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // backgroundTextBox
        // 
        backgroundTextBox.Dock = DockStyle.Fill;
        backgroundTextBox.Location = new Point(106, 446);
        backgroundTextBox.Margin = new Padding(5, 8, 5, 4);
        backgroundTextBox.Name = "backgroundTextBox";
        backgroundTextBox.ReadOnly = true;
        backgroundTextBox.Size = new Size(365, 30);
        backgroundTextBox.TabIndex = 17;
        // 
        // backgroundColorButton
        // 
        backgroundColorButton.Dock = DockStyle.Fill;
        backgroundColorButton.Location = new Point(481, 442);
        backgroundColorButton.Margin = new Padding(5, 4, 5, 4);
        backgroundColorButton.Name = "backgroundColorButton";
        backgroundColorButton.Size = new Size(69, 40);
        backgroundColorButton.TabIndex = 18;
        backgroundColorButton.Text = "...";
        backgroundColorButton.UseVisualStyleBackColor = true;
        backgroundColorButton.Click += backgroundColorButton_Click;
        // 
        // okButton
        // 
        okButton.Location = new Point(412, 18);
        okButton.Margin = new Padding(5, 4, 5, 4);
        okButton.Name = "okButton";
        okButton.Size = new Size(138, 45);
        okButton.TabIndex = 0;
        okButton.Text = "确定";
        okButton.UseVisualStyleBackColor = true;
        okButton.Click += okButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.Location = new Point(264, 18);
        cancelButton.Margin = new Padding(5, 4, 5, 4);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(138, 45);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "取消";
        cancelButton.UseVisualStyleBackColor = true;
        cancelButton.Click += cancelButton_Click;
        // 
        // buttonPanel
        // 
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Dock = DockStyle.Bottom;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(19, 503);
        buttonPanel.Margin = new Padding(5, 4, 5, 4);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Padding = new Padding(0, 14, 0, 0);
        buttonPanel.Size = new Size(555, 73);
        buttonPanel.TabIndex = 1;
        // 
        // MapSettingsDialog
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(11F, 24F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(593, 593);
        Controls.Add(mainTableLayoutPanel);
        Controls.Add(buttonPanel);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Margin = new Padding(5, 4, 5, 4);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "MapSettingsDialog";
        Padding = new Padding(19, 17, 19, 17);
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "地图设定";
        mainTableLayoutPanel.ResumeLayout(false);
        mainTableLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)widthNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)heightNumericUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)tileSizeNumericUpDown).EndInit();
        buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
