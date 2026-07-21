using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace StarTooth.Bluetooth;

/// <summary>
/// Paired Bluetooth LE devices. Unlike Classic, LE has no "connect" call: the link comes up as a
/// side effect of touching the GATT server, and stays up only while a device object is alive.
/// </summary>
internal static class LowEnergyBluetooth
{
    /// <summary>Devices we are deliberately holding open to keep their link established.</summary>
    private static readonly Dictionary<ulong, BluetoothLEDevice> Held = [];

    internal static async Task<List<BluetoothEntry>> ListPairedAsync()
    {
        var entries = new List<BluetoothEntry>();

        string selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
        DeviceInformationCollection found = await DeviceInformation.FindAllAsync(selector);

        foreach (DeviceInformation info in found)
        {
            BluetoothLEDevice? device = null;
            try
            {
                device = await BluetoothLEDevice.FromIdAsync(info.Id);
                if (device is null)
                    continue;

                entries.Add(new BluetoothEntry
                {
                    Address = device.BluetoothAddress,
                    Name = string.IsNullOrWhiteSpace(device.Name) ? info.Name : device.Name,
                    IsConnected = device.ConnectionStatus == BluetoothConnectionStatus.Connected,
                    Kind = DeviceKind.LowEnergy,
                });
            }
            catch (Exception)
            {
                // A device that cannot be opened simply does not appear in the menu.
            }
            finally
            {
                if (device is not null && !Held.ContainsKey(device.BluetoothAddress))
                    device.Dispose();
            }
        }

        return entries;
    }

    internal static async Task SetConnectedAsync(ulong address, bool connect)
    {
        if (!connect)
        {
            if (Held.Remove(address, out BluetoothLEDevice? held))
                held.Dispose();
            return;
        }

        BluetoothLEDevice? device = await BluetoothLEDevice.FromBluetoothAddressAsync(address)
            ?? throw new InvalidOperationException("Gerät nicht erreichbar.");

        // Requesting the services is what triggers the actual connection attempt.
        var result = await device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (result.Status != Windows.Devices.Bluetooth.GenericAttributeProfile.GattCommunicationStatus.Success)
        {
            device.Dispose();
            throw new InvalidOperationException($"Verbindung fehlgeschlagen ({result.Status}).");
        }

        if (Held.Remove(address, out BluetoothLEDevice? previous))
            previous.Dispose();
        Held[address] = device;
    }
}
