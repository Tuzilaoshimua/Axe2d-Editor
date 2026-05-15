using Axe2DEditor.Editor.Localization;

namespace Axe2DEditor.Editor.Modules;

internal partial class MapSettingsDialog : Form
{
    private sealed record ViewTypeChoice(string Value, string Display)
    {
        public override string ToString() => Display;
    }

    private readonly LocalizationService? _localization;

    public string MapId { get; private set; } = string.Empty;
    public string MapName { get; private set; } = string.Empty;
    public string MapDescription { get; private set; } = string.Empty;
    public string ViewType { get; private set; } = string.Empty;
    public int MapWidth { get; private set; }
    public int MapHeight { get; private set; }
    public int TileSize { get; private set; }
    public string Tileset { get; private set; } = string.Empty;
    public string BackgroundColor { get; private set; } = string.Empty;

    public MapSettingsDialog() : this(null!)
    {
    }

    public MapSettingsDialog(LocalizationService localization)
    {
        _localization = localization;
        InitializeComponent();
    }

    public void SetValues(string id, string name, string description, string viewType, int width, int height, int tileSize, string tileset, string backgroundColor)
    {
        idTextBox.Text = id;
        nameTextBox.Text = name;
        descriptionTextBox.Text = description;
        widthNumericUpDown.Value = width;
        heightNumericUpDown.Value = height;
        tileSizeNumericUpDown.Value = tileSize;
        tilesetTextBox.Text = tileset;
        backgroundTextBox.Text = backgroundColor;

        ConfigureViewTypeCombo(viewType);
        UpdateBackgroundColorPreview();
    }

    private void ConfigureViewTypeCombo(string selectedValue)
    {
        viewTypeComboBox.Items.Clear();
        viewTypeComboBox.Items.Add(new ViewTypeChoice("TopDown", T("dataEditor.option.viewType.TopDown", "俯视图")));
        viewTypeComboBox.Items.Add(new ViewTypeChoice("Platformer", T("dataEditor.option.viewType.Platformer", "横版")));
        viewTypeComboBox.Items.Add(new ViewTypeChoice("Isometric", T("dataEditor.option.viewType.Isometric", "等距视角")));
        viewTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        for (var i = 0; i < viewTypeComboBox.Items.Count; i++)
        {
            if (viewTypeComboBox.Items[i] is ViewTypeChoice choice
                && string.Equals(choice.Value, selectedValue, StringComparison.OrdinalIgnoreCase))
            {
                viewTypeComboBox.SelectedIndex = i;
                return;
            }
        }

        viewTypeComboBox.SelectedIndex = viewTypeComboBox.Items.Count > 0 ? 0 : -1;
    }

    private string T(string key, string fallback)
    {
        if (_localization is null)
        {
            return fallback;
        }

        var value = _localization.T(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private void UpdateBackgroundColorPreview()
    {
        var color = ParseColorOrDefault(backgroundTextBox.Text);
        backgroundColorButton.BackColor = color;
        backgroundColorButton.ForeColor = GetContrastColor(color);
    }

    private static Color ParseColorOrDefault(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Color.Black;
        }

        try
        {
            return ColorTranslator.FromHtml(text);
        }
        catch
        {
            return Color.Black;
        }
    }

    private static Color GetContrastColor(Color color)
    {
        var luminance = (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
        return luminance >= 140 ? Color.Black : Color.White;
    }

    private void backgroundColorButton_Click(object sender, EventArgs e)
    {
        var initialColor = ParseColorOrDefault(backgroundTextBox.Text);
        using var colorDialog = new ColorDialog
        {
            AnyColor = true,
            FullOpen = true,
            SolidColorOnly = false,
            Color = initialColor
        };

        if (colorDialog.ShowDialog(this) == DialogResult.OK)
        {
            backgroundTextBox.Text = $"#{colorDialog.Color.R:X2}{colorDialog.Color.G:X2}{colorDialog.Color.B:X2}".ToLowerInvariant();
            UpdateBackgroundColorPreview();
        }
    }

    private void okButton_Click(object sender, EventArgs e)
    {
        MapId = idTextBox.Text;
        MapName = nameTextBox.Text;
        MapDescription = descriptionTextBox.Text;
        ViewType = viewTypeComboBox.SelectedItem is ViewTypeChoice choice
            ? choice.Value
            : "TopDown";
        MapWidth = (int)widthNumericUpDown.Value;
        MapHeight = (int)heightNumericUpDown.Value;
        TileSize = (int)tileSizeNumericUpDown.Value;
        Tileset = tilesetTextBox.Text;
        BackgroundColor = backgroundTextBox.Text;

        DialogResult = DialogResult.OK;
        Close();
    }

    private void cancelButton_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}