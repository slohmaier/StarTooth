using StarTooth.Bluetooth;
using StarTooth.Resources;

namespace StarTooth;

/// <summary>
/// Turns the device list into menu items. Kept separate from the tray context so the result can be
/// rendered and inspected with synthetic devices covering every state.
/// </summary>
internal static class DeviceMenuBuilder
{
    // Indicators are decoration only. Screen readers announce symbols inconsistently — whether a
    // glyph is spoken at all depends on the user's symbol verbosity — so every one of these has a
    // spoken counterpart in AccessibleName, and none of them is the sole carrier of any state.
    private const string IndicatorConnected = "●";
    private const string IndicatorDisconnected = "○";
    private const string IndicatorBusy = "◌";
    private const string StarFilled = "★";

    /// <summary>
    /// En space rather than a plain one: it matches the star's width closely, so the connection
    /// indicators line up in a column when only some devices are starred.
    /// </summary>
    private const string StarNone = " ";

    internal static List<ToolStripItem> Build(
        IReadOnlyList<BluetoothEntry> devices,
        Func<BluetoothEntry, bool> isFavorite,
        Func<BluetoothEntry, DeviceActivity> activityOf,
        Action<BluetoothEntry> onActivate)
    {
        var items = new List<ToolStripItem>();

        if (devices.Count == 0)
        {
            items.Add(new ToolStripMenuItem(Strings.MenuNoDevices) { Enabled = false });
            return items;
        }

        var starred = devices.Where(d => isFavorite(d)).ToList();
        var rest = devices.Where(d => !isFavorite(d)).ToList();

        // Without a single star there is nothing to group by, so the list stays flat.
        if (starred.Count == 0)
        {
            foreach (var device in rest)
                items.Add(CreateItem(device, false, activityOf(device), onActivate));
            return items;
        }

        foreach (var device in starred)
            items.Add(CreateItem(device, true, activityOf(device), onActivate));

        if (rest.Count > 0)
        {
            items.Add(new ToolStripSeparator());
            items.Add(new ToolStripMenuItem(Strings.MenuOtherDevices) { Enabled = false });
            foreach (var device in rest)
                items.Add(CreateItem(device, false, activityOf(device), onActivate));
        }

        return items;
    }

    private static ToolStripMenuItem CreateItem(
        BluetoothEntry device,
        bool isFavorite,
        DeviceActivity activity,
        Action<BluetoothEntry> onActivate)
    {
        bool busy = activity != DeviceActivity.None;

        string state = activity switch
        {
            DeviceActivity.Connecting => Strings.StateConnecting,
            DeviceActivity.Disconnecting => Strings.StateDisconnecting,
            _ => device.IsConnected ? Strings.StateConnected : Strings.StateNotConnected,
        };

        string indicator = busy
            ? IndicatorBusy
            : device.IsConnected ? IndicatorConnected : IndicatorDisconnected;

        var item = new ToolStripMenuItem($"{(isFavorite ? StarFilled : StarNone)} {indicator}  {device.Name}")
        {
            // Deliberately left enabled while an attempt runs: ToolStrip skips disabled items
            // during keyboard navigation, which would make the running attempt the one state a
            // keyboard or screen reader user could never reach. Re-entry is refused by the caller
            // instead.
            AccessibleName = isFavorite
                ? Strings.DeviceAccessibleFavorite(device.Name, state)
                : Strings.DeviceAccessiblePlain(device.Name, state),
            AccessibleDescription = busy
                ? Strings.DeviceAccessibleActionBusy
                : device.IsConnected
                    ? Strings.DeviceAccessibleActionDisconnect
                    : Strings.DeviceAccessibleActionConnect,
            ToolTipText = Strings.DeviceTooltip(ClassicBluetooth.FormatAddress(device.Address), state),
        };

        if (device.IsConnected && !busy)
            item.Font = new Font(item.Font, FontStyle.Bold);

        item.Click += (_, _) => onActivate(device);
        return item;
    }
}
