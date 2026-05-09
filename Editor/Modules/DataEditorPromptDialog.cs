using System.Windows.Forms;

namespace Axe2DEditor.Editor.Modules;

public partial class DataEditorPromptDialog : Form
{
    public DataEditorPromptDialog()
    {
        InitializeComponent();
    }

    public void SetContent(Control content)
    {
        contentHostPanel.SuspendLayout();
        contentHostPanel.Controls.Clear();

        if (content is not null)
        {
            content.Dock = DockStyle.Fill;
            content.Margin = Padding.Empty;
            contentHostPanel.Controls.Add(content);
        }

        contentHostPanel.ResumeLayout(true);
    }

    public Button ConfirmButton => okButton;
    public Button CancelActionButton => cancelButton;
}
