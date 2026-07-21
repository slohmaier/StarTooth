namespace StarTooth.Bluetooth;

/// <summary>
/// Merges the Classic and LE device lists and caches the result. The menu always renders from the
/// cache so that opening it is instant; refreshes happen in the background.
/// </summary>
internal sealed class DeviceService
{
    private List<BluetoothEntry> _cache = [];

    /// <summary>Raised on a background thread once a refresh has produced a new list.</summary>
    internal event Action? Updated;

    internal IReadOnlyList<BluetoothEntry> Devices => _cache;

    internal async Task RefreshAsync()
    {
        // Classic enumeration is a fast synchronous Win32 call, LE is not.
        List<BluetoothEntry> classic = await Task.Run(ClassicBluetooth.ListPaired);
        List<BluetoothEntry> lowEnergy = await LowEnergyBluetooth.ListPairedAsync();

        // A dual-mode device shows up in both lists. Classic wins: its connect path is the one
        // Windows itself uses for such devices.
        var byAddress = new Dictionary<ulong, BluetoothEntry>();
        foreach (var entry in lowEnergy)
            byAddress[entry.Address] = entry;
        foreach (var entry in classic)
            byAddress[entry.Address] = entry;

        _cache = byAddress.Values
            .OrderBy(d => d.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Updated?.Invoke();
    }

    internal static async Task SetConnectedAsync(BluetoothEntry device, bool connect)
    {
        if (device.Kind == DeviceKind.LowEnergy)
            await LowEnergyBluetooth.SetConnectedAsync(device.Address, connect);
        else
            await Task.Run(() => ClassicBluetooth.SetConnected(device.Address, connect));
    }
}
