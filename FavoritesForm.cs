using StarTooth.Bluetooth;

namespace StarTooth;

/// <summary>
/// Lets the user star devices with the keyboard and a screen reader. A plain CheckedListBox is
/// used on purpose: it is a standard control, so its checked state is announced without any
/// custom accessibility work.
/// </summary>
internal sealed class FavoritesForm : Form
{
    private readonly IReadOnlyList<BluetoothEntry> _devices;
    private readonly Favorites _favorites;
    private readonly CheckedListBox _list;

    internal FavoritesForm(IReadOnlyList<BluetoothEntry> devices, Favorites favorites)
    {
        _devices = devices;
        _favorites = favorites;

        Text = "StarTooth – Favoriten verwalten";
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(420, 340);
        MinimumSize = new Size(360, 280);
        Icon = TrayIcons.Star;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        Padding = new Padding(12);

        var hint = new Label
        {
            Text = "&Geräte: Leertaste setzt oder entfernt den Stern.",
            Dock = DockStyle.Top,
            AutoSize = true,
            ForeColor = Theme.Foreground,
            Margin = new Padding(0, 0, 0, 6),
        };

        _list = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.IsDark ? Color.FromArgb(45, 45, 45) : Color.White,
            ForeColor = Theme.Foreground,
            IntegralHeight = false,
            AccessibleName = "Gepairte Geräte",
            AccessibleDescription =
                "Liste aller gepairten Bluetooth-Geräte. Angehakte Geräte erscheinen im Menü als Favoriten oben.",
        };

        foreach (var device in _devices)
            _list.Items.Add(device.Name, _favorites.Contains(device.Key));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 8, 0, 0),
        };

        var cancel = new Button
        {
            Text = "Abbre&chen",
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            BackColor = Theme.Highlight,
            ForeColor = Theme.Foreground,
            FlatStyle = Theme.IsDark ? FlatStyle.Flat : FlatStyle.Standard,
        };

        var ok = new Button
        {
            Text = "&OK",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            BackColor = Theme.Highlight,
            ForeColor = Theme.Foreground,
            FlatStyle = Theme.IsDark ? FlatStyle.Flat : FlatStyle.Standard,
        };

        buttons.Controls.Add(cancel);
        buttons.Controls.Add(ok);

        // Docked controls are laid out in reverse add order, so the fill goes in first.
        Controls.Add(_list);
        Controls.Add(hint);
        Controls.Add(buttons);

        AcceptButton = ok;
        CancelButton = cancel;

        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            for (int i = 0; i < _devices.Count; i++)
                _favorites.SetFavorite(_devices[i].Key, _list.GetItemChecked(i));
        }
        base.OnFormClosing(e);
    }
}
