using System.Runtime.InteropServices;
using StarTooth.Native;
using StarTooth.Resources;
using static StarTooth.Native.BluetoothApis;

namespace StarTooth.Bluetooth;

/// <summary>
/// Enumeration and connect/disconnect for paired Classic Bluetooth devices (headsets, keyboards,
/// mice, controllers). BLE devices are handled separately in <see cref="LowEnergyBluetooth"/>.
/// </summary>
internal static class ClassicBluetooth
{
    /// <summary>Every local Bluetooth radio. Caller must dispose the returned handles.</summary>
    internal static List<IntPtr> OpenRadios()
    {
        var radios = new List<IntPtr>();
        var findParams = new BLUETOOTH_FIND_RADIO_PARAMS
        {
            dwSize = (uint)Marshal.SizeOf<BLUETOOTH_FIND_RADIO_PARAMS>(),
        };

        IntPtr hFind = BluetoothFindFirstRadio(ref findParams, out IntPtr hRadio);
        if (hFind == IntPtr.Zero)
            return radios;

        try
        {
            radios.Add(hRadio);
            while (BluetoothFindNextRadio(hFind, out hRadio))
                radios.Add(hRadio);
        }
        finally
        {
            BluetoothFindRadioClose(hFind);
        }

        return radios;
    }

    /// <summary>All remembered (paired) devices across all radios.</summary>
    internal static List<BluetoothEntry> ListPaired()
    {
        var entries = new List<BluetoothEntry>();

        foreach (IntPtr hRadio in OpenRadios())
        {
            try
            {
                foreach (var info in EnumerateDevices(hRadio))
                {
                    entries.Add(new BluetoothEntry
                    {
                        Address = info.Address,
                        Name = string.IsNullOrWhiteSpace(info.szName)
                            ? FormatAddress(info.Address)
                            : info.szName,
                        IsConnected = info.fConnected,
                        ClassOfDevice = info.ulClassofDevice,
                        Kind = DeviceKind.Classic,
                    });
                }
            }
            finally
            {
                CloseHandle(hRadio);
            }
        }

        return entries;
    }

    private static IEnumerable<BLUETOOTH_DEVICE_INFO> EnumerateDevices(IntPtr hRadio)
    {
        var searchParams = new BLUETOOTH_DEVICE_SEARCH_PARAMS
        {
            dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_SEARCH_PARAMS>(),
            fReturnAuthenticated = true,
            fReturnRemembered = true,
            fReturnUnknown = false,
            fReturnConnected = true,
            fIssueInquiry = false,
            cTimeoutMultiplier = 0,
            hRadio = hRadio,
        };

        var info = BLUETOOTH_DEVICE_INFO.Create();
        IntPtr hFind = BluetoothFindFirstDevice(ref searchParams, ref info);
        if (hFind == IntPtr.Zero)
            yield break;

        try
        {
            do
            {
                yield return info;
                info = BLUETOOTH_DEVICE_INFO.Create();
            }
            while (BluetoothFindNextDevice(hFind, ref info));
        }
        finally
        {
            BluetoothFindDeviceClose(hFind);
        }
    }

    /// <summary>
    /// Toggles every installed service on the device. Enabling the services is what makes Windows
    /// bring the link up; disabling tears it down.
    /// </summary>
    internal static void SetConnected(ulong address, bool connect)
    {
        uint flags = connect ? BLUETOOTH_SERVICE_ENABLE : BLUETOOTH_SERVICE_DISABLE;
        var errors = new List<string>();
        bool found = false;

        foreach (IntPtr hRadio in OpenRadios())
        {
            try
            {
                var info = BLUETOOTH_DEVICE_INFO.Create();
                info.Address = address;

                Guid[] services = GetInstalledServices(hRadio, ref info);
                if (services.Length == 0)
                    continue;

                found = true;
                foreach (Guid service in services)
                {
                    Guid guid = service;
                    uint result = BluetoothSetServiceState(hRadio, ref info, ref guid, flags);
                    if (result != ERROR_SUCCESS)
                        errors.Add($"{guid}: {new System.ComponentModel.Win32Exception((int)result).Message}");
                }
            }
            finally
            {
                CloseHandle(hRadio);
            }
        }

        if (!found)
            throw new InvalidOperationException(Strings.ErrorNoServices);
        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
    }

    private static Guid[] GetInstalledServices(IntPtr hRadio, ref BLUETOOTH_DEVICE_INFO info)
    {
        uint count = 0;
        uint result = BluetoothEnumerateInstalledServices(hRadio, ref info, ref count, null);

        // With a null buffer the API reports the count and returns ERROR_MORE_DATA-ish codes;
        // treat "no services" and hard failures alike as nothing to do on this radio.
        if (count == 0)
            return [];

        var services = new Guid[count];
        result = BluetoothEnumerateInstalledServices(hRadio, ref info, ref count, services);
        if (result != ERROR_SUCCESS)
            return [];

        return services[..(int)count];
    }

    internal static string FormatAddress(ulong address)
    {
        byte[] bytes = BitConverter.GetBytes(address);
        return string.Join(":", bytes.Take(6).Reverse().Select(b => b.ToString("X2")));
    }
}
