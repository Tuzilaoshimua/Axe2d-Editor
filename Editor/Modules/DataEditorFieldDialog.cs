using System.Windows.Forms;

namespace Axe2DEditor.Editor.Modules;

public partial class DataEditorFieldDialog : Form
{
    public DataEditorFieldDialog()
    {
        InitializeComponent();
    }

    public void SetEditor(Control editor)
    {
        editorHostPanel.SuspendLayout();
        editorHostPanel.Controls.Clear();

        if (editor is not null)
        {
            editor.Dock = DockStyle.Fill;
            editor.Margin = Padding.Empty;
            editorHostPanel.Controls.Add(editor);
        }

        editorHostPanel.ResumeLayout(true);
    }

    public void SetHint(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            hintLabel.Visible = false;
            hintLabel.Text = string.Empty;
            return;
        }

        hintLabel.Visible = true;
        hintLabel.Text = text;
    }

    public Button ConfirmButton => okButton;
    public Button CancelActionButton => cancelButton;
}
