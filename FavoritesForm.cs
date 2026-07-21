using StarTooth.Bluetooth;
using StarTooth.Resources;

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

        Text = Strings.DialogTitle;
        FormBorderStyle = FormBorderStyle.Sizable;
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = true;
        ClientSize = new Size(420, 360);
        MinimumSize = new Size(360, 300);
        Icon = TrayIcons.Star;
        BackColor = Theme.Background;
        ForeColor = Theme.Foreground;
        Padding = new Padding(12);

        // AutoSize only grows the height of a top-docked label, so a translation wider than the
        // dialog gets clipped. The height is measured from the wrapped text instead of guessed,
        // because translations differ in length by far more than a fixed value would absorb.
        var hint = new Label
        {
            Text = Strings.DialogHint,
            Dock = DockStyle.Top,
            AutoSize = false,
            TextAlign = ContentAlignment.TopLeft,
            ForeColor = Theme.Foreground,
        };

        _list = new CheckedListBox
        {
            Dock = DockStyle.Fill,
            CheckOnClick = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.IsDark ? Color.FromArgb(45, 45, 45) : Color.White,
            ForeColor = Theme.Foreground,
            IntegralHeight = false,
            AccessibleName = Strings.DialogListName,
            AccessibleDescription = Strings.DialogListDescription,
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
            Text = Strings.DialogCancel,
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            BackColor = Theme.Highlight,
            ForeColor = Theme.Foreground,
            FlatStyle = Theme.IsDark ? FlatStyle.Flat : FlatStyle.Standard,
        };

        var ok = new Button
        {
            Text = Strings.DialogOk,
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

        hint.Height = MeasureWrappedHeight(hint) + 8;

        if (_list.Items.Count > 0)
            _list.SelectedIndex = 0;
    }

    /// <summary>Height the label needs once its text wraps to the available width.</summary>
    private int MeasureWrappedHeight(Label label)
    {
        int available = ClientSize.Width - Padding.Horizontal;
        return TextRenderer.MeasureText(
            label.Text,
            label.Font,
            new Size(available, 0),
            TextFormatFlags.WordBreak).Height;
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
