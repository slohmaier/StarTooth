using System.Globalization;
using System.Resources;

namespace StarTooth.Resources;

/// <summary>
/// Typed access to the localised strings. Written by hand rather than generated, so that the
/// project builds the same way from the CLI as it does from an IDE.
/// </summary>
internal static class Strings
{
    private static readonly ResourceManager Manager =
        new("StarTooth.Resources.Strings", typeof(Strings).Assembly);

    internal static string MenuNoDevices => Get("Menu.NoDevices");
    internal static string MenuOtherDevices => Get("Menu.OtherDevices");
    internal static string MenuManageFavorites => Get("Menu.ManageFavorites");
    internal static string MenuRefresh => Get("Menu.Refresh");
    internal static string MenuExit => Get("Menu.Exit");

    internal static string StateConnected => Get("State.Connected");
    internal static string StateNotConnected => Get("State.NotConnected");
    internal static string StateConnecting => Get("State.Connecting");
    internal static string StateDisconnecting => Get("State.Disconnecting");

    internal static string DeviceAccessibleFavorite(string name, string state) =>
        Format("Device.Accessible.Favorite", name, state);
    internal static string DeviceAccessiblePlain(string name, string state) =>
        Format("Device.Accessible.Plain", name, state);
    internal static string DeviceAccessibleActionConnect => Get("Device.Accessible.ActionConnect");
    internal static string DeviceAccessibleActionDisconnect => Get("Device.Accessible.ActionDisconnect");
    internal static string DeviceAccessibleActionBusy => Get("Device.Accessible.ActionBusy");
    internal static string DeviceTooltip(string address, string state) =>
        Format("Device.Tooltip", address, state);

    internal static string NotifyConnectingTitle => Get("Notify.ConnectingTitle");
    internal static string NotifyConnectingText(string name) => Format("Notify.ConnectingText", name);
    internal static string NotifyDisconnectingText(string name) => Format("Notify.DisconnectingText", name);
    internal static string NotifyConnectedTitle => Get("Notify.ConnectedTitle");
    internal static string NotifyConnectedText(string name) => Format("Notify.ConnectedText", name);
    internal static string NotifyDisconnectedTitle => Get("Notify.DisconnectedTitle");
    internal static string NotifyDisconnectedText(string name) => Format("Notify.DisconnectedText", name);

    internal static string TrayConnecting(string name) => Format("Tray.Connecting", name);
    internal static string TrayDisconnecting(string name) => Format("Tray.Disconnecting", name);

    internal static string ErrorConnectTitle => Get("Error.ConnectTitle");
    internal static string ErrorDisconnectTitle => Get("Error.DisconnectTitle");
    internal static string ErrorBalloon(string name, string message) =>
        Format("Error.Balloon", name, message);
    internal static string ErrorNoServices => Get("Error.NoServices");
    internal static string ErrorDeviceUnreachable => Get("Error.DeviceUnreachable");
    internal static string ErrorGattFailed(string status) => Format("Error.GattFailed", status);

    internal static string DialogTitle => Get("Dialog.Title");
    internal static string DialogHint => Get("Dialog.Hint");
    internal static string DialogListName => Get("Dialog.ListName");
    internal static string DialogListDescription => Get("Dialog.ListDescription");
    internal static string DialogOk => Get("Dialog.Ok");
    internal static string DialogCancel => Get("Dialog.Cancel");

    /// <summary>Falls back to the key itself, so a missing resource is visible rather than blank.</summary>
    private static string Get(string key) =>
        Manager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    private static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.CurrentUICulture, Get(key), args);
}
