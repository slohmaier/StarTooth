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
        CaptureWindow(form, outputPath);

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

    /// <summary>
    /// Renders a window to a PNG for inspecting layout and text.
    ///
    /// The background colour in the result is NOT trustworthy: DrawToBitmap renders the client
    /// background dark even when the form's BackColor is white and Application.ColorMode is
    /// Classic. Judge light mode by the values the app reports, printed alongside each capture,
    /// or by simply looking at the running dialog. Reading the screen instead is not an option
    /// either — without a running message loop the window is not painted in time, and the capture
    /// returns whatever else is on the desktop.
    /// </summary>
    private static void CaptureWindow(Form form, string outputPath)
    {
        form.Show();
        Application.DoEvents();

        using var bmp = new Bitmap(form.Width, form.Height);
        using (var g = Graphics.FromImage(bmp))
            g.Clear(form.BackColor);

        form.DrawToBitmap(bmp, new Rectangle(0, 0, form.Width, form.Height));
        bmp.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
        form.Close();
    }

    /// <summary>Captures the settings dialog so its layout and theming can be checked.</summary>
    internal static int RenderSettings(string outputPath, Settings settings)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        using var form = new SettingsForm(settings);
        CaptureWindow(form, outputPath);

#pragma warning disable WFO5001 // Application.ColorMode is experimental in .NET 9.
        string colorMode = Application.ColorMode.ToString();
#pragma warning restore WFO5001

        // These values, not the PNG, are what says whether light mode is applied.
        Console.WriteLine($"culture:      {CultureInfo.CurrentUICulture.Name}");
        Console.WriteLine($"autostart:    {Autostart.IsEnabled}");
        Console.WriteLine($"Theme.Mode:   {Theme.Mode} (isDark: {Theme.IsDark})");
        Console.WriteLine($"ColorMode:    {colorMode}");
        Console.WriteLine($"BackColor:    {form.BackColor}");
        Console.WriteLine($"written to:   {outputPath}");
        return 0;
    }

    /// <summary>
    /// Writes a real multi-resolution .ico (PNG-compressed entries, supported by Windows Vista+)
    /// from the runtime icon artwork. Used as the application and installer icon.
    /// </summary>
    internal static int RenderIcoFile(string outputPath)
    {
        AttachConsole(ATTACH_PARENT_PROCESS);
        int[] sizes = [16, 24, 32, 48, 64, 128, 256];

        var images = new List<byte[]>();
        foreach (int size in sizes)
        {
            using Bitmap bmp = TrayIcons.Render(size);
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            images.Add(ms.ToArray());
        }

        using var file = new FileStream(outputPath, FileMode.Create);
        using var w = new BinaryWriter(file);

        // ICONDIR
        w.Write((ushort)0);            // reserved
        w.Write((ushort)1);            // type: icon
        w.Write((ushort)sizes.Length); // image count

        int offset = 6 + (16 * sizes.Length);
        for (int i = 0; i < sizes.Length; i++)
        {
            // ICONDIRENTRY. A dimension of 256 is encoded as 0.
            w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i])); // width
            w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i])); // height
            w.Write((byte)0);              // palette count
            w.Write((byte)0);              // reserved
            w.Write((ushort)1);            // colour planes
            w.Write((ushort)32);           // bits per pixel
            w.Write((uint)images[i].Length);
            w.Write((uint)offset);
            offset += images[i].Length;
        }

        foreach (byte[] image in images)
            w.Write(image);

        Console.WriteLine($"{sizes.Length} sizes -> {outputPath}");
        return 0;
    }

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
