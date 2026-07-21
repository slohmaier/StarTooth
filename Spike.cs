using System.Globalization;
using System.Runtime.InteropServices;
using StarTooth.Bluetooth;

namespace StarTooth;

/// <summary>
/// Command-line probes used to validate the Bluetooth interop against real hardware without
/// going through the tray UI. Not part of the shipped user experience.
/// </summary>
internal static partial class Spike
{
    private const int ATTACH_PARENT_PROCESS = -1;

    [LibraryImport("kernel32.dll", EntryPoint = "AttachConsole")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AttachConsole(int dwProcessId);

    /// <summary>Writes the icon artwork to PNGs so it can be eyeballed at real tray sizes.</summary>
    internal static int RenderIcon(string outputDir)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        Directory.CreateDirectory(outputDir);
        foreach (int size in new[] { 16, 24, 32, 128, 512 })
        {
            using Bitmap bmp = TrayIcons.Render(size);
            string path = Path.Combine(outputDir, $"icon-{size}.png");
            bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            Console.WriteLine(path);
        }
        return 0;
    }

    internal static int List()
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        try
        {
            var devices = ClassicBluetooth.ListPaired();
            Console.WriteLine($"{devices.Count} gepairte Classic-Geräte:");
            foreach (var d in devices)
            {
                Console.WriteLine(
                    $"  [{(d.IsConnected ? "verbunden" : "getrennt ")}] " +
                    $"{ClassicBluetooth.FormatAddress(d.Address)}  CoD=0x{d.ClassOfDevice:X6}  {d.Name}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FEHLER: {ex}");
            return 1;
        }
    }

    internal static int SetConnected(string address, bool connect)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        try
        {
            ulong addr = ParseAddress(address);
            Console.WriteLine($"{(connect ? "Verbinde" : "Trenne")} {ClassicBluetooth.FormatAddress(addr)} ...");
            ClassicBluetooth.SetConnected(addr, connect);
            Console.WriteLine("OK");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FEHLER: {ex.Message}");
            return 1;
        }
    }

    /// <summary>Accepts "AA:BB:CC:DD:EE:FF", "AABBCCDDEEFF" or a raw decimal address.</summary>
    private static ulong ParseAddress(string text)
    {
        string cleaned = text.Replace(":", "").Replace("-", "").Trim();
        if (cleaned.Length == 12 &&
            ulong.TryParse(cleaned, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong hex))
            return hex;
        return ulong.Parse(text, CultureInfo.InvariantCulture);
    }
}
