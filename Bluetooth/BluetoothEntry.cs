namespace StarTooth.Bluetooth;

internal enum DeviceKind
{
    Classic,
    LowEnergy,
}

/// <summary>A paired device as shown in the tray menu.</summary>
internal sealed class BluetoothEntry
{
    internal required ulong Address { get; init; }
    internal required string Name { get; init; }
    internal required bool IsConnected { get; init; }
    internal required DeviceKind Kind { get; init; }

    /// <summary>Class-of-Device bitfield; only meaningful for Classic devices.</summary>
    internal uint ClassOfDevice { get; init; }

    /// <summary>Stable key used to persist favourites.</summary>
    internal string Key => $"{Kind}:{Address:X12}";
}
