using System.Runtime.InteropServices;

namespace StarTooth.Native;

/// <summary>
/// P/Invoke surface for bthprops.cpl (bluetoothapis.h). Used for enumerating paired
/// Classic Bluetooth devices and driving their connection state, which WinRT does not expose.
/// </summary>
internal static partial class BluetoothApis
{
    private const string Lib = "bthprops.cpl";

    internal const int BLUETOOTH_MAX_NAME_SIZE = 248;

    internal const uint BLUETOOTH_SERVICE_DISABLE = 0x00;
    internal const uint BLUETOOTH_SERVICE_ENABLE = 0x01;

    internal const int ERROR_SUCCESS = 0;
    internal const int ERROR_NO_MORE_ITEMS = 259;
    internal const int ERROR_INVALID_PARAMETER = 87;

    [StructLayout(LayoutKind.Sequential)]
    internal struct BLUETOOTH_FIND_RADIO_PARAMS
    {
        internal uint dwSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct BLUETOOTH_DEVICE_SEARCH_PARAMS
    {
        internal uint dwSize;
        [MarshalAs(UnmanagedType.Bool)] internal bool fReturnAuthenticated;
        [MarshalAs(UnmanagedType.Bool)] internal bool fReturnRemembered;
        [MarshalAs(UnmanagedType.Bool)] internal bool fReturnUnknown;
        [MarshalAs(UnmanagedType.Bool)] internal bool fReturnConnected;
        [MarshalAs(UnmanagedType.Bool)] internal bool fIssueInquiry;
        internal byte cTimeoutMultiplier;
        internal IntPtr hRadio;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct BLUETOOTH_DEVICE_INFO
    {
        internal uint dwSize;
        internal ulong Address;
        internal uint ulClassofDevice;
        [MarshalAs(UnmanagedType.Bool)] internal bool fConnected;
        [MarshalAs(UnmanagedType.Bool)] internal bool fRemembered;
        [MarshalAs(UnmanagedType.Bool)] internal bool fAuthenticated;
        internal SYSTEMTIME stLastSeen;
        internal SYSTEMTIME stLastUsed;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = BLUETOOTH_MAX_NAME_SIZE)]
        internal string szName;

        internal static BLUETOOTH_DEVICE_INFO Create() => new()
        {
            dwSize = (uint)Marshal.SizeOf<BLUETOOTH_DEVICE_INFO>(),
            szName = string.Empty,
        };
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SYSTEMTIME
    {
        internal ushort wYear, wMonth, wDayOfWeek, wDay, wHour, wMinute, wSecond, wMilliseconds;
    }

    [LibraryImport(Lib, EntryPoint = "BluetoothFindFirstRadio")]
    internal static partial IntPtr BluetoothFindFirstRadio(
        ref BLUETOOTH_FIND_RADIO_PARAMS pbtfrp, out IntPtr phRadio);

    [LibraryImport(Lib, EntryPoint = "BluetoothFindNextRadio")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool BluetoothFindNextRadio(IntPtr hFind, out IntPtr phRadio);

    [LibraryImport(Lib, EntryPoint = "BluetoothFindRadioClose")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool BluetoothFindRadioClose(IntPtr hFind);

    // The structs below carry BOOL and fixed-size string fields, which the LibraryImport source
    // generator refuses to marshal. DllImport's runtime marshaller handles them.

    [DllImport(Lib, EntryPoint = "BluetoothFindFirstDevice", CharSet = CharSet.Unicode)]
    internal static extern IntPtr BluetoothFindFirstDevice(
        ref BLUETOOTH_DEVICE_SEARCH_PARAMS pbtsp, ref BLUETOOTH_DEVICE_INFO pbtdi);

    [DllImport(Lib, EntryPoint = "BluetoothFindNextDevice", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool BluetoothFindNextDevice(IntPtr hFind, ref BLUETOOTH_DEVICE_INFO pbtdi);

    [LibraryImport(Lib, EntryPoint = "BluetoothFindDeviceClose")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool BluetoothFindDeviceClose(IntPtr hFind);

    /// <summary>
    /// Enables or disables a service (profile) on a paired device. Enabling is what actually
    /// triggers Windows to establish the link, so this is our "connect".
    /// </summary>
    [DllImport(Lib, EntryPoint = "BluetoothSetServiceState", CharSet = CharSet.Unicode)]
    internal static extern uint BluetoothSetServiceState(
        IntPtr hRadio, ref BLUETOOTH_DEVICE_INFO pbtdi, ref Guid pGuidService, uint dwServiceFlags);

    /// <summary>
    /// Lists the service GUIDs Windows has installed for a paired device. Call once with a null
    /// buffer to learn the count, then again to fill it.
    /// </summary>
    [DllImport(Lib, EntryPoint = "BluetoothEnumerateInstalledServices", CharSet = CharSet.Unicode)]
    internal static extern uint BluetoothEnumerateInstalledServices(
        IntPtr hRadio, ref BLUETOOTH_DEVICE_INFO pbtdi, ref uint pcServiceInout,
        [Out] Guid[]? pGuidServices);

    [LibraryImport("kernel32.dll", EntryPoint = "CloseHandle")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(IntPtr hObject);
}
