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

    /// <summary>Captures the favourites dialog to a PNG so its theming can be checked.</summary>
    internal static int RenderDialog(string outputPath)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        var devices = ClassicBluetooth.ListPaired();
        using var form = new FavoritesForm(devices, new Favorites());
        form.Text += $"  [{CultureInfo.CurrentUICulture.Name}]";
        form.Show();
        Application.DoEvents();

        using var bmp = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
        form.Close();

        Console.WriteLine(
            $"Theme: {(Theme.IsDark ? "dark" : "light")}, " +
            $"culture: {CultureInfo.CurrentUICulture.Name} -> {outputPath}");
        return 0;
    }

    /// <summary>
    /// Captures the device menu with synthetic devices covering every state, so the indicators can
    /// be checked without having to reproduce each state on real hardware.
    /// </summary>
    internal static int RenderMenu(string outputPath)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);

        var devices = new List<BluetoothEntry>
        {
            Fake(0x001122334455, "Shokz OpenFit", connected: true),
            Fake(0x00AABBCCDDEE, "HyperBraille", connected: false),
            Fake(0x001100110011, "Soundcore Space A40", connected: false),
            Fake(0x002200220022, "HyperFlat-76", connected: true),
        };
        var starred = new HashSet<ulong> { 0x001122334455, 0x00AABBCCDDEE };
        var activity = new Dictionary<ulong, DeviceActivity>
        {
            [0x001100110011] = DeviceActivity.Connecting,
            [0x002200220022] = DeviceActivity.Disconnecting,
        };

        using var menu = new ContextMenuStrip
        {
            ShowImageMargin = false,
            Renderer = new ThemedMenuRenderer(),
        };
        menu.Items.AddRange(DeviceMenuBuilder.Build(
            devices,
            d => starred.Contains(d.Address),
            d => activity.GetValueOrDefault(d.Address, DeviceActivity.None),
            _ => { }).ToArray());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem(StarTooth.Resources.Strings.MenuManageFavorites));
        menu.Items.Add(new ToolStripMenuItem(StarTooth.Resources.Strings.MenuRefresh));
        menu.Items.Add(new ToolStripMenuItem(StarTooth.Resources.Strings.MenuExit));

        menu.Show(new Point(0, 0));
        Application.DoEvents();

        using var bmp = new Bitmap(menu.Width, menu.Height);
        menu.DrawToBitmap(bmp, new Rectangle(0, 0, menu.Width, menu.Height));
        bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
        menu.Close();

        Console.WriteLine($"culture: {CultureInfo.CurrentUICulture.Name} -> {outputPath}");
        foreach (ToolStripItem item in menu.Items)
        {
            if (item is ToolStripMenuItem m && !string.IsNullOrEmpty(m.AccessibleName))
                Console.WriteLine($"  screen reader: \"{m.AccessibleName}\" | {m.AccessibleDescription}");
        }
        return 0;
    }

    private static BluetoothEntry Fake(ulong address, string name, bool connected) => new()
    {
        Address = address,
        Name = name,
        IsConnected = connected,
        Kind = DeviceKind.Classic,
    };

    internal static int List()
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        try
        {
            var devices = ClassicBluetooth.ListPaired();
            Console.WriteLine($"{devices.Count} paired Classic device(s):");
            foreach (var d in devices)
            {
                Console.WriteLine(
                    $"  [{(d.IsConnected ? "connected   " : "disconnected")}] " +
                    $"{ClassicBluetooth.FormatAddress(d.Address)}  CoD=0x{d.ClassOfDevice:X6}  {d.Name}");
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex}");
            return 1;
        }
    }

    internal static int SetConnected(string address, bool connect)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        try
        {
            ulong addr = ParseAddress(address);
            Console.WriteLine(
                $"{(connect ? "Connecting" : "Disconnecting")} {ClassicBluetooth.FormatAddress(addr)} ...");
            ClassicBluetooth.SetConnected(addr, connect);
            Console.WriteLine("OK");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
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
