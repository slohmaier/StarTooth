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

    private const int BalloonTimeoutMs = 5000;

    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _menu;
    private readonly DeviceService _devices = new();
    private readonly Favorites _favorites = new();
    private readonly Settings _settings;
    private readonly System.Windows.Forms.Timer _refreshTimer;

    /// <summary>Devices with an attempt in flight, so the menu can show it and block re-entry.</summary>
    private readonly Dictionary<ulong, DeviceActivity> _activity = [];

    internal TrayApplicationContext(Settings settings)
    {
        _settings = settings;

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

        // Rebuilding while the menu is on screen would yank the items out from under the pointer
        // and out from under a screen reader's cursor. It is rebuilt on opening anyway.
        _devices.Updated += () => _menu.BeginInvoke(() =>
        {
            if (!_menu.Visible)
                BuildMenu();
        });

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

        _menu.Items.AddRange(DeviceMenuBuilder.Build(
            _devices.Devices,
            device => _favorites.Contains(device.Key),
            device => _activity.GetValueOrDefault(device.Address, DeviceActivity.None),
            device => _ = ToggleConnectionAsync(device)).ToArray());

        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(new ToolStripMenuItem(Strings.MenuManageFavorites, null, (_, _) => ShowFavorites()));
        _menu.Items.Add(new ToolStripMenuItem(Strings.MenuSettings, null, (_, _) => ShowSettings()));
        _menu.Items.Add(new ToolStripMenuItem(Strings.MenuRefresh, null, (_, _) => _ = RefreshAsync()));
        _menu.Items.Add(new ToolStripMenuItem(Strings.MenuExit, null, (_, _) => ExitThread()));
    }

    private void ShowFavorites()
    {
        using var form = new FavoritesForm(_devices.Devices, _favorites);
        form.ShowDialog();
    }

    private void ShowSettings()
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        // Both take effect without a restart: the menu is rebuilt from scratch every time it
        // opens, and any further dialog is constructed fresh.
        AppSetup.ApplyLanguage(_settings);
        AppSetup.ApplyColorMode(_settings);
        _menu.Renderer = new ThemedMenuRenderer();
        BuildMenu();
    }

    private async Task ToggleConnectionAsync(BluetoothEntry device)
    {
        // Activating an entry closes the menu, so the progress of an attempt cannot be shown
        // there. Notifications carry it instead, which is also the only channel a screen reader
        // hears without the user going looking for it.
        if (_activity.ContainsKey(device.Address))
            return;

        bool connect = !device.IsConnected;
        _activity[device.Address] = connect ? DeviceActivity.Connecting : DeviceActivity.Disconnecting;

        try
        {
            SetTrayText(connect
                ? Strings.TrayConnecting(device.Name)
                : Strings.TrayDisconnecting(device.Name));
            Notify(
                Strings.NotifyConnectingTitle,
                connect
                    ? Strings.NotifyConnectingText(device.Name)
                    : Strings.NotifyDisconnectingText(device.Name),
                ToolTipIcon.Info);

            await DeviceService.SetConnectedAsync(device, connect);

            Notify(
                connect ? Strings.NotifyConnectedTitle : Strings.NotifyDisconnectedTitle,
                connect
                    ? Strings.NotifyConnectedText(device.Name)
                    : Strings.NotifyDisconnectedText(device.Name),
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            Notify(
                connect ? Strings.ErrorConnectTitle : Strings.ErrorDisconnectTitle,
                Strings.ErrorBalloon(device.Name, ex.Message),
                ToolTipIcon.Error);
        }
        finally
        {
            _activity.Remove(device.Address);
            SetTrayText(AppName);
            await RefreshAsync();
        }
    }

    /// <summary>
    /// Shows a notification. Windows renders these as toasts, which both remain in the action
    /// centre and get announced by a screen reader without the user having to hunt for them.
    /// </summary>
    private void Notify(string title, string text, ToolTipIcon icon)
    {
        _notifyIcon.ShowBalloonTip(BalloonTimeoutMs, title, text, icon);
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
