#nullable disable
namespace Axe2DEditor.Editor.Modules;

partial class DataEditorFieldDialog
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel rootLayout;
    private Panel contentPanel;
    private Label hintLabel;
    private Panel editorHostPanel;
    private FlowLayoutPanel buttonPanel;
    private Button cancelButton;
    private Button okButton;

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
        contentPanel = new Panel();
        hintLabel = new Label();
        editorHostPanel = new Panel();
        buttonPanel = new FlowLayoutPanel();
        cancelButton = new Button();
        okButton = new Button();
        rootLayout.SuspendLayout();
        contentPanel.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(contentPanel, 0, 0);
        rootLayout.Controls.Add(buttonPanel, 0, 1);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 2;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        rootLayout.Size = new Size(320, 180);
        rootLayout.TabIndex = 0;
        // 
        // contentPanel
        // 
        contentPanel.Controls.Add(editorHostPanel);
        contentPanel.Controls.Add(hintLabel);
        contentPanel.Dock = DockStyle.Fill;
        contentPanel.Location = new Point(14, 14);
        contentPanel.Margin = new Padding(0);
        contentPanel.Name = "contentPanel";
        contentPanel.Padding = new Padding(8);
        contentPanel.Size = new Size(292, 110);
        contentPanel.TabIndex = 0;
        // 
        // hintLabel
        // 
        hintLabel.Dock = DockStyle.Top;
        hintLabel.ForeColor = SystemColors.GrayText;
        hintLabel.Location = new Point(0, 0);
        hintLabel.Name = "hintLabel";
        hintLabel.Padding = new Padding(0);
        hintLabel.Size = new Size(292, 30);
        hintLabel.TabIndex = 0;
        hintLabel.Text = "提示";
        hintLabel.TextAlign = ContentAlignment.MiddleLeft;
        hintLabel.Visible = false;
        // 
        // editorHostPanel
        // 
        editorHostPanel.Dock = DockStyle.Fill;
        editorHostPanel.Location = new Point(0, 30);
        editorHostPanel.Margin = new Padding(0);
        editorHostPanel.Name = "editorHostPanel";
        editorHostPanel.Padding = new Padding(0);
        editorHostPanel.Size = new Size(292, 80);
        editorHostPanel.TabIndex = 1;
        // 
        // buttonPanel
        // 
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(14, 124);
        buttonPanel.Margin = new Padding(0);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Padding = new Padding(0, 6, 0, 0);
        buttonPanel.Size = new Size(292, 52);
        buttonPanel.TabIndex = 1;
        buttonPanel.WrapContents = false;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(203, 9);
        cancelButton.Margin = new Padding(6, 0, 0, 0);
        cancelButton.MinimumSize = new Size(96, 32);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(96, 32);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "取消";
        cancelButton.UseVisualStyleBackColor = true;
        // 
        // okButton
        // 
        okButton.AutoSize = true;
        okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        okButton.Location = new Point(101, 9);
        okButton.Margin = new Padding(6, 0, 0, 0);
        okButton.MinimumSize = new Size(96, 32);
        okButton.Name = "okButton";
        okButton.Size = new Size(96, 32);
        okButton.TabIndex = 0;
        okButton.Text = "应用修改";
        okButton.UseVisualStyleBackColor = true;
        // 
        // DataEditorFieldDialog
        // 
        AcceptButton = okButton;
        AutoScaleMode = AutoScaleMode.None;
        CancelButton = cancelButton;
        ClientSize = new Size(320, 180);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(320, 150);
        Padding = new Padding(12);
        Name = "DataEditorFieldDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "编辑";
        rootLayout.ResumeLayout(false);
        contentPanel.ResumeLayout(false);
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }
}
