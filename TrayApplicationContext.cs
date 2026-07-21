using System.Reflection;
using Microsoft.Win32;
using StarTooth.Bluetooth;
using StarTooth.Resources;

namespace StarTooth;

/// <summary>Owns the tray icon and builds the device menu for the lifetime of the process.</summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    /// <summary>The product name is a proper noun and stays untranslated.</summary>
    private const string AppName = "StarTooth";

    private const string StarFilled = "★";
    private const string StarHollow = "☆";

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly DeviceService _devices = new();
    private readonly Favorites _favorites = new();
    private readonly System.Windows.Forms.Timer _refreshTimer;

    internal TrayApplicationContext()
    {
        _menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Renderer = new ThemedMenuRenderer(),
            ShowItemToolTips = true,
        };
        _menu.Opening += (_, _) => BuildMenu();

        _notifyIcon = new NotifyIcon
        {
            Icon = TrayIcons.Star,
            Text = AppName,
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _notifyIcon.MouseUp += OnTrayMouseUp;

        _devices.Updated += () => _menu.BeginInvoke(BuildMenu);

        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 15_000 };
        _refreshTimer.Tick += (_, _) => _ = RefreshAsync();
        _refreshTimer.Start();

        _ = RefreshAsync();
    }

    /// <summary>Picks up a light/dark switch while the app is running.</summary>
    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Color))
            return;

        Theme.Invalidate();
        _menu.Renderer = new ThemedMenuRenderer();
    }

    /// <summary>A left click should open the same menu as a right click.</summary>
    private void OnTrayMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;

        // NotifyIcon only wires the context menu to right clicks; this is the long-standing way
        // to reuse its positioning and dismissal behaviour for left clicks too.
        MethodInfo? show = typeof(NotifyIcon)
            .GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
        if (show is not null)
            show.Invoke(_notifyIcon, null);
        else
            _menu.Show(Cursor.Position);
    }

    private async Task RefreshAsync()
    {
        try
        {
            await _devices.RefreshAsync();
        }
        catch (Exception)
        {
            // A failed poll is not actionable; the next tick tries again.
        }
    }

    private void BuildMenu()
    {
        _menu.Items.Clear();

        var devices = _devices.Devices;
        if (devices.Count == 0)
        {
            _menu.Items.Add(new ToolStripMenuItem(Strings.MenuNoDevices) { Enabled = false });
        }
        else if (_favorites.IsEmpty)
        {
            // No star has ever been given: keep it a plain, ungrouped list.
            foreach (var device in devices)
                _menu.Items.Add(CreateDeviceItem(device));
        }
        else
        {
            var starred = devices.Where(d => _favorites.Contains(d.Key)).ToList();
            var rest = devices.Where(d => !_favorites.Contains(d.Key)).ToList();

            foreach (var device in starred)
                _menu.Items.Add(CreateDeviceItem(device));

            if (starred.Count > 0 && rest.Count > 0)
            {
                _menu.Items.Add(new ToolStripSeparator());
                _menu.Items.Add(new ToolStripMenuItem(Strings.MenuOtherDevices) { Enabled = false });
            }

            foreach (var device in rest)
                _menu.Items.Add(CreateDeviceItem(device));
        }

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Strings.MenuManageFavorites, null, (_, _) => ShowFavorites()));
        _menu.Items.Add(new ToolStripMenuItem(Strings.MenuRefresh, null, (_, _) => _ = RefreshAsync()));
        _menu.Items.Add(new ToolStripMenuItem(Strings.MenuExit, null, (_, _) => ExitThread()));
    }

    private ToolStripMenuItem CreateDeviceItem(BluetoothEntry device)
    {
        bool isFavorite = _favorites.Contains(device.Key);
        string state = device.IsConnected ? Strings.StateConnected : Strings.StateNotConnected;

        var item = new ToolStripMenuItem($"{(isFavorite ? StarFilled : StarHollow)}  {device.Name}")
        {
            Checked = device.IsConnected,
            CheckOnClick = false,
            ToolTipText = Strings.DeviceTooltip(ClassicBluetooth.FormatAddress(device.Address), state),

            // Bold text and a star glyph carry no meaning for a screen reader, so the state is
            // spelled out here instead of being left to the visuals.
            AccessibleName = isFavorite
                ? Strings.DeviceAccessibleFavorite(device.Name, state)
                : Strings.DeviceAccessiblePlain(device.Name, state),
            AccessibleDescription = device.IsConnected
                ? Strings.DeviceAccessibleActionDisconnect
                : Strings.DeviceAccessibleActionConnect,
        };

        if (device.IsConnected)
            item.Font = new Font(item.Font, FontStyle.Bold);

        item.Click += (_, _) => _ = ToggleConnectionAsync(device);
        return item;
    }

    private void ShowFavorites()
    {
        using var form = new FavoritesForm(_devices.Devices, _favorites);
        form.ShowDialog();
    }

    private async Task ToggleConnectionAsync(BluetoothEntry device)
    {
        bool connect = !device.IsConnected;
        try
        {
            SetTrayText(connect
                ? Strings.TrayConnecting(device.Name)
                : Strings.TrayDisconnecting(device.Name));
            await DeviceService.SetConnectedAsync(device, connect);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _notifyIcon.ShowBalloonTip(
                5000,
                connect ? Strings.ErrorConnectTitle : Strings.ErrorDisconnectTitle,
                Strings.ErrorBalloon(device.Name, ex.Message),
                ToolTipIcon.Warning);
        }
        finally
        {
            SetTrayText(AppName);
        }
    }

    /// <summary>NotifyIcon.Text throws above 63 characters, which a long device name can exceed.</summary>
    private void SetTrayText(string text)
    {
        _notifyIcon.Text = text.Length <= 63 ? text : text[..62] + "…";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            _refreshTimer.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
        }
        base.Dispose(disposing);
    }
}
