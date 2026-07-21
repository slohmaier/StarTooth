using StarTooth.Resources;

namespace StarTooth;

/// <summary>
/// Language, colour mode and autostart. Laid out with a TableLayoutPanel throughout so that a
/// longer translation grows the dialog instead of being clipped.
/// </summary>
internal sealed class SettingsForm : Form
{
    private readonly Settings _settings;
    private readonly ComboBox _language = new();
    private readonly ComboBox _colorMode = new();
    private readonly CheckBox _autostart = new();

    /// <summary>Display name paired with the value it stands for.</summary>
    private sealed record Choice(string Label, object Value)
    {
        public override string ToString() => Label;
    }

    internal SettingsForm(Settings settings)
    {
        _settings = settings;

        Text = Strings.SettingsTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = true;
        Icon = TrayIcons.Star;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        Padding = new Padding(14);

        // Deliberately not docked. A docked table inherits the form's width before the form has
        // been sized, and Form.AutoSize cannot widen it afterwards, which collapses the columns.
        var layout = new TableLayoutPanel
        {
            Location = new Point(Padding.Left, Padding.Top),
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Each label is added immediately before its control, so its access key lands on the
        // right one: a label hands focus to whatever follows it in the tab order.
        layout.Controls.Add(CreateLabel(Strings.SettingsLanguage), 0, 0);
        layout.Controls.Add(ConfigureCombo(_language, Strings.SettingsLanguage), 1, 0);

        layout.Controls.Add(CreateLabel(Strings.SettingsColorMode), 0, 1);
        layout.Controls.Add(ConfigureCombo(_colorMode, Strings.SettingsColorMode), 1, 1);

        _autostart.Text = Strings.SettingsAutostart;
        _autostart.AutoSize = true;
        _autostart.ForeColor = Theme.Foreground;
        _autostart.Margin = new Padding(3, 10, 3, 3);
        _autostart.AccessibleDescription = Strings.SettingsAutostartDescription;
        layout.Controls.Add(_autostart, 0, 2);
        layout.SetColumnSpan(_autostart, 2);

        // The buttons live in the table as well. A second docked panel would leave Form.AutoSize
        // unable to work out the height it needs, which collapses the dialog.
        var buttons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(3, 14, 3, 3),
        };
        var cancel = CreateButton(Strings.DialogCancel, DialogResult.Cancel);
        var ok = CreateButton(Strings.DialogOk, DialogResult.OK);
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        layout.Controls.Add(buttons, 0, 3);
        layout.SetColumnSpan(buttons, 2);

        Controls.Add(layout);

        AcceptButton = ok;
        CancelButton = cancel;

        Populate();
        SizeToContent(_language);
        SizeToContent(_colorMode);

        // Sized from the measured content, so a longer translation widens the dialog instead of
        // being cut off.
        Size preferred = layout.PreferredSize;
        ClientSize = new Size(
            preferred.Width + Padding.Horizontal,
            preferred.Height + Padding.Vertical);
    }

    private Label CreateLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = Theme.Foreground,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 7, 8, 3),
    };

    private ComboBox ConfigureCombo(ComboBox combo, string label)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        // AutoSize would discard the width, leaving the box collapsed in an AutoSize table.
        // The real width is measured from the items once they exist, see SizeToContent.
        combo.AutoSize = false;
        combo.Anchor = AnchorStyles.Left;
        combo.Margin = new Padding(3, 4, 3, 4);
        combo.FlatStyle = Theme.IsDark ? FlatStyle.Flat : FlatStyle.Standard;
        combo.BackColor = Theme.IsDark ? Color.FromArgb(45, 45, 45) : Color.White;
        combo.ForeColor = Theme.Foreground;
        // Without the ampersand, so the access key is not spoken as part of the name.
        combo.AccessibleName = label.Replace("&", string.Empty);
        return combo;
    }

    private Button CreateButton(string text, DialogResult result) => new()
    {
        Text = text,
        DialogResult = result,
        AutoSize = true,
        BackColor = Theme.Highlight,
        ForeColor = Theme.Foreground,
        FlatStyle = Theme.IsDark ? FlatStyle.Flat : FlatStyle.Standard,
        Margin = new Padding(6, 0, 0, 0),
    };

    /// <summary>Widens the box to its longest entry, so no translation gets cut off.</summary>
    private static void SizeToContent(ComboBox combo)
    {
        int widest = 0;
        foreach (object item in combo.Items)
            widest = Math.Max(widest, TextRenderer.MeasureText(item.ToString(), combo.Font).Width);

        combo.Width = widest + SystemInformation.VerticalScrollBarWidth + 12;
    }

    private void Populate()
    {
        // Languages are listed under their own names, as language pickers conventionally do.
        _language.Items.Add(new Choice(Strings.SettingsLanguageSystem, string.Empty));
        _language.Items.Add(new Choice("Deutsch", "de"));
        _language.Items.Add(new Choice("English", "en"));
        _language.SelectedIndex = _settings.Language switch
        {
            "de" => 1,
            "en" => 2,
            _ => 0,
        };

        _colorMode.Items.Add(new Choice(Strings.SettingsColorModeSystem, ThemeMode.System));
        _colorMode.Items.Add(new Choice(Strings.SettingsColorModeLight, ThemeMode.Light));
        _colorMode.Items.Add(new Choice(Strings.SettingsColorModeDark, ThemeMode.Dark));
        _colorMode.SelectedIndex = _settings.Theme switch
        {
            ThemeMode.Light => 1,
            ThemeMode.Dark => 2,
            _ => 0,
        };

        _autostart.Checked = Autostart.IsEnabled;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
        {
            base.OnFormClosing(e);
            return;
        }

        if (_autostart.Checked != Autostart.IsEnabled && !Autostart.TrySet(_autostart.Checked))
        {
            MessageBox.Show(
                this,
                Strings.SettingsAutostartFailedText,
                Strings.SettingsAutostartFailedTitle,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            // Keep the dialog open rather than reporting a change that did not happen.
            _autostart.Checked = Autostart.IsEnabled;
            e.Cancel = true;
            return;
        }

        _settings.Language = (string)((Choice)_language.SelectedItem!).Value;
        _settings.Theme = (ThemeMode)((Choice)_colorMode.SelectedItem!).Value;
        _settings.Save();

        base.OnFormClosing(e);
    }
}
