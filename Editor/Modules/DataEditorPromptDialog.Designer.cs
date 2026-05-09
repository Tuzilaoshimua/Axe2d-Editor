#nullable disable
namespace Axe2DEditor.Editor.Modules;

partial class DataEditorPromptDialog
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel rootLayout;
    private Panel contentHostPanel;
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
        contentHostPanel = new Panel();
        buttonPanel = new FlowLayoutPanel();
        okButton = new Button();
        cancelButton = new Button();
        rootLayout.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // rootLayout
        // 
        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(buttonPanel, 0, 1);
        rootLayout.Controls.Add(contentHostPanel, 0, 0);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Location = new Point(0, 0);
        rootLayout.Margin = new Padding(0);
        rootLayout.Name = "rootLayout";
        rootLayout.RowCount = 2;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        rootLayout.Size = new Size(420, 180);
        rootLayout.TabIndex = 0;
        // 
        // contentHostPanel
        // 
        contentHostPanel.Dock = DockStyle.Fill;
        contentHostPanel.Location = new Point(0, 0);
        contentHostPanel.Margin = new Padding(0);
        contentHostPanel.Name = "contentHostPanel";
        contentHostPanel.Padding = new Padding(8);
        contentHostPanel.Size = new Size(392, 110);
        contentHostPanel.TabIndex = 0;
        // 
        // buttonPanel
        // 
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Dock = DockStyle.Fill;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(0, 128);
        buttonPanel.Margin = new Padding(0);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Padding = new Padding(0, 6, 0, 0);
        buttonPanel.Size = new Size(420, 52);
        buttonPanel.TabIndex = 1;
        buttonPanel.WrapContents = false;
        // 
        // okButton
        // 
        okButton.AutoSize = true;
        okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        okButton.Location = new Point(332, 6);
        okButton.Margin = new Padding(6, 0, 0, 0);
        okButton.MinimumSize = new Size(88, 30);
        okButton.Name = "okButton";
        okButton.Size = new Size(88, 34);
        okButton.TabIndex = 0;
        okButton.Text = "确定";
        okButton.UseVisualStyleBackColor = true;
        // 
        // cancelButton
        // 
        cancelButton.AutoSize = true;
        cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(238, 6);
        cancelButton.Margin = new Padding(6, 0, 0, 0);
        cancelButton.MinimumSize = new Size(88, 30);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(88, 34);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "取消";
        cancelButton.UseVisualStyleBackColor = true;
        // 
        // DataEditorPromptDialog
        // 
        AcceptButton = okButton;
        AutoScaleMode = AutoScaleMode.None;
        CancelButton = cancelButton;
        ClientSize = new Size(420, 180);
        Controls.Add(rootLayout);
        Font = new Font("Microsoft YaHei UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(320, 150);
        Padding = new Padding(12);
        Name = "DataEditorPromptDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "编辑";
        rootLayout.ResumeLayout(false);
        buttonPanel.ResumeLayout(false);
        buttonPanel.PerformLayout();
        ResumeLayout(false);
    }
}
